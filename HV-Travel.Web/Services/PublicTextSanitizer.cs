using System.Text;
using HVTravel.Domain.Entities;

namespace HVTravel.Web.Services;

public static class PublicTextSanitizer
{
    private static readonly string[] MojibakeMarkers =
    [
        "Ã", "Â", "Ä", "Æ", "áº", "á»", "â€™", "â€œ", "â€", "�"
    ];

    static PublicTextSanitizer()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static TravelArticle NormalizeArticleForDisplay(TravelArticle article)
    {
        if (article == null)
        {
            return new TravelArticle();
        }

        article.Title = NormalizeText(article.Title);
        article.Summary = NormalizeText(article.Summary);
        article.Body = NormalizeText(article.Body);
        article.Category = NormalizeText(article.Category);
        article.Destination = NormalizeText(article.Destination);
        article.Tags = NormalizeList(article.Tags);
        return article;
    }

    public static Tour NormalizeTourForDisplay(Tour tour)
    {
        if (tour == null)
        {
            return new Tour();
        }

        tour.Name = NormalizeText(tour.Name);
        tour.Description = NormalizeText(tour.Description);
        tour.ShortDescription = NormalizeText(tour.ShortDescription);
        tour.ConfirmationType = NormalizeText(tour.ConfirmationType);
        tour.MeetingPoint = NormalizeText(tour.MeetingPoint);
        tour.Highlights = NormalizeList(tour.Highlights);
        tour.BadgeSet = NormalizeList(tour.BadgeSet);
        tour.GeneratedInclusions = NormalizeList(tour.GeneratedInclusions);
        tour.GeneratedExclusions = NormalizeList(tour.GeneratedExclusions);

        if (tour.Destination != null)
        {
            tour.Destination.City = NormalizeText(tour.Destination.City);
            tour.Destination.Country = NormalizeText(tour.Destination.Country);
            tour.Destination.Region = NormalizeText(tour.Destination.Region);
        }

        if (tour.Duration != null)
        {
            tour.Duration.Text = NormalizeText(tour.Duration.Text);
        }

        if (tour.Seo != null)
        {
            tour.Seo.Title = NormalizeText(tour.Seo.Title);
            tour.Seo.Description = NormalizeText(tour.Seo.Description);
            tour.Seo.CanonicalPath = NormalizeText(tour.Seo.CanonicalPath);
        }

        if (tour.CancellationPolicy != null)
        {
            tour.CancellationPolicy.Summary = NormalizeText(tour.CancellationPolicy.Summary);
        }

        if (tour.SupplierRef != null)
        {
            tour.SupplierRef.SupplierName = NormalizeText(tour.SupplierRef.SupplierName);
            tour.SupplierRef.SourceSystem = NormalizeText(tour.SupplierRef.SourceSystem);
        }

        foreach (var departure in tour.Departures ?? [])
        {
            departure.ConfirmationType = NormalizeText(departure.ConfirmationType);
            departure.Status = NormalizeText(departure.Status);
        }

        foreach (var item in tour.Schedule ?? [])
        {
            item.Title = NormalizeText(item.Title);
            item.Description = NormalizeText(item.Description);
            item.Activities = NormalizeList(item.Activities);
        }

        return tour;
    }

    public static Booking NormalizeBookingForDisplay(Booking booking)
    {
        if (booking == null)
        {
            return new Booking();
        }

        booking.Status = NormalizeText(booking.Status);
        booking.PaymentStatus = NormalizeText(booking.PaymentStatus);
        booking.Notes = NormalizeText(booking.Notes);
        booking.BookingCode = NormalizeText(booking.BookingCode);
        booking.CouponCode = NormalizeText(booking.CouponCode);
        booking.FulfillmentStatus = NormalizeText(booking.FulfillmentStatus);

        if (booking.TourSnapshot != null)
        {
            booking.TourSnapshot.Code = NormalizeText(booking.TourSnapshot.Code);
            booking.TourSnapshot.Name = NormalizeText(booking.TourSnapshot.Name);
            booking.TourSnapshot.Duration = NormalizeText(booking.TourSnapshot.Duration);
        }

        if (booking.ContactInfo != null)
        {
            booking.ContactInfo.Name = NormalizeText(booking.ContactInfo.Name);
            booking.ContactInfo.Email = NormalizeText(booking.ContactInfo.Email);
            booking.ContactInfo.Phone = NormalizeText(booking.ContactInfo.Phone);
        }

        foreach (var passenger in booking.Passengers ?? [])
        {
            passenger.FullName = NormalizeText(passenger.FullName);
            passenger.Type = NormalizeText(passenger.Type);
            passenger.Gender = NormalizeText(passenger.Gender);
            passenger.PassportNumber = NormalizeText(passenger.PassportNumber);
        }

        foreach (var evt in booking.Events ?? [])
        {
            evt.Type = NormalizeText(evt.Type);
            evt.Title = NormalizeText(evt.Title);
            evt.Description = NormalizeText(evt.Description);
            evt.Actor = NormalizeText(evt.Actor);
        }

        foreach (var log in booking.HistoryLog ?? [])
        {
            log.Action = NormalizeText(log.Action);
            log.Note = NormalizeText(log.Note);
            log.User = NormalizeText(log.User);
        }

        if (booking.CancellationRequest != null)
        {
            booking.CancellationRequest.Status = NormalizeText(booking.CancellationRequest.Status);
            booking.CancellationRequest.Reason = NormalizeText(booking.CancellationRequest.Reason);
            booking.CancellationRequest.RequestedBy = NormalizeText(booking.CancellationRequest.RequestedBy);
            booking.CancellationRequest.ResolutionNote = NormalizeText(booking.CancellationRequest.ResolutionNote);
        }

        return booking;
    }

    public static string NormalizeText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (!LooksLikeMojibake(value))
        {
            return value;
        }

        try
        {
            var repaired = Encoding.UTF8.GetString(Encoding.GetEncoding(1252).GetBytes(value));
            return ScoreCandidate(repaired) >= ScoreCandidate(value) ? repaired : value;
        }
        catch (ArgumentException)
        {
            return value;
        }
    }

    private static List<string> NormalizeList(IEnumerable<string>? values)
    {
        return values?.Select(NormalizeText).ToList() ?? [];
    }

    private static bool LooksLikeMojibake(string value)
    {
        return MojibakeMarkers.Any(marker => value.Contains(marker, StringComparison.Ordinal));
    }

    private static int ScoreCandidate(string value)
    {
        const string vietnameseChars = "ăâđêôơưáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵĂÂĐÊÔƠƯÁÀẢÃẠẮẰẲẴẶẤẦẨẪẬÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ";
        var suspiciousPenalty = MojibakeMarkers.Count(marker => value.Contains(marker, StringComparison.Ordinal)) * 10;
        var vietnameseBonus = value.Count(character => vietnameseChars.Contains(character));
        return vietnameseBonus - suspiciousPenalty;
    }
}