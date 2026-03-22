using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HVTravel.Web.Validation;

public sealed class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value)
    {
        return value is true;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-required", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
    }

    public override string FormatErrorMessage(string name)
    {
        return string.IsNullOrWhiteSpace(ErrorMessage)
            ? $"{name} must be accepted."
            : ErrorMessage!;
    }

    private static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (attributes.ContainsKey(key))
        {
            return false;
        }

        attributes.Add(key, value);
        return true;
    }
}
