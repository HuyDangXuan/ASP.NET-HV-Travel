using System.ComponentModel.DataAnnotations;
using HVTravel.Web.Validation;

namespace HV_Travel.Web.Tests;

public class MustBeTrueAttributeTests
{
    [Fact]
    public void GetValidationResult_ReturnsError_WhenCheckboxIsFalse()
    {
        var attribute = new MustBeTrueAttribute
        {
            ErrorMessage = "Bạn cần đồng ý với điều khoản để tiếp tục."
        };

        var result = attribute.GetValidationResult(false, new ValidationContext(new object()));

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Equal("Bạn cần đồng ý với điều khoản để tiếp tục.", result?.ErrorMessage);
    }

}
