namespace AccountantPortal.Services;

public record TaxCalculationResult(decimal TaxableAmount, decimal TaxRate, decimal TaxAmount, string Notes);
