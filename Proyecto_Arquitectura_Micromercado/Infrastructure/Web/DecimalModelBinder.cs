using Microsoft.AspNetCore.Mvc.ModelBinding;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

public sealed class DecimalModelBinder : IModelBinder
{
    private static readonly Regex DecimalPattern =
        new(@"^\d+([.,]\d{1,2})?$", RegexOptions.CultureInvariant);

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        var rawValue = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(rawValue))
        {
            bindingContext.ModelState.AddModelError(
                bindingContext.ModelName,
                GetErrorMessage(bindingContext.ModelName));
            return Task.CompletedTask;
        }

        if (!DecimalPattern.IsMatch(rawValue))
        {
            bindingContext.ModelState.AddModelError(
                bindingContext.ModelName,
                GetErrorMessage(bindingContext.ModelName));
            return Task.CompletedTask;
        }

        var normalizedValue = rawValue.Replace(',', '.');
        if (decimal.TryParse(
                normalizedValue,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var decimalValue))
        {
            bindingContext.Result = ModelBindingResult.Success(decimalValue);
        }
        else
        {
            bindingContext.ModelState.AddModelError(
                bindingContext.ModelName,
                "El precio no tiene un formato válido.");
        }

        return Task.CompletedTask;
    }

    private static string GetErrorMessage(string modelName) =>
        modelName.Contains("PrecioVenta", StringComparison.Ordinal)
            ? ProductPriceValidation.SaleMessage
            : ProductPriceValidation.CostMessage;
}

public sealed class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.ModelType == typeof(decimal)
            ? new DecimalModelBinder()
            : null;
}
