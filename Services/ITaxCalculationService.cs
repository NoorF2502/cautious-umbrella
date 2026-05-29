namespace AccountantPortal.Services;

public interface ITaxCalculationService
{
    Task<TaxCalculationResult> CalculateAsync(IFormFile file, CancellationToken cancellationToken = default);
}
