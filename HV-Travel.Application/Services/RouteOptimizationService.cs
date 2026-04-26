using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class RouteOptimizationService : IRouteOptimizationService
{
    private const int BruteForceLimit = 7;
    private const int MaxTwoOptIterations = 10;
    private static readonly string[] StartAnchorKeywords = ["meeting", "pickup", "checkin", "departure"];
    private static readonly string[] EndAnchorKeywords = ["hotel", "dropoff", "return", "checkout"];

    private readonly IRouteInsightService _routeInsightService;
    private readonly IRouteTravelEstimator _routeTravelEstimator;

    public RouteOptimizationService()
        : this(new RouteInsightService(new RouteTravelEstimator()), new RouteTravelEstimator())
    {
    }

    public RouteOptimizationService(IRouteInsightService routeInsightService)
        : this(routeInsightService, new RouteTravelEstimator())
    {
    }

    public RouteOptimizationService(IRouteInsightService routeInsightService, IRouteTravelEstimator routeTravelEstimator)
    {
        _routeInsightService = routeInsightService;
        _routeTravelEstimator = routeTravelEstimator;
    }

    public RouteOptimizationResult Optimize(Tour? tour)
    {
        return Optimize(tour, new RouteOptimizationRequest
        {
            Profile = RouteOptimizationProfiles.Balanced
        });
    }

    public RouteOptimizationResult Optimize(Tour? tour, RouteOptimizationRequest request)
    {
        var profile = ResolveProfile(request);
        var orderedStops = (tour?.Routing?.Stops ?? new List<TourRouteStop>())
            .Where(IsValidStop)
            .OrderBy(stop => stop.Day)
            .ThenBy(stop => stop.Order)
            .Select(CloneStop)
            .ToList();

        if (tour == null || orderedStops.Count == 0)
        {
            return BuildEmptyResult(profile, "Chưa có dữ liệu lộ trình hợp lệ để tối ưu.");
        }

        var currentTour = CloneTour(tour, orderedStops);
        var currentInsight = _routeInsightService.Build(currentTour);

        var warnings = new List<RouteOptimizationWarning>();
        var dayResults = new List<RouteOptimizationDayResult>();
        var suggestedStops = new List<TourRouteStop>();
        var hasOptimizableDay = false;
        var hasChangedDay = false;
        double currentObjectiveScore = 0d;
        double suggestedObjectiveScore = 0d;

        foreach (var dayGroup in orderedStops.GroupBy(stop => stop.Day).OrderBy(group => group.Key))
        {
            var dayStops = dayGroup
                .OrderBy(stop => stop.Order)
                .Select(CloneStop)
                .ToList();

            var outcome = OptimizeDay(dayStops, warnings, profile);
            hasOptimizableDay |= outcome.CanOptimize;
            hasChangedDay |= outcome.Changed;
            currentObjectiveScore += outcome.CurrentObjectiveScore;
            suggestedObjectiveScore += outcome.SuggestedObjectiveScore;

            suggestedStops.AddRange(outcome.SuggestedCandidate.Stops.Select(CloneStop));

            var currentDayInsight = outcome.CurrentCandidate.Insight;
            var suggestedDayInsight = outcome.SuggestedCandidate.Insight;

            dayResults.Add(new RouteOptimizationDayResult
            {
                Day = dayGroup.Key,
                Changed = outcome.Changed,
                StopCount = dayStops.Count,
                CurrentObjectiveScore = outcome.CurrentObjectiveScore,
                SuggestedObjectiveScore = outcome.SuggestedObjectiveScore,
                CurrentTravelMinutes = currentDayInsight.TotalTravelMinutes,
                SuggestedTravelMinutes = suggestedDayInsight.TotalTravelMinutes,
                CurrentJourneyMinutes = currentDayInsight.TotalJourneyMinutes,
                SuggestedJourneyMinutes = suggestedDayInsight.TotalJourneyMinutes,
                CurrentDistanceKm = currentDayInsight.TotalDistanceKm,
                SuggestedDistanceKm = suggestedDayInsight.TotalDistanceKm,
                CurrentStops = outcome.CurrentCandidate.Stops.Select(ToPreview).ToList(),
                SuggestedStops = outcome.SuggestedCandidate.Stops.Select(ToPreview).ToList()
            });
        }

        var suggestedTour = CloneTour(tour, suggestedStops);
        var suggestedInsight = _routeInsightService.Build(suggestedTour);
        var assignments = suggestedStops
            .OrderBy(stop => stop.Day)
            .ThenBy(stop => stop.Order)
            .Select(stop => new RouteOptimizationAssignment
            {
                ClientKey = stop.Id,
                Day = stop.Day,
                Order = stop.Order
            })
            .ToList();

        return new RouteOptimizationResult
        {
            CanOptimize = hasChangedDay,
            Profile = profile.Id,
            ProfileLabel = profile.Label,
            ProfileDescription = profile.Description,
            CurrentObjectiveScore = currentObjectiveScore,
            SuggestedObjectiveScore = suggestedObjectiveScore,
            CurrentInsight = currentInsight,
            SuggestedInsight = suggestedInsight,
            Assignments = assignments,
            Days = dayResults,
            Warnings = warnings,
            UnchangedReason = hasChangedDay ? string.Empty : ResolveUnchangedReason(hasOptimizableDay, warnings, orderedStops.Count)
        };
    }

    private DayOptimizationOutcome OptimizeDay(
        IReadOnlyList<TourRouteStop> dayStops,
        ICollection<RouteOptimizationWarning> warnings,
        RouteOptimizationProfileDefinition profile)
    {
        var original = dayStops
            .OrderBy(stop => stop.Order)
            .Select(CloneStop)
            .ToList();

        var currentCandidate = EvaluateCandidate(original, original);
        var fallback = new DayOptimizationOutcome
        {
            CurrentCandidate = currentCandidate,
            SuggestedCandidate = currentCandidate
        };

        if (original.Count < 3)
        {
            return fallback;
        }

        if (original.Any(stop => !HasCoordinatePair(stop.Coordinates)))
        {
            warnings.Add(new RouteOptimizationWarning
            {
                Code = "missing_coordinates",
                Day = original[0].Day,
                ClientKey = original.FirstOrDefault(stop => !HasCoordinatePair(stop.Coordinates))?.Id ?? string.Empty,
                Message = $"Ngày {original[0].Day} bị bỏ qua vì có ít nhất một điểm dừng thiếu tọa độ."
            });
            return fallback;
        }

        var startAnchor = original.FirstOrDefault(stop => MatchesAny(stop.Type, StartAnchorKeywords));
        var endAnchor = original.LastOrDefault(stop => MatchesAny(stop.Type, EndAnchorKeywords) && !ReferenceEquals(stop, startAnchor));

        var optimizable = original
            .Where(stop => !ReferenceEquals(stop, startAnchor) && !ReferenceEquals(stop, endAnchor))
            .Select(CloneStop)
            .ToList();

        if (optimizable.Count < 2)
        {
            return fallback;
        }

        var candidates = optimizable.Count <= BruteForceLimit
            ? BuildBruteForceCandidates(original, startAnchor, endAnchor, optimizable)
            : BuildHeuristicCandidates(original, startAnchor, endAnchor, optimizable, profile);

        AddCandidate(candidates, currentCandidate);
        ApplyObjectiveScores(candidates.Values, profile);

        var bestCandidate = candidates.Values
            .OrderBy(candidate => candidate, DayCandidateComparer.Instance)
            .First();

        var changed = !HaveSameSequence(original, bestCandidate.Stops);
        return new DayOptimizationOutcome
        {
            CanOptimize = true,
            Changed = changed,
            CurrentCandidate = currentCandidate,
            SuggestedCandidate = bestCandidate,
            CurrentObjectiveScore = currentCandidate.ObjectiveScore,
            SuggestedObjectiveScore = bestCandidate.ObjectiveScore
        };
    }

    private Dictionary<string, DayCandidate> BuildBruteForceCandidates(
        IReadOnlyList<TourRouteStop> originalDay,
        TourRouteStop? startAnchor,
        TourRouteStop? endAnchor,
        IReadOnlyList<TourRouteStop> optimizableStops)
    {
        var candidates = new Dictionary<string, DayCandidate>(StringComparer.Ordinal);

        foreach (var permutation in EnumeratePermutations(optimizableStops.ToList(), 0))
        {
            var candidateStops = BuildCandidateStops(startAnchor, permutation, endAnchor);
            AddCandidate(candidates, EvaluateCandidate(originalDay, candidateStops));
        }

        return candidates;
    }

    private Dictionary<string, DayCandidate> BuildHeuristicCandidates(
        IReadOnlyList<TourRouteStop> originalDay,
        TourRouteStop? startAnchor,
        TourRouteStop? endAnchor,
        IReadOnlyList<TourRouteStop> optimizableStops,
        RouteOptimizationProfileDefinition profile)
    {
        var candidates = new Dictionary<string, DayCandidate>(StringComparer.Ordinal);
        var currentMiddle = optimizableStops
            .OrderBy(stop => stop.Order)
            .Select(CloneStop)
            .ToList();

        var seedOrders = new List<List<TourRouteStop>>
        {
            currentMiddle.Select(CloneStop).ToList(),
            BuildNearestNeighborByTravel(originalDay, startAnchor, currentMiddle),
            BuildNearestNeighborByProfile(originalDay, startAnchor, currentMiddle, profile),
            currentMiddle.AsEnumerable().Reverse().Select(CloneStop).ToList()
        };

        if (string.Equals(profile.Id, RouteOptimizationProfiles.HighlightsFirst, StringComparison.Ordinal))
        {
            seedOrders.Add(currentMiddle
                .OrderByDescending(stop => stop.AttractionScore)
                .ThenBy(stop => stop.Order)
                .Select(CloneStop)
                .ToList());
        }

        foreach (var seed in seedOrders)
        {
            var seededDay = BuildCandidateStops(startAnchor, seed, endAnchor);
            AddCandidate(candidates, EvaluateCandidate(originalDay, seededDay));

            var improvedDay = ApplyIterativeTwoOpt(
                seededDay,
                startAnchor != null,
                endAnchor != null,
                originalDay,
                profile);

            AddCandidate(candidates, EvaluateCandidate(originalDay, improvedDay));
        }

        return candidates;
    }

    private List<TourRouteStop> BuildNearestNeighborByTravel(
        IReadOnlyList<TourRouteStop> originalDay,
        TourRouteStop? startAnchor,
        IReadOnlyList<TourRouteStop> optimizableStops)
    {
        var remaining = optimizableStops
            .Select(CloneStop)
            .ToList();

        var ordered = new List<TourRouteStop>();
        TourRouteStop current;

        if (startAnchor != null)
        {
            current = CloneStop(startAnchor);
        }
        else
        {
            current = CloneStop(originalDay.OrderBy(stop => stop.Order).First());
            ordered.Add(CloneStop(current));
            remaining.RemoveAll(stop => string.Equals(stop.Id, current.Id, StringComparison.Ordinal));
        }

        while (remaining.Count > 0)
        {
            var next = remaining
                .OrderBy(stop => CalculateLegTravelMinutes(current, stop))
                .ThenBy(stop => CalculateDistanceKm(current, stop))
                .ThenBy(stop => stop.Order)
                .First();

            ordered.Add(CloneStop(next));
            remaining.RemoveAll(stop => string.Equals(stop.Id, next.Id, StringComparison.Ordinal));
            current = next;
        }

        return ordered;
    }

    private List<TourRouteStop> BuildNearestNeighborByProfile(
        IReadOnlyList<TourRouteStop> originalDay,
        TourRouteStop? startAnchor,
        IReadOnlyList<TourRouteStop> optimizableStops,
        RouteOptimizationProfileDefinition profile)
    {
        var remaining = optimizableStops
            .Select(CloneStop)
            .ToList();

        var originalIndex = originalDay
            .Select((stop, index) => new { stop.Id, index })
            .ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);

        var ordered = new List<TourRouteStop>();
        TourRouteStop current;

        if (startAnchor != null)
        {
            current = CloneStop(startAnchor);
        }
        else
        {
            current = CloneStop(originalDay.OrderBy(stop => stop.Order).First());
            ordered.Add(CloneStop(current));
            remaining.RemoveAll(stop => string.Equals(stop.Id, current.Id, StringComparison.Ordinal));
        }

        while (remaining.Count > 0)
        {
            var candidateChoices = remaining
                .Select(stop =>
                {
                    originalIndex.TryGetValue(stop.Id, out var sourceIndex);
                    var projectedPosition = ordered.Count + (startAnchor != null ? 1 : 0);
                    return new LocalChoiceCandidate
                    {
                        Stop = CloneStop(stop),
                        TravelMinutes = CalculateLegTravelMinutes(current, stop),
                        DistanceKm = CalculateDistanceKm(current, stop),
                        AttractionScore = stop.AttractionScore,
                        Displacement = Math.Abs(sourceIndex - projectedPosition),
                        Signature = stop.Id
                    };
                })
                .ToList();

            ApplyLocalChoiceScores(candidateChoices, profile);

            var next = candidateChoices
                .OrderBy(choice => choice, LocalChoiceComparer.Instance)
                .First()
                .Stop;

            ordered.Add(CloneStop(next));
            remaining.RemoveAll(stop => string.Equals(stop.Id, next.Id, StringComparison.Ordinal));
            current = next;
        }

        return ordered;
    }

    private List<TourRouteStop> ApplyIterativeTwoOpt(
        IReadOnlyList<TourRouteStop> candidateStops,
        bool hasFixedStart,
        bool hasFixedEnd,
        IReadOnlyList<TourRouteStop> originalDay,
        RouteOptimizationProfileDefinition profile)
    {
        var current = candidateStops.Select(CloneStop).ToList();
        var currentSignature = BuildSignature(current);
        var startIndex = hasFixedStart ? 1 : 0;
        var endIndexExclusive = hasFixedEnd ? current.Count - 1 : current.Count;

        for (var iteration = 0; iteration < MaxTwoOptIterations; iteration++)
        {
            var neighbours = new Dictionary<string, DayCandidate>(StringComparer.Ordinal)
            {
                [currentSignature] = EvaluateCandidate(originalDay, current)
            };

            for (var start = startIndex; start < endIndexExclusive - 1; start++)
            {
                for (var end = start + 1; end < endIndexExclusive; end++)
                {
                    var swapped = current.Select(CloneStop).ToList();
                    swapped.Reverse(start, end - start + 1);
                    RenumberDay(swapped, candidateStops.First().Day);
                    AddCandidate(neighbours, EvaluateCandidate(originalDay, swapped));
                }
            }

            ApplyObjectiveScores(neighbours.Values, profile);
            var best = neighbours.Values
                .OrderBy(candidate => candidate, DayCandidateComparer.Instance)
                .First();

            if (string.Equals(best.Signature, currentSignature, StringComparison.Ordinal))
            {
                break;
            }

            current = best.Stops.Select(CloneStop).ToList();
            currentSignature = best.Signature;
        }

        return current;
    }

    private static void AddCandidate(IDictionary<string, DayCandidate> candidates, DayCandidate candidate)
    {
        candidates[candidate.Signature] = candidate;
    }

    private static void ApplyLocalChoiceScores(
        IReadOnlyCollection<LocalChoiceCandidate> candidates,
        RouteOptimizationProfileDefinition profile)
    {
        if (candidates.Count == 0)
        {
            return;
        }

        var minTravel = candidates.Min(candidate => candidate.TravelMinutes);
        var maxTravel = candidates.Max(candidate => candidate.TravelMinutes);
        var minDistance = candidates.Min(candidate => candidate.DistanceKm);
        var maxDistance = candidates.Max(candidate => candidate.DistanceKm);
        var minAttraction = candidates.Min(candidate => candidate.AttractionScore);
        var maxAttraction = candidates.Max(candidate => candidate.AttractionScore);
        var minDisplacement = candidates.Min(candidate => candidate.Displacement);
        var maxDisplacement = candidates.Max(candidate => candidate.Displacement);

        foreach (var candidate in candidates)
        {
            var travelScore = NormalizeLowerIsBetter(candidate.TravelMinutes, minTravel, maxTravel);
            var distanceScore = NormalizeLowerIsBetter(candidate.DistanceKm, minDistance, maxDistance);
            var attractionScore = NormalizeHigherIsBetter(candidate.AttractionScore, minAttraction, maxAttraction);
            var stabilityScore = NormalizeLowerIsBetter(candidate.Displacement, minDisplacement, maxDisplacement);

            candidate.ObjectiveScore =
                (travelScore * profile.TravelWeight)
                + (distanceScore * profile.DistanceWeight)
                + (attractionScore * profile.AttractionWeight)
                + (stabilityScore * profile.StabilityWeight);
        }
    }

    private static void ApplyObjectiveScores(
        IEnumerable<DayCandidate> candidates,
        RouteOptimizationProfileDefinition profile)
    {
        var list = candidates.ToList();
        if (list.Count == 0)
        {
            return;
        }

        var minTravel = list.Min(candidate => candidate.Insight.TotalTravelMinutes);
        var maxTravel = list.Max(candidate => candidate.Insight.TotalTravelMinutes);
        var minDistance = list.Min(candidate => candidate.Insight.TotalDistanceKm);
        var maxDistance = list.Max(candidate => candidate.Insight.TotalDistanceKm);
        var minAttraction = list.Min(candidate => candidate.FrontLoadedAttractionUtility);
        var maxAttraction = list.Max(candidate => candidate.FrontLoadedAttractionUtility);
        var minDisplacement = list.Min(candidate => candidate.Displacement);
        var maxDisplacement = list.Max(candidate => candidate.Displacement);

        foreach (var candidate in list)
        {
            candidate.TravelMinutesScore = NormalizeLowerIsBetter(candidate.Insight.TotalTravelMinutes, minTravel, maxTravel);
            candidate.DistanceScore = NormalizeLowerIsBetter(candidate.Insight.TotalDistanceKm, minDistance, maxDistance);
            candidate.AttractionFrontloadScore = NormalizeHigherIsBetter(candidate.FrontLoadedAttractionUtility, minAttraction, maxAttraction);
            candidate.StabilityScore = NormalizeLowerIsBetter(candidate.Displacement, minDisplacement, maxDisplacement);

            candidate.ObjectiveScore =
                (candidate.TravelMinutesScore * profile.TravelWeight)
                + (candidate.DistanceScore * profile.DistanceWeight)
                + (candidate.AttractionFrontloadScore * profile.AttractionWeight)
                + (candidate.StabilityScore * profile.StabilityWeight);
        }
    }

    private static double NormalizeLowerIsBetter(double value, double min, double max)
    {
        if (max <= min)
        {
            return 1d;
        }

        return (max - value) / (max - min);
    }

    private static double NormalizeHigherIsBetter(double value, double min, double max)
    {
        if (max <= min)
        {
            return 1d;
        }

        return (value - min) / (max - min);
    }

    private DayCandidate EvaluateCandidate(IReadOnlyList<TourRouteStop> originalDay, IReadOnlyList<TourRouteStop> candidateStops)
    {
        var normalizedStops = candidateStops.Select(CloneStop).ToList();
        if (normalizedStops.Count > 0)
        {
            RenumberDay(normalizedStops, normalizedStops[0].Day);
        }

        var insight = BuildInsightForStops(null, normalizedStops);
        var originalIndex = originalDay
            .Select((stop, index) => new { stop.Id, index })
            .ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);

        var displacement = normalizedStops
            .Select((stop, index) =>
            {
                originalIndex.TryGetValue(stop.Id, out var sourceIndex);
                return Math.Abs(sourceIndex - index);
            })
            .Sum();

        var attractionUtility = normalizedStops
            .Select((stop, index) => stop.AttractionScore / (index + 1d))
            .Sum();

        return new DayCandidate
        {
            Stops = normalizedStops,
            Insight = insight,
            FrontLoadedAttractionUtility = attractionUtility,
            Displacement = displacement,
            Signature = BuildSignature(normalizedStops)
        };
    }

    private RouteInsightResult BuildInsightForStops(Tour? templateTour, IReadOnlyList<TourRouteStop> stops)
    {
        var day = stops.Count > 0 ? stops.Min(stop => stop.Day) : 1;
        var tour = CloneTour(templateTour ?? new Tour
        {
            Schedule = [new ScheduleItem { Day = day, Title = $"Ngày {day}", Description = string.Empty }]
        }, stops);

        return _routeInsightService.Build(tour);
    }

    private static List<TourRouteStop> BuildCandidateStops(TourRouteStop? startAnchor, IReadOnlyList<TourRouteStop> middleStops, TourRouteStop? endAnchor)
    {
        var candidate = new List<TourRouteStop>();

        if (startAnchor != null)
        {
            candidate.Add(CloneStop(startAnchor));
        }

        candidate.AddRange(middleStops.Select(CloneStop));

        if (endAnchor != null)
        {
            candidate.Add(CloneStop(endAnchor));
        }

        var day = startAnchor?.Day ?? middleStops.FirstOrDefault()?.Day ?? endAnchor?.Day ?? 1;
        RenumberDay(candidate, day);
        return candidate;
    }

    private static IEnumerable<IReadOnlyList<TourRouteStop>> EnumeratePermutations(List<TourRouteStop> items, int startIndex)
    {
        if (startIndex >= items.Count - 1)
        {
            yield return items.Select(CloneStop).ToList();
            yield break;
        }

        for (var index = startIndex; index < items.Count; index++)
        {
            (items[startIndex], items[index]) = (items[index], items[startIndex]);

            foreach (var permutation in EnumeratePermutations(items, startIndex + 1))
            {
                yield return permutation;
            }

            (items[startIndex], items[index]) = (items[index], items[startIndex]);
        }
    }

    private static string ResolveUnchangedReason(bool hasOptimizableDay, IReadOnlyCollection<RouteOptimizationWarning> warnings, int stopCount)
    {
        if (stopCount == 0)
        {
            return "Chưa có dữ liệu lộ trình hợp lệ để tối ưu.";
        }

        if (warnings.Count > 0 && !hasOptimizableDay)
        {
            return "Không có ngày nào đủ dữ liệu tọa độ để tối ưu.";
        }

        if (!hasOptimizableDay)
        {
            return "Không có ngày nào đủ điểm dừng để tối ưu.";
        }

        return "Current routing is already optimal for the selected profile.";
    }

    private static bool HaveSameSequence(IReadOnlyList<TourRouteStop> left, IReadOnlyList<TourRouteStop> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (!string.Equals(left[index].Id, right[index].Id, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildSignature(IEnumerable<TourRouteStop> stops)
    {
        return string.Join("|", stops.Select(stop => stop.Id));
    }

    private static void RenumberDay(IReadOnlyList<TourRouteStop> stops, int day)
    {
        for (var index = 0; index < stops.Count; index++)
        {
            stops[index].Day = day;
            stops[index].Order = index + 1;
        }
    }

    private static bool IsValidStop(TourRouteStop? stop)
    {
        return stop != null
               && stop.Day > 0
               && stop.Order > 0
               && !string.IsNullOrWhiteSpace(stop.Id)
               && !string.IsNullOrWhiteSpace(stop.Name);
    }

    private static bool MatchesAny(string? value, IEnumerable<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasCoordinatePair(GeoPoint? coordinates)
    {
        return coordinates?.Lat.HasValue == true && coordinates.Lng.HasValue;
    }

    private int CalculateLegTravelMinutes(TourRouteStop fromStop, TourRouteStop toStop)
    {
        var profile = _routeTravelEstimator.ResolveProfile(fromStop, toStop);
        return _routeTravelEstimator.Estimate(fromStop, toStop, profile, 8 * 60).TravelMinutes;
    }

    private double CalculateDistanceKm(TourRouteStop fromStop, TourRouteStop toStop)
    {
        var profile = _routeTravelEstimator.ResolveProfile(fromStop, toStop);
        return _routeTravelEstimator.Estimate(fromStop, toStop, profile, 8 * 60).DistanceKm;
    }

    private static Tour CloneTour(Tour template, IReadOnlyList<TourRouteStop> stops)
    {
        var schedule = (template.Schedule ?? new List<ScheduleItem>())
            .Select(item => new ScheduleItem
            {
                Day = item.Day,
                Title = item.Title,
                Description = item.Description,
                Activities = item.Activities?.ToList() ?? new List<string>()
            })
            .ToList();

        if (schedule.Count == 0 && stops.Count > 0)
        {
            schedule = stops
                .GroupBy(stop => stop.Day)
                .OrderBy(group => group.Key)
                .Select(group => new ScheduleItem
                {
                    Day = group.Key,
                    Title = $"Day {group.Key}",
                    Description = string.Empty,
                    Activities = new List<string>()
                })
                .ToList();
        }

        return new Tour
        {
            Id = template.Id,
            Name = template.Name,
            Code = template.Code,
            Description = template.Description,
            ShortDescription = template.ShortDescription,
            Status = template.Status,
            Destination = template.Destination,
            Price = template.Price,
            Duration = template.Duration,
            Schedule = schedule,
            Routing = new TourRouting
            {
                SchemaVersion = 1,
                Stops = stops.Select(CloneStop).ToList()
            }
        };
    }

    private static TourRouteStop CloneStop(TourRouteStop stop)
    {
        return new TourRouteStop
        {
            Id = stop.Id,
            Day = stop.Day,
            Order = stop.Order,
            Name = stop.Name,
            Type = stop.Type,
            VisitMinutes = stop.VisitMinutes,
            AttractionScore = stop.AttractionScore,
            Note = stop.Note,
            Coordinates = new GeoPoint
            {
                Lat = stop.Coordinates?.Lat,
                Lng = stop.Coordinates?.Lng
            }
        };
    }

    private static RouteOptimizationStopPreview ToPreview(TourRouteStop stop)
    {
        return new RouteOptimizationStopPreview
        {
            ClientKey = stop.Id,
            Day = stop.Day,
            Order = stop.Order,
            Name = stop.Name,
            Type = stop.Type,
            VisitMinutes = stop.VisitMinutes,
            AttractionScore = stop.AttractionScore,
            Note = stop.Note
        };
    }

    private static RouteOptimizationResult BuildEmptyResult(RouteOptimizationProfileDefinition profile, string unchangedReason)
    {
        return new RouteOptimizationResult
        {
            Profile = profile.Id,
            ProfileLabel = profile.Label,
            ProfileDescription = profile.Description,
            CurrentInsight = RouteInsightResult.Empty,
            SuggestedInsight = RouteInsightResult.Empty,
            UnchangedReason = unchangedReason
        };
    }

    private static RouteOptimizationProfileDefinition ResolveProfile(RouteOptimizationRequest? request)
    {
        return RouteOptimizationProfiles.Normalize(request?.Profile) switch
        {
            RouteOptimizationProfiles.DistanceFirst => new RouteOptimizationProfileDefinition(
                RouteOptimizationProfiles.DistanceFirst,
                RouteOptimizationProfiles.GetLabel(RouteOptimizationProfiles.DistanceFirst),
                RouteOptimizationProfiles.GetDescription(RouteOptimizationProfiles.DistanceFirst),
                travelWeight: 0.30d,
                distanceWeight: 0.45d,
                attractionWeight: 0.10d,
                stabilityWeight: 0.15d),
            RouteOptimizationProfiles.HighlightsFirst => new RouteOptimizationProfileDefinition(
                RouteOptimizationProfiles.HighlightsFirst,
                RouteOptimizationProfiles.GetLabel(RouteOptimizationProfiles.HighlightsFirst),
                RouteOptimizationProfiles.GetDescription(RouteOptimizationProfiles.HighlightsFirst),
                travelWeight: 0.25d,
                distanceWeight: 0.10d,
                attractionWeight: 0.45d,
                stabilityWeight: 0.20d),
            _ => new RouteOptimizationProfileDefinition(
                RouteOptimizationProfiles.Balanced,
                RouteOptimizationProfiles.GetLabel(RouteOptimizationProfiles.Balanced),
                RouteOptimizationProfiles.GetDescription(RouteOptimizationProfiles.Balanced),
                travelWeight: 0.40d,
                distanceWeight: 0.25d,
                attractionWeight: 0.25d,
                stabilityWeight: 0.10d)
        };
    }

    private sealed class DayOptimizationOutcome
    {
        public bool CanOptimize { get; set; }

        public bool Changed { get; set; }

        public double CurrentObjectiveScore { get; set; }

        public double SuggestedObjectiveScore { get; set; }

        public DayCandidate CurrentCandidate { get; set; } = new();

        public DayCandidate SuggestedCandidate { get; set; } = new();
    }

    private sealed class RouteOptimizationProfileDefinition
    {
        public RouteOptimizationProfileDefinition(
            string id,
            string label,
            string description,
            double travelWeight,
            double distanceWeight,
            double attractionWeight,
            double stabilityWeight)
        {
            Id = id;
            Label = label;
            Description = description;
            TravelWeight = travelWeight;
            DistanceWeight = distanceWeight;
            AttractionWeight = attractionWeight;
            StabilityWeight = stabilityWeight;
        }

        public string Id { get; }

        public string Label { get; }

        public string Description { get; }

        public double TravelWeight { get; }

        public double DistanceWeight { get; }

        public double AttractionWeight { get; }

        public double StabilityWeight { get; }
    }

    private sealed class DayCandidate
    {
        public List<TourRouteStop> Stops { get; set; } = new();

        public RouteInsightResult Insight { get; set; } = RouteInsightResult.Empty;

        public double FrontLoadedAttractionUtility { get; set; }

        public int Displacement { get; set; }

        public double TravelMinutesScore { get; set; }

        public double DistanceScore { get; set; }

        public double AttractionFrontloadScore { get; set; }

        public double StabilityScore { get; set; }

        public double ObjectiveScore { get; set; }

        public string Signature { get; set; } = string.Empty;
    }

    private sealed class LocalChoiceCandidate
    {
        public TourRouteStop Stop { get; set; } = new();

        public int TravelMinutes { get; set; }

        public double DistanceKm { get; set; }

        public double AttractionScore { get; set; }

        public int Displacement { get; set; }

        public double ObjectiveScore { get; set; }

        public string Signature { get; set; } = string.Empty;
    }

    private sealed class LocalChoiceComparer : IComparer<LocalChoiceCandidate>
    {
        public static LocalChoiceComparer Instance { get; } = new();

        public int Compare(LocalChoiceCandidate? left, LocalChoiceCandidate? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var compare = right.ObjectiveScore.CompareTo(left.ObjectiveScore);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.TravelMinutes.CompareTo(right.TravelMinutes);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.DistanceKm.CompareTo(right.DistanceKm);
            if (compare != 0)
            {
                return compare;
            }

            compare = right.AttractionScore.CompareTo(left.AttractionScore);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.Displacement.CompareTo(right.Displacement);
            if (compare != 0)
            {
                return compare;
            }

            return string.CompareOrdinal(left.Signature, right.Signature);
        }
    }

    private sealed class DayCandidateComparer : IComparer<DayCandidate>
    {
        public static DayCandidateComparer Instance { get; } = new();

        public int Compare(DayCandidate? left, DayCandidate? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var compare = right.ObjectiveScore.CompareTo(left.ObjectiveScore);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.Insight.TotalTravelMinutes.CompareTo(right.Insight.TotalTravelMinutes);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.Insight.TotalDistanceKm.CompareTo(right.Insight.TotalDistanceKm);
            if (compare != 0)
            {
                return compare;
            }

            compare = right.FrontLoadedAttractionUtility.CompareTo(left.FrontLoadedAttractionUtility);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.Displacement.CompareTo(right.Displacement);
            if (compare != 0)
            {
                return compare;
            }

            return string.CompareOrdinal(left.Signature, right.Signature);
        }
    }
}
