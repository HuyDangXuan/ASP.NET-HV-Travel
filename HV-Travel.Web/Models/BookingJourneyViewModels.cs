using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HVTravel.Web.Models;

public enum BookingJourneyStage
{
    Build,
    Payment,
    Success,
    Failed,
    Lookup,
    Consultation,
    Error
}

public class BookingJourneyPageVm
{
    public string StageKey { get; set; } = string.Empty;
    public string Eyebrow { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StageIndex { get; set; }
    public bool ShowStageBar { get; set; } = true;
    public BookingJourneyStatusVm Status { get; set; } = new();
    public BookingSummaryVm Summary { get; set; } = new();
    public BookingTimelineVm Timeline { get; set; } = new();
    public BookingSupportVm Support { get; set; } = new();
    public IReadOnlyList<BookingPaymentMethodVm> PaymentMethods { get; set; } = Array.Empty<BookingPaymentMethodVm>();
}

public class BookingJourneyStatusVm
{
    public string Variant { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string Eyebrow { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public IReadOnlyList<BookingJourneyActionVm> Actions { get; set; } = Array.Empty<BookingJourneyActionVm>();
}

public class BookingSummaryVm
{
    public string Eyebrow { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PrimaryAmount { get; set; } = string.Empty;
    public string SecondaryAmount { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public IReadOnlyList<BookingSummaryRowVm> Rows { get; set; } = Array.Empty<BookingSummaryRowVm>();
}

public class BookingSummaryRowVm
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Emphasized { get; set; }
}

public class BookingTimelineVm
{
    public string Title { get; set; } = string.Empty;
    public string EmptyText { get; set; } = string.Empty;
    public IReadOnlyList<BookingTimelineItemVm> Items { get; set; } = Array.Empty<BookingTimelineItemVm>();
}

public class BookingTimelineItemVm
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Meta { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public class BookingSupportVm
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SecondaryNote { get; set; } = string.Empty;
    public IReadOnlyList<BookingJourneyActionVm> Actions { get; set; } = Array.Empty<BookingJourneyActionVm>();
}

public class BookingJourneyActionVm
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Tone { get; set; } = "secondary";
    public bool IsExternal { get; set; }
}

public class BookingPaymentMethodVm
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BodyTitle { get; set; } = string.Empty;
    public string BodyDescription { get; set; } = string.Empty;
    public string CTA { get; set; } = string.Empty;
    public string Accent { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class PublicTourDetailsPageViewModel
{
    public HVTravel.Domain.Entities.Tour Tour { get; set; } = new();
    public IReadOnlyList<HVTravel.Domain.Entities.Tour> RelatedTours { get; set; } = Array.Empty<HVTravel.Domain.Entities.Tour>();
    public PublicTourDossierViewModel Dossier { get; set; } = new();
}

public class PublicTourDossierViewModel
{
    public string Eyebrow { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DestinationText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string NextDepartureText { get; set; } = string.Empty;
    public string MeetingPointText { get; set; } = string.Empty;
    public string StartingPriceDisplay { get; set; } = string.Empty;
    public string ConfirmationText { get; set; } = string.Empty;
    public string RatingsText { get; set; } = string.Empty;
    public string? PrimaryDepartureId { get; set; }
    public string? PrimaryDepartureStartDateValue { get; set; }
    public IReadOnlyList<string> GalleryImages { get; set; } = Array.Empty<string>();
    public IReadOnlyList<PublicTourFactVm> Facts { get; set; } = Array.Empty<PublicTourFactVm>();
    public IReadOnlyList<PublicTourDepartureCardVm> Departures { get; set; } = Array.Empty<PublicTourDepartureCardVm>();
}

public class PublicTourFactVm
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public class PublicTourDepartureCardVm
{
    public string Id { get; set; } = string.Empty;
    public string StartDateText { get; set; } = string.Empty;
    public string StartDateValue { get; set; } = string.Empty;
    public string ConfirmationText { get; set; } = string.Empty;
    public string RemainingCapacityText { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public bool LowAvailability { get; set; }
}
