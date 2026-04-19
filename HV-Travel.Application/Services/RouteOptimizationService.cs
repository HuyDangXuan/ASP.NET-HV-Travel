using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class RouteOptimizationService : IRouteOptimizationService
{
    private const int BruteForceLimit = 7;
    private static readonly string[] StartAnchorKeywords = ["meeting", "pickup", "checkin", "departure"];
    private static readonly string[] EndAnchorKeywords = ["hotel", "dropoff", "return", "checkout"];

    private readonly IRouteInsightService _routeInsightService;

    public RouteOptimizationService()
        : this(new RouteInsightService())
    {
    }

    public RouteOptimizationService(IRouteInsightService routeInsightService)
    {
        _routeInsightService = routeInsightService;
    }

    public RouteOptimizationResult Optimize(Tour? tour)
    {
        var orderedStops = (tour?.Routing?.Stops ?? new List<TourRouteStop>())
            .Where(IsValidStop)
            .OrderBy(stop => stop.Day)
            .ThenBy(stop => stop.Order)
            .Select(CloneStop)
            .ToList();

        if (tour == null || orderedStops.Count == 0)
        {
            return new RouteOptimizationResult
            {
                CurrentInsight = RouteInsightResult.Empty,
                SuggestedInsight = RouteInsightResult.Empty,
                UnchangedReason = "No valid routing data to optimize."
            };
        }

        var currentTour = CloneTour(tour, orderedStops);
        var currentInsight = _routeInsightService.Build(currentTour);

        var warnings = new List<RouteOptimizationWarning>();
        var dayResults = new List<RouteOptimizationDayResult>();
        var suggestedStops = new List<TourRouteStop>();
        var hasOptimizableDay = false;
        var hasChangedDay = false;

        foreach (var dayGroup in orderedStops.GroupBy(stop => stop.Day).OrderBy(group => group.Key))
        {
            var dayStops = dayGroup
                .OrderBy(stop => stop.Order)
                .Select(CloneStop)
                .ToList();

            var optimizedDay = OptimizeDay(dayStops, warnings, out var dayCanOptimize, out var dayChanged);
            hasOptimizableDay |= dayCanOptimize;
            hasChangedDay |= dayChanged;

            suggestedStops.AddRange(optimizedDay.Select(CloneStop));

            var currentDayInsight = BuildInsightForStops(tour, dayStops);
            var suggestedDayInsight = BuildInsightForStops(tour, optimizedDay);

            dayResults.Add(new RouteOptimizationDayResult
            {
                Day = dayGroup.Key,
                Changed = dayChanged,
                StopCount = dayStops.Count,
                CurrentTravelMinutes = currentDayInsight.TotalTravelMinutes,
                SuggestedTravelMinutes = suggestedDayInsight.TotalTravelMinutes,
                CurrentJourneyMinutes = currentDayInsight.TotalJourneyMinutes,
                SuggestedJourneyMinutes = suggestedDayInsight.TotalJourneyMinutes,
                CurrentDistanceKm = currentDayInsight.TotalDistanceKm,
                SuggestedDistanceKm = suggestedDayInsight.TotalDistanceKm,
                CurrentStops = dayStops.Select(ToPreview).ToList(),
                SuggestedStops = optimizedDay.Select(ToPreview).ToList()
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
            CurrentInsight = currentInsight,
            SuggestedInsight = suggestedInsight,
            Assignments = assignments,
            Days = dayResults,
            Warnings = warnings,
            UnchangedReason = hasChangedDay ? string.Empty : ResolveUnchangedReason(hasOptimizableDay, warnings, orderedStops.Count)
        };
    }

    private List<TourRouteStop> OptimizeDay(
        IReadOnlyList<TourRouteStop> dayStops,
        ICollection<RouteOptimizationWarning> warnings,
        out bool canOptimizeDay,
        out bool changed)
    {
        var original = dayStops
            .OrderBy(stop => stop.Order)
            .Select(CloneStop)
            .ToList();

        canOptimizeDay = false;
        changed = false;

        if (original.Count < 3)
        {
            return original;
        }

        if (original.Any(stop => !HasCoordinatePair(stop.Coordinates)))
        {
            warnings.Add(new RouteOptimizationWarning
            {
                Code = "missing_coordinates",
                Day = original[0].Day,
                ClientKey = original.FirstOrDefault(stop => !HasCoordinatePair(stop.Coordinates))?.Id ?? string.Empty,
                Message = $"Day {original[0].Day} was skipped because at least one stop is missing coordinates."
            });
            return original;
        }

        var startAnchor = original.FirstOrDefault(stop => MatchesAny(stop.Type, StartAnchorKeywords));
        var endAnchor = original.LastOrDefault(stop => MatchesAny(stop.Type, EndAnchorKeywords) && !ReferenceEquals(stop, startAnchor));

        var optimizable = original
            .Where(stop => !ReferenceEquals(stop, startAnchor) && !ReferenceEquals(stop, endAnchor))
            .Select(CloneStop)
            .ToList();

        if (optimizable.Count < 2)
        {
            return original;
        }

        canOptimizeDay = true;

        List<TourRouteStop> bestDay;
        if (optimizable.Count <= BruteForceLimit)
        {
            bestDay = FindBestBruteForceDay(original, startAnchor, endAnchor, optimizable);
        }
        else
        {
            bestDay = FindBestHeuristicDay(original, startAnchor, endAnchor, optimizable);
        }

        RenumberDay(bestDay, original[0].Day);
        changed = !HaveSameSequence(original, bestDay);
        return bestDay;
    }

    private List<TourRouteStop> FindBestBruteForceDay(
        IReadOnlyList<TourRouteStop> originalDay,
        TourRouteStop? startAnchor,
        TourRouteStop? endAnchor,
        IReadOnlyList<TourRouteStop> optimizableStops)
    {
        DayCandidate? bestCandidate = null;

        foreach (var permutation in EnumeratePermutations(optimizableStops.ToList(), 0))
        {
            var candidateStops = BuildCandidateStops(startAnchor, permutation, endAnchor);
            var candidate = EvaluateCandidate(originalDay, candidateStops);
            if (IsBetterCandidate(candidate, bestCandidate))
            {
                bestCandidate = candidate;
            }
        }

        return bestCandidate?.Stops.Select(CloneStop).ToList()
            ?? originalDay.Select(CloneStop).ToList();
    }

    private List<TourRouteStop> FindBestHeuristicDay(
        IReadOnlyList<TourRouteStop> originalDay,
        TourRouteStop? startAnchor,
        TourRouteStop? endAnchor,
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
            remaining.Remove(next);
            current = next;
        }

        var dayCandidate = BuildCandidateStops(startAnchor, ordered, endAnchor);
        var improved = ApplySinglePassTwoOpt(dayCandidate, startAnchor != null, endAnchor != null);
        var initialEvaluation = EvaluateCandidate(originalDay, dayCandidate);
        var improvedEvaluation = EvaluateCandidate(originalDay, improved);

        return IsBetterCandidate(improvedEvaluation, initialEvaluation)
            ? improvedEvaluation.Stops.Select(CloneStop).ToList()
            : initialEvaluation.Stops.Select(CloneStop).ToList();
    }

    private List<TourRouteStop> ApplySinglePassTwoOpt(IReadOnlyList<TourRouteStop> candidateStops, bool hasFixedStart, bool hasFixedEnd)
    {
        var best = candidateStops.Select(CloneStop).ToList();
        var bestEvaluation = EvaluateCandidate(best, best);
        var startIndex = hasFixedStart ? 1 : 0;
        var endIndexExclusive = hasFixedEnd ? best.Count - 1 : best.Count;

        for (var start = startIndex; start < endIndexExclusive - 1; start++)
        {
            for (var end = start + 1; end < endIndexExclusive; end++)
            {
                var candidate = best.Select(CloneStop).ToList();
                candidate.Reverse(start, end - start + 1);
                RenumberDay(candidate, candidateStops.First().Day);

                var evaluation = EvaluateCandidate(best, candidate);
                if (IsBetterCandidate(evaluation, bestEvaluation))
                {
                    best = candidate;
                    bestEvaluation = evaluation;
                }
            }
        }

        return best;
    }

    private DayCandidate EvaluateCandidate(IReadOnlyList<TourRouteStop> originalDay, IReadOnlyList<TourRouteStop> candidateStops)
    {
        var insight = BuildInsightForStops(null, candidateStops);
        var originalIndex = originalDay
            .Select((stop, index) => new { stop.Id, index })
            .ToDictionary(item => item.Id, item => item.index, StringComparer.Ordinal);

        var displacement = candidateStops
            .Select((stop, index) =>
            {
                originalIndex.TryGetValue(stop.Id, out var originalPosition);
                return Math.Abs(originalPosition - index);
            })
            .Sum();

        var attractionUtility = candidateStops
            .Select((stop, index) => stop.AttractionScore / (index + 1d))
            .Sum();

        return new DayCandidate
        {
            Stops = candidateStops.Select(CloneStop).ToList(),
            Insight = insight,
            FrontLoadedAttractionUtility = attractionUtility,
            Displacement = displacement
        };
    }

    private static bool IsBetterCandidate(DayCandidate candidate, DayCandidate? bestCandidate)
    {
        if (bestCandidate == null)
        {
            return true;
        }

        var current = candidate.Insight;
        var best = bestCandidate.Insight;

        var compare = current.TotalTravelMinutes.CompareTo(best.TotalTravelMinutes);
        if (compare != 0)
        {
            return compare < 0;
        }

        compare = current.TotalDistanceKm.CompareTo(best.TotalDistanceKm);
        if (compare != 0)
        {
            return compare < 0;
        }

        compare = bestCandidate.FrontLoadedAttractionUtility.CompareTo(candidate.FrontLoadedAttractionUtility);
        if (compare != 0)
        {
            return compare > 0;
        }

        compare = candidate.Displacement.CompareTo(bestCandidate.Displacement);
        if (compare != 0)
        {
            return compare < 0;
        }

        return string.CompareOrdinal(
                   string.Join("|", candidate.Stops.Select(stop => stop.Id)),
                   string.Join("|", bestCandidate.Stops.Select(stop => stop.Id))) < 0;
    }

    private RouteInsightResult BuildInsightForStops(Tour? templateTour, IReadOnlyList<TourRouteStop> stops)
    {
        var day = stops.Count > 0 ? stops.Min(stop => stop.Day) : 1;
        var tour = CloneTour(templateTour ?? new Tour
        {
            Schedule = [new ScheduleItem { Day = day, Title = $"Day {day}", Description = string.Empty }]
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
            return "No valid routing data to optimize.";
        }

        if (warnings.Count > 0 && !hasOptimizableDay)
        {
            return "No route day had enough coordinate data for optimization.";
        }

        if (!hasOptimizableDay)
        {
            return "No route day had enough stops to optimize.";
        }

        return "Current routing is already optimal for the configured heuristic.";
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

    private static int CalculateLegTravelMinutes(TourRouteStop fromStop, TourRouteStop toStop)
    {
        var profile = ResolveProfile(toStop.Type, fromStop.Type);
        var speedKmPerHour = profile switch
        {
            "urban" => 24d,
            "scenic" => 32d,
            _ => 28d
        };

        var junctionDelayMinutes = profile switch
        {
            "urban" => 6,
            "scenic" => 3,
            _ => 4
        };

        var distance = CalculateDistanceKm(fromStop, toStop);
        var driveMinutes = Math.Max(5, (int)Math.Round((distance / speedKmPerHour) * 60d, MidpointRounding.AwayFromZero));
        return driveMinutes + junctionDelayMinutes;
    }

    private static string ResolveProfile(string? primaryType, string? fallbackType)
    {
        var type = !string.IsNullOrWhiteSpace(primaryType) ? primaryType : fallbackType;
        if (string.IsNullOrWhiteSpace(type))
        {
            return "default";
        }

        if (type.Contains("market", StringComparison.OrdinalIgnoreCase)
            || type.Contains("city", StringComparison.OrdinalIgnoreCase)
            || type.Contains("museum", StringComparison.OrdinalIgnoreCase)
            || type.Contains("landmark", StringComparison.OrdinalIgnoreCase)
            || type.Contains("meeting", StringComparison.OrdinalIgnoreCase))
        {
            return "urban";
        }

        if (type.Contains("viewpoint", StringComparison.OrdinalIgnoreCase)
            || type.Contains("beach", StringComparison.OrdinalIgnoreCase)
            || type.Contains("park", StringComparison.OrdinalIgnoreCase)
            || type.Contains("forest", StringComparison.OrdinalIgnoreCase)
            || type.Contains("lake", StringComparison.OrdinalIgnoreCase))
        {
            return "scenic";
        }

        return "default";
    }

    private static double CalculateDistanceKm(TourRouteStop fromStop, TourRouteStop toStop)
    {
        if (!HasCoordinatePair(fromStop.Coordinates) || !HasCoordinatePair(toStop.Coordinates))
        {
            return 0d;
        }

        var earthRadiusKm = 6371d;
        var latDistance = DegreesToRadians(toStop.Coordinates.Lat!.Value - fromStop.Coordinates.Lat!.Value);
        var lngDistance = DegreesToRadians(toStop.Coordinates.Lng!.Value - fromStop.Coordinates.Lng!.Value);
        var a = Math.Pow(Math.Sin(latDistance / 2d), 2d)
                + Math.Cos(DegreesToRadians(fromStop.Coordinates.Lat.Value))
                * Math.Cos(DegreesToRadians(toStop.Coordinates.Lat.Value))
                * Math.Pow(Math.Sin(lngDistance / 2d), 2d);

        var c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180d);
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

    private sealed class DayCandidate
    {
        public List<TourRouteStop> Stops { get; set; } = new();

        public RouteInsightResult Insight { get; set; } = RouteInsightResult.Empty;

        public double FrontLoadedAttractionUtility { get; set; }

        public int Displacement { get; set; }
    }
}
