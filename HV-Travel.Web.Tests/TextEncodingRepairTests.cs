using System.Text;
using HVTravel.Domain.Utils;

namespace HV_Travel.Web.Tests;

public class TextEncodingRepairTests
{
    [Fact]
    public void NormalizeText_DoesNotThrow_WhenCodePageProviderHasNotBeenRegisteredYet()
    {
        var exception = Record.Exception(() => TextEncodingRepair.NormalizeText("Plain ASCII text."));

        Assert.Null(exception);
    }

    [Fact]
    public void NormalizeText_Repairs_DoubleEncoded_Vietnamese_Text()
    {
        var corrupted = ToMojibake("Khám phá Việt Nam theo cách của bạn");

        var result = TextEncodingRepair.NormalizeText(corrupted);

        Assert.Equal("Khám phá Việt Nam theo cách của bạn", result);
    }

    [Fact]
    public void NormalizeText_Repairs_Mojibake_With_D_Character()
    {
        var corrupted = ToMojibake("Được tham quan các danh lam");

        var result = TextEncodingRepair.NormalizeText(corrupted);

        Assert.Equal("Được tham quan các danh lam", result);
    }

    [Fact]
    public void NormalizeText_Leaves_Clean_Vietnamese_Text_Unchanged()
    {
        const string clean = "Đặt tour thành công và sẵn sàng tra cứu booking.";

        var result = TextEncodingRepair.NormalizeText(clean);

        Assert.Equal(clean, result);
    }

    [Fact]
    public void LooksCorrupted_Flags_Mojibake_But_Not_Clean_Text()
    {
        var corrupted = ToMojibake("Cẩm nang Nhật Bản");

        Assert.True(TextEncodingRepair.LooksCorrupted(corrupted));
        Assert.False(TextEncodingRepair.LooksCorrupted("Cẩm nang Nhật Bản"));
    }

    private static string ToMojibake(string value)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.UTF8.GetBytes(value);
        return Encoding.GetEncoding(1252).GetString(bytes);
    }
}
