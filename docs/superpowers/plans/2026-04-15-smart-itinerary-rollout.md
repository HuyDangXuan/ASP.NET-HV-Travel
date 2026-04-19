# Smart Itinerary Rollout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add route-aware itinerary recommendation to the existing tour booking platform in an order that produces user-visible value early without forcing premature map/traffic integration.

**Architecture:** Extend the existing `Tour` aggregate with optional structured stops and points of interest, then layer a read-only recommendation service on top before introducing optimization algorithms. Keep heuristic scoring, TSP solving, and ETA estimation in dedicated services so the public site, admin area, and AI tour chat can consume the same backend outputs.

**Tech Stack:** ASP.NET Core 8 MVC, MongoDB, existing Domain/Application/Infrastructure/Web layers, xUnit, optional external map provider in the final phase

---

## Rollout Order

1. Smart itinerary data foundation
2. Public recommendation experience v1
3. Multi-criteria scoring engine
4. TSP-based route optimizer
5. ETA and traffic calibration
6. AI chat and admin operationalization

## Why This Order

- The current codebase already supports fixed tours, schedule text, tour search, and AI chat, but does not store coordinates, stop durations, or route matrices.
- A route engine without structured stop data will be guesswork.
- A traffic-aware ETA model without a working recommendation pipeline will delay delivery and increase integration risk.
- The fastest path to real product value is to first make itinerary recommendations work from tour-owned data, then increase algorithmic sophistication behind the same API.

## Target File Structure

**Domain**
- Create: `HV-Travel.Domain/Entities/Tour.Routing.cs`
- Create: `HV-Travel.Domain/Models/ItineraryRecommendationModels.cs`
- Modify: `HV-Travel.Domain/Interfaces/ITourRepository.cs`

**Application**
- Create: `HV-Travel.Application/Interfaces/IItineraryRecommendationService.cs`
- Create: `HV-Travel.Application/Interfaces/IRouteOptimizationService.cs`
- Create: `HV-Travel.Application/Interfaces/ITravelTimeEstimator.cs`
- Create: `HV-Travel.Application/Services/ItineraryRecommendationService.cs`
- Create: `HV-Travel.Application/Services/RouteScoringService.cs`
- Create: `HV-Travel.Application/Services/RouteOptimizationService.cs`
- Modify: `HV-Travel.Application/DependencyInjection.cs`

**Infrastructure**
- Create: `HV-Travel.Infrastructure/Services/FallbackTravelTimeEstimator.cs`
- Create: `HV-Travel.Infrastructure/Services/MapProviderTravelTimeEstimator.cs`
- Create: `HV-Travel.Infrastructure/Options/MapProviderOptions.cs`
- Modify: `HV-Travel.Infrastructure/Data/DbInitializer.cs`

**Web**
- Create: `HV-Travel.Web/Controllers/ItineraryController.cs`
- Create: `HV-Travel.Web/Models/ItineraryViewModels.cs`
- Modify: `HV-Travel.Web/Controllers/PublicToursController.cs`
- Modify: `HV-Travel.Web/Areas/Admin/Controllers/ToursController.cs`
- Modify: `HV-Travel.Web/Views/PublicTours/Details.cshtml`
- Modify: `HV-Travel.Web/Services/TourAiChatService.cs`
- Modify: `HV-Travel.Web/Program.cs`
- Modify: `HV-Travel.Web/appsettings.json`

**Tests**
- Create: `HV-Travel.Web.Tests/ItineraryRecommendationServiceTests.cs`
- Create: `HV-Travel.Web.Tests/RouteOptimizationServiceTests.cs`
- Create: `HV-Travel.Web.Tests/TravelTimeEstimatorTests.cs`
- Create: `HV-Travel.Web.Tests/PublicTourItineraryMarkupTests.cs`

### Task 1: Add Structured Route Data To Tour

**Files:**
- Create: `HV-Travel.Domain/Entities/Tour.Routing.cs`
- Modify: `HV-Travel.Infrastructure/Data/DbInitializer.cs`
- Test: `HV-Travel.Web.Tests/ItineraryRecommendationServiceTests.cs`

- [ ] Add optional route-ready models to `Tour`: geo point, stop/POI, visit duration, opening window, attractiveness score, transport mode, manual ordering hints.
- [ ] Keep existing `Schedule` text untouched so current booking and public pages continue to work.
- [ ] Seed 1-2 tours with structured stops matching existing destinations so the feature can be demoed immediately.
- [ ] Add tests that verify tours without route data still behave normally.
- [ ] Run: `dotnet test "HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj" --filter "ItineraryRecommendationServiceTests"`

### Task 2: Ship Recommendation Experience V1 Without External Maps

**Files:**
- Create: `HV-Travel.Domain/Models/ItineraryRecommendationModels.cs`
- Create: `HV-Travel.Application/Interfaces/IItineraryRecommendationService.cs`
- Create: `HV-Travel.Application/Services/ItineraryRecommendationService.cs`
- Create: `HV-Travel.Infrastructure/Services/FallbackTravelTimeEstimator.cs`
- Create: `HV-Travel.Web/Controllers/ItineraryController.cs`
- Create: `HV-Travel.Web/Models/ItineraryViewModels.cs`
- Modify: `HV-Travel.Web/Views/PublicTours/Details.cshtml`
- Modify: `HV-Travel.Web/Program.cs`
- Test: `HV-Travel.Web.Tests/PublicTourItineraryMarkupTests.cs`

- [ ] Build a recommendation endpoint that returns a suggested visit order from structured stop data only.
- [ ] Start with deterministic heuristics: preserve admin order, enforce day/time budget, and drop low-value stops when over budget.
- [ ] Surface the recommendation on the public tour detail page as a read-only “gợi ý tham quan” block.
- [ ] Add markup tests to guarantee the block renders only when a tour has structured route data.
- [ ] Run: `dotnet test "HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj" --filter "PublicTourItineraryMarkupTests|ItineraryRecommendationServiceTests"`

### Task 3: Introduce Multi-Criteria Scoring Engine

**Files:**
- Create: `HV-Travel.Application/Services/RouteScoringService.cs`
- Modify: `HV-Travel.Application/Services/ItineraryRecommendationService.cs`
- Test: `HV-Travel.Web.Tests/ItineraryRecommendationServiceTests.cs`

- [ ] Add weighted scoring across distance estimate, travel time estimate, attraction score, and remaining time budget.
- [ ] Keep weights configurable per tour or global default, but start with one sane default profile.
- [ ] Make recommendation responses explain why a stop was included, delayed, or removed so UI and AI chat can reuse the explanation.
- [ ] Add tests for ties, missing attraction score, and time-budget overflow.
- [ ] Run: `dotnet test "HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj" --filter "ItineraryRecommendationServiceTests"`

### Task 4: Add TSP-Based Optimization Behind A Small-N Boundary

**Files:**
- Create: `HV-Travel.Application/Interfaces/IRouteOptimizationService.cs`
- Create: `HV-Travel.Application/Services/RouteOptimizationService.cs`
- Modify: `HV-Travel.Application/Services/ItineraryRecommendationService.cs`
- Test: `HV-Travel.Web.Tests/RouteOptimizationServiceTests.cs`

- [ ] Use the current heuristic recommendation as the baseline candidate route.
- [ ] Add TSP optimization only when the candidate stop count is small enough to keep latency predictable.
- [ ] Keep a hard fallback to heuristic ordering when data is sparse or stop count is large.
- [ ] Add tests comparing optimized vs fallback behavior and verifying no regression when no optimization should run.
- [ ] Run: `dotnet test "HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj" --filter "RouteOptimizationServiceTests|ItineraryRecommendationServiceTests"`

### Task 5: Integrate ETA Estimation And Traffic Calibration Last

**Files:**
- Create: `HV-Travel.Application/Interfaces/ITravelTimeEstimator.cs`
- Create: `HV-Travel.Infrastructure/Services/MapProviderTravelTimeEstimator.cs`
- Create: `HV-Travel.Infrastructure/Options/MapProviderOptions.cs`
- Modify: `HV-Travel.Web/appsettings.json`
- Modify: `HV-Travel.Web/Program.cs`
- Test: `HV-Travel.Web.Tests/TravelTimeEstimatorTests.cs`

- [ ] Add an estimator abstraction so the system can switch between fallback heuristic time and real provider-backed time.
- [ ] Start with provider integration as optional configuration, not a hard dependency.
- [ ] Only after provider data is stable, add correction factors for time-of-day and intersection delay.
- [ ] Add tests for missing API config, provider failure, cache hit behavior, and fallback behavior.
- [ ] Run: `dotnet test "HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj" --filter "TravelTimeEstimatorTests|RouteOptimizationServiceTests"`

### Task 6: Wire Results Into AI Chat And Admin Operations

**Files:**
- Modify: `HV-Travel.Web/Services/TourAiChatService.cs`
- Modify: `HV-Travel.Web/Areas/Admin/Controllers/ToursController.cs`
- Modify: `HV-Travel.Web/Views/PublicTours/Details.cshtml`
- Test: `HV-Travel.Web.Tests/PublicTourItineraryMarkupTests.cs`

- [ ] Update AI tour chat context so it can mention recommended order, estimated visit time, and why some stops are deprioritized.
- [ ] Add admin inputs for route stops and attraction score only after the public read-only experience is proven useful.
- [ ] Keep current schedule editing flow backward-compatible; route data must be additive, not mandatory.
- [ ] Add regression tests so tours with no structured route data still render the current detail experience.
- [ ] Run: `dotnet test "HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj" --filter "PublicTourItineraryMarkupTests|ItineraryRecommendationServiceTests"`

## Release Gates

- **Gate 1:** At least one seeded tour can render a structured itinerary recommendation without any external map provider.
- **Gate 2:** Recommendation output is explainable and stable enough to expose publicly.
- **Gate 3:** TSP optimization never blocks or slows normal page load for large tours.
- **Gate 4:** Provider-backed ETA can be disabled without breaking recommendation generation.

## Practical Product Mapping

- **Module 3 should ship first:** smart itinerary recommendation from structured tour data.
- **Module 4 should ship second:** multi-criteria optimization that improves recommendation quality.
- **Module 1 should ship third:** TSP as an optimization layer, not as the product entry point.
- **Module 2 should ship last:** ETA and traffic refinement after route data and optimization are already working.

## Non-Goals For Initial Rollout

- Full real-time city traffic simulation
- Dynamic rerouting during an active tour
- Large-scale map search across arbitrary public POIs
- Mandatory geo data for every legacy tour

## Verification Checklist

- [ ] Legacy tours without geo data still load in admin and public pages
- [ ] Recommendation API returns deterministic output for the same seeded input
- [ ] Public detail page shows recommendation only when data exists
- [ ] AI chat stays truthful and does not invent route timing when estimator data is missing
- [ ] Provider-backed ETA can fail safely and fall back to heuristic time
