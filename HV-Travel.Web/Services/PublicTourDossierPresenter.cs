using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public class PublicTourDossierPresenter
{
    public PublicTourDetailsPageViewModel Build(Tour tour, IEnumerable<Tour> relatedTours)
    {
        var departures = ((tour.Departures != null && tour.Departures.Any()) ? tour.Departures : tour.EffectiveDepartures)
            .OrderBy(item => item.StartDate)
            .ToList();
        var featuredDeparture = departures.FirstOrDefault();
        var startingPrice = departures.Any()
            ? departures.Min(item => item.AdultPrice > 0m ? item.AdultPrice : tour.Price?.Adult ?? 0m)
            : tour.Price?.Adult ?? 0m;
        var description = RichTextContentFormatter.ToPlainTextSummary(tour.ShortDescription ?? tour.Description, 260);
        var galleryImages = (tour.Images ?? new List<string>())
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .Take(3)
            .ToList();
        var destinationText = tour.Destination?.City ?? "Việt Nam";
        var durationText = tour.Duration?.Text ?? "Liên hệ tư vấn";
        var nextDepartureText = featuredDeparture?.StartDate.ToString("dd/MM/yyyy") ?? "Liên hệ tư vấn";
        var confirmationText = string.Equals(tour.ConfirmationType, "Instant", StringComparison.OrdinalIgnoreCase)
            ? "Xác nhận tức thì"
            : "Chờ xác nhận";

        return new PublicTourDetailsPageViewModel
        {
            Tour = tour,
            RelatedTours = relatedTours.ToList(),
            Dossier = new PublicTourDossierViewModel
            {
                Eyebrow = confirmationText,
                Headline = tour.Name,
                Description = description,
                DestinationText = destinationText,
                DurationText = durationText,
                NextDepartureText = nextDepartureText,
                MeetingPointText = string.IsNullOrWhiteSpace(tour.MeetingPoint)
                    ? "HV Travel sẽ gửi meeting point chi tiết trong voucher xác nhận."
                    : tour.MeetingPoint,
                StartingPriceDisplay = $"{startingPrice:N0}₫",
                ConfirmationText = confirmationText,
                RatingsText = $"{tour.Rating:0.0}/5 · {tour.ReviewCount} đánh giá",
                PrimaryDepartureId = featuredDeparture?.Id,
                PrimaryDepartureStartDateValue = featuredDeparture?.StartDate.ToString("yyyy-MM-dd"),
                GalleryImages = galleryImages,
                Facts = new List<PublicTourFactVm>
                {
                    new() { Label = "Điểm đến", Value = destinationText },
                    new() { Label = "Thời lượng", Value = durationText },
                    new() { Label = "Khởi hành gần nhất", Value = nextDepartureText },
                    new() { Label = "Giá từ", Value = $"{startingPrice:N0}₫", Tone = "accent" }
                },
                Departures = departures.Select(departure => new PublicTourDepartureCardVm
                {
                    Id = departure.Id,
                    StartDateText = departure.StartDate.ToString("dd/MM/yyyy"),
                    StartDateValue = departure.StartDate.ToString("yyyy-MM-dd"),
                    ConfirmationText = string.Equals(departure.ConfirmationType, "Instant", StringComparison.OrdinalIgnoreCase) ? "Xác nhận tức thì" : "Chờ xác nhận",
                    RemainingCapacityText = $"{departure.RemainingCapacity} chỗ còn lại",
                    PriceText = $"{(departure.AdultPrice > 0m ? departure.AdultPrice : tour.Price?.Adult ?? 0m):N0}₫",
                    LowAvailability = departure.RemainingCapacity is > 0 and <= 5
                }).ToList()
            }
        };
    }
}
