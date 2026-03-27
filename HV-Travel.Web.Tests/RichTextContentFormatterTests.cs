using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class RichTextContentFormatterTests
{
    [Fact]
    public void ToPlainText_DecodesEntitiesAndStripsHtml()
    {
        var html = "<p>Ðu?c tham quan c&aacute;c danh lam</p>";

        var result = RichTextContentFormatter.ToPlainText(html);

        Assert.Equal("Ðu?c tham quan các danh lam", result);
    }

    [Fact]
    public void ToPlainText_NormalizesWhitespaceAcrossBlockElements()
    {
        var html = "<p>Ngày 1</p><ul><li>An sáng</li><li>Tham quan</li></ul><p>  K?t thúc </p>";

        var result = RichTextContentFormatter.ToPlainText(html);

        Assert.Equal("Ngày 1 An sáng Tham quan K?t thúc", result);
    }

    [Fact]
    public void ToPlainTextSummary_TruncatesAfterNormalization()
    {
        var html = "<p>Ðu?c tham quan c&aacute;c danh lam th?ng c?nh ? xung quanh Hà N?i</p>";

        var result = RichTextContentFormatter.ToPlainTextSummary(html, 25);

        Assert.Equal("Ðu?c tham quan các danh…", result);
    }

    [Fact]
    public void ToTrustedHtml_DecodesDoubleEncodedRichText()
    {
        var html = "&amp;lt;p&amp;gt;L&amp;ecirc;n xe di d?n H&amp;agrave; N?i&amp;lt;/p&amp;gt;";

        var result = RichTextContentFormatter.ToTrustedHtml(html);

        Assert.Contains("<p>", result);
        Assert.Contains("</p>", result);
        Assert.DoesNotContain("&amp;lt;", result);
        Assert.DoesNotContain("&lt;p&gt;", result);
        Assert.DoesNotContain("&ecirc;", result);
        Assert.DoesNotContain("&agrave;", result);
    }
}
