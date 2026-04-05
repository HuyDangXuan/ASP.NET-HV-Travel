using System.Text;
using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class RichTextContentFormatterTests
{
    [Fact]
    public void ToPlainText_StripsHtml_And_Repairs_Mojibake()
    {
        var html = $"<p>{ToMojibake("Được tham quan các danh lam")}</p>";

        var result = RichTextContentFormatter.ToPlainText(html);

        Assert.Equal("Được tham quan các danh lam", result);
    }

    [Fact]
    public void ToPlainText_NormalizesWhitespaceAcrossBlockElements()
    {
        var html = "<p>Ngày 1</p><ul><li>Ăn sáng</li><li>Tham quan</li></ul><p>  Kết thúc </p>";

        var result = RichTextContentFormatter.ToPlainText(html);

        Assert.Equal("Ngày 1 Ăn sáng Tham quan Kết thúc", result);
    }

    [Fact]
    public void ToPlainTextSummary_TruncatesAfterNormalization()
    {
        var html = $"<p>{ToMojibake("Được tham quan các danh thắng cảnh ở xung quanh Hà Nội")}</p>";

        var result = RichTextContentFormatter.ToPlainTextSummary(html, 25);

        Assert.Equal("Được tham quan các danh…", result);
    }

    [Fact]
    public void ToTrustedHtml_DecodesDoubleEncodedRichText()
    {
        var html = "&amp;lt;p&amp;gt;L&amp;ecirc;n xe di chuyển về H&amp;agrave; Nội&amp;lt;/p&amp;gt;";

        var result = RichTextContentFormatter.ToTrustedHtml(html);

        Assert.Contains("<p>", result);
        Assert.Contains("</p>", result);
        Assert.DoesNotContain("&amp;lt;", result);
        Assert.DoesNotContain("&lt;p&gt;", result);
        Assert.DoesNotContain("&ecirc;", result);
        Assert.DoesNotContain("&agrave;", result);
    }

    private static string ToMojibake(string value)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.UTF8.GetBytes(value);
        return Encoding.GetEncoding(1252).GetString(bytes);
    }
}
