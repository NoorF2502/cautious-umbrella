using System.Globalization;
using System.Text.RegularExpressions;

namespace AccountantPortal.Services;

public class TaxCalculationService : ITaxCalculationService
{
    public async Task<TaxCalculationResult> CalculateAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return new TaxCalculationResult(0, 0, 0, "Empty file uploaded.");
        }

        decimal taxableAmount = 0;
        if (IsTextLike(file.ContentType, file.FileName))
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var text = await reader.ReadToEndAsync(cancellationToken);
            taxableAmount = ExtractMoneyValues(text).Sum();
        }

        var taxRate = ResolveTaxRate(taxableAmount);
        var taxAmount = Math.Round(taxableAmount * taxRate, 2, MidpointRounding.AwayFromZero);
        var notes = taxableAmount == 0
            ? "No numeric taxable amount could be read automatically. Accountant review is required."
            : $"Automatic estimate based on readable values in {file.FileName}.";

        return new TaxCalculationResult(taxableAmount, taxRate, taxAmount, notes);
    }

    private static bool IsTextLike(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || extension is ".csv" or ".txt";
    }

    private static IEnumerable<decimal> ExtractMoneyValues(string text)
    {
        var matches = Regex.Matches(text, @"(?<![\w.])-?\$?\d{1,3}(?:,?\d{3})*(?:\.\d{1,2})?(?![\w.])");
        foreach (Match match in matches)
        {
            var normalized = match.Value.Replace("$", string.Empty).Replace(",", string.Empty);
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0)
            {
                yield return value;
            }
        }
    }

    private static decimal ResolveTaxRate(decimal taxableAmount)
    {
        return taxableAmount switch
        {
            <= 0 => 0,
            <= 10_000 => 0.10m,
            <= 50_000 => 0.15m,
            <= 100_000 => 0.20m,
            _ => 0.25m
        };
    }
}
