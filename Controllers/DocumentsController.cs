using AccountantPortal.Data;
using AccountantPortal.Models;
using AccountantPortal.Services;
using AccountantPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountantPortal.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly INotificationService _notifications;

    public DocumentsController(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        ITaxCalculationService taxCalculationService,
        INotificationService notifications)
    {
        _dbContext = dbContext;
        _environment = environment;
        _userManager = userManager;
        _taxCalculationService = taxCalculationService;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var client = await GetCurrentClientAsync(cancellationToken);
        if (client is null)
        {
            return RedirectToAction("CreateProfile", "ClientDashboard");
        }

        var documents = await _dbContext.Documents
            .Include(document => document.Reports)
            .Where(document => document.ClientId == client.Id)
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return View(documents);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        var client = await GetCurrentClientAsync(cancellationToken);
        if (client is null)
        {
            return RedirectToAction("CreateProfile", "ClientDashboard");
        }

        if (model.File is null || model.File.Length == 0)
        {
            TempData["Error"] = "Choose a document or image to upload.";
            return RedirectToAction(nameof(Index));
        }

        var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", client.Id.ToString());
        Directory.CreateDirectory(uploadDirectory);

        var safeExtension = Path.GetExtension(model.File.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var absolutePath = Path.Combine(uploadDirectory, storedFileName);
        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await model.File.CopyToAsync(stream, cancellationToken);
        }

        var document = new Document
        {
            ClientId = client.Id,
            AccountantId = client.AccountantId,
            OriginalFileName = Path.GetFileName(model.File.FileName),
            StoredFileName = storedFileName,
            FilePath = $"/uploads/{client.Id}/{storedFileName}",
            ContentType = model.File.ContentType,
            FileSize = model.File.Length,
            Status = DocumentStatus.Pending
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (client.Accountant is not null)
        {
            await _notifications.NotifyAsync(
                client.Accountant.ApplicationUserId,
                "New client document",
                $"{client.DisplayName} uploaded {document.OriginalFileName}.",
                cancellationToken);
        }

        TempData["Success"] = "Document uploaded and sent to your accountant.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var client = await GetCurrentClientAsync(cancellationToken);
        if (client is null)
        {
            return RedirectToAction("CreateProfile", "ClientDashboard");
        }

        var document = await _dbContext.Documents.FirstOrDefaultAsync(item => item.Id == id && item.ClientId == client.Id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var absolutePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }

        _dbContext.Documents.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Document deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken cancellationToken)
    {
        var client = await GetCurrentClientAsync(cancellationToken);
        if (client is null)
        {
            return RedirectToAction("CreateProfile", "ClientDashboard");
        }

        var document = await _dbContext.Documents
            .Include(item => item.Accountant)
            .FirstOrDefaultAsync(item => item.Id == id && item.ClientId == client.Id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        document.Status = DocumentStatus.Paid;
        document.PaidAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (document.Accountant is not null)
        {
            await _notifications.NotifyAsync(
                document.Accountant.ApplicationUserId,
                "Payment completed",
                $"{client.DisplayName} marked {document.OriginalFileName} as paid.",
                cancellationToken);
        }

        TempData["Success"] = "Payment marked as completed and your accountant was notified.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Open(int id, CancellationToken cancellationToken)
    {
        var document = await GetAuthorizedDocumentAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var absolutePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(absolutePath))
        {
            return NotFound();
        }

        return PhysicalFile(absolutePath, string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType, document.OriginalFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateReport(ReportCreateViewModel model, CancellationToken cancellationToken)
    {
        var accountant = await GetCurrentAccountantAsync(cancellationToken);
        if (accountant is null)
        {
            return RedirectToAction("CreateProfile", "AccountantDashboard");
        }

        var document = await _dbContext.Documents
            .Include(item => item.Client)
            .FirstOrDefaultAsync(item => item.Id == model.DocumentId && item.AccountantId == accountant.Id, cancellationToken);
        if (document is null || document.Client is null)
        {
            return NotFound();
        }

        var absolutePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(absolutePath))
        {
            return NotFound();
        }

        await using var fileStream = System.IO.File.OpenRead(absolutePath);
        var formFile = new FormFile(fileStream, 0, fileStream.Length, document.OriginalFileName, document.OriginalFileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = document.ContentType
        };
        var calculation = await _taxCalculationService.CalculateAsync(formFile, cancellationToken);

        document.Status = DocumentStatus.Reviewed;
        var report = new Report
        {
            AccountantId = accountant.Id,
            ClientId = document.ClientId,
            DocumentId = document.Id,
            TaxableAmount = calculation.TaxableAmount,
            TaxRate = calculation.TaxRate,
            TaxAmount = calculation.TaxAmount,
            PaymentStatus = DocumentStatus.Unpaid,
            AccountantComments = string.IsNullOrWhiteSpace(model.AccountantComments)
                ? calculation.Notes
                : $"{model.AccountantComments}\n{calculation.Notes}"
        };

        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyAsync(document.Client.ApplicationUserId, "New tax report", "Your accountant generated a new tax report.", cancellationToken);

        TempData["Success"] = "Report generated and sent to the client.";
        return RedirectToAction("MyClients", "AccountantDashboard");
    }

    private async Task<Client?> GetCurrentClientAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        return user is null
            ? null
            : await _dbContext.Clients.Include(item => item.Accountant).FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
    }

    private async Task<Accountant?> GetCurrentAccountantAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        return user is null
            ? null
            : await _dbContext.Accountants.FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
    }

    private async Task<Document?> GetAuthorizedDocumentAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return null;
        }

        var client = await _dbContext.Clients.FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
        var accountant = await _dbContext.Accountants.FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
        return await _dbContext.Documents.FirstOrDefaultAsync(document =>
            document.Id == id &&
            ((client != null && document.ClientId == client.Id) || (accountant != null && document.AccountantId == accountant.Id)),
            cancellationToken);
    }
}
