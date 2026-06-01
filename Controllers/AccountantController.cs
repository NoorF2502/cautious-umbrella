using CautiousUmbrella.Data;
using CautiousUmbrella.Models;
using CautiousUmbrella.ViewModels;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CautiousUmbrella.Controllers;

public class AccountantController : Controller
{
    private const string SessionAccountantId = "AccountantId";
    private readonly ApplicationDbContext _db;

    public AccountantController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new AccountantRegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AccountantRegisterViewModel model)
    {
        var email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (await _db.Accountants.AnyAsync(a => a.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "An accountant account already exists for this email.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var accountant = new Accountant
        {
            FullName = model.FullName,
            Email = email,
            PasswordHash = HashPassword(model.Password),
            PhoneNumber = model.PhoneNumber,
            OfficeAddress = model.OfficeAddress
        };

        _db.Accountants.Add(accountant);
        await _db.SaveChangesAsync();
        HttpContext.Session.SetInt32(SessionAccountantId, accountant.Id);
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        var accountant = await _db.Accountants.FirstOrDefaultAsync(a => a.Email == email);
        if (accountant == null || !VerifyPassword(model.Password, accountant.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid accountant login.");
            return View(model);
        }

        HttpContext.Session.SetInt32(SessionAccountantId, accountant.Id);
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(SessionAccountantId);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Dashboard()
    {
        var accountantId = GetCurrentAccountantId();
        if (accountantId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var accountant = await _db.Accountants.FirstAsync(a => a.Id == accountantId.Value);
        var clients = await _db.Clients
            .Where(c => c.AccountantId == accountantId.Value)
            .Include(c => c.Documents)
            .Include(c => c.Reports)
            .OrderBy(c => c.FullName)
            .ToListAsync();
        var recentDocuments = await _db.Documents
            .Include(d => d.Client)
            .Where(d => d.Client.AccountantId == accountantId.Value)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Take(10)
            .ToListAsync();

        return View(new AccountantDashboardViewModel
        {
            Accountant = accountant,
            Clients = clients,
            RecentDocuments = recentDocuments
        });
    }

    public async Task<IActionResult> Clients()
    {
        var accountantId = GetCurrentAccountantId();
        if (accountantId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var clients = await _db.Clients
            .Where(c => c.AccountantId == accountantId.Value)
            .Include(c => c.Documents)
            .Include(c => c.Reports)
            .OrderBy(c => c.FullName)
            .ToListAsync();
        return View(clients);
    }

    public async Task<IActionResult> ClientDetails(int id)
    {
        var accountantId = GetCurrentAccountantId();
        if (accountantId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var client = await _db.Clients
            .Include(c => c.Documents.OrderByDescending(d => d.UploadedAtUtc))
            .Include(c => c.Reports.OrderByDescending(r => r.CreatedAtUtc))
                .ThenInclude(r => r.Document)
            .FirstOrDefaultAsync(c => c.Id == id && c.AccountantId == accountantId.Value);
        if (client == null)
        {
            return NotFound();
        }

        return View(new ClientDetailsViewModel
        {
            Client = client,
            Documents = client.Documents.OrderByDescending(d => d.UploadedAtUtc).ToList(),
            Reports = client.Reports.OrderByDescending(r => r.CreatedAtUtc).ToList()
        });
    }

    public async Task<IActionResult> Documents()
    {
        var accountantId = GetCurrentAccountantId();
        if (accountantId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var documents = await _db.Documents
            .Include(d => d.Client)
            .Where(d => d.Client.AccountantId == accountantId.Value)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
        return View(documents);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDocumentReviewed(int id)
    {
        var accountantId = GetCurrentAccountantId();
        if (accountantId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var document = await _db.Documents
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == id && d.Client.AccountantId == accountantId.Value);
        if (document == null)
        {
            return NotFound();
        }

        document.Status = DocumentStatus.Reviewed;
        document.ReviewedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Documents));
    }

    [HttpGet]
    public async Task<IActionResult> CreateReport(int documentId)
    {
        var document = await GetOwnedDocument(documentId);
        if (document == null)
        {
            if (GetCurrentAccountantId() == null)
            {
                return RedirectToAction(nameof(Login));
            }

            return NotFound();
        }

        return View(new CreateReportViewModel
        {
            ClientId = document.ClientId,
            DocumentId = document.Id,
            Title = $"Report for {document.OriginalFileName}",
            PaymentAmount = 0,
            Client = document.Client,
            Document = document
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReport(CreateReportViewModel model)
    {
        var document = await GetOwnedDocument(model.DocumentId);
        if (document == null)
        {
            if (GetCurrentAccountantId() == null)
            {
                return RedirectToAction(nameof(Login));
            }

            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Client = document.Client;
            model.Document = document;
            return View(model);
        }

        var taxableAmount = Math.Max(0, model.Income - model.Expenses);
        var taxDue = Math.Round(taxableAmount * (model.TaxRatePercent / 100), 2);
        var report = new Report
        {
            ClientId = document.ClientId,
            DocumentId = document.Id,
            Title = model.Title,
            Income = model.Income,
            Expenses = model.Expenses,
            TaxableAmount = taxableAmount,
            TaxDue = taxDue,
            PaymentAmount = model.PaymentAmount,
            PaymentStatus = model.PaymentAmount > 0 ? PaymentStatus.Unpaid : PaymentStatus.Paid,
            PaidAtUtc = model.PaymentAmount > 0 ? null : DateTime.UtcNow,
            Notes = model.Notes,
            AccountantComments = model.AccountantComments
        };

        document.Status = DocumentStatus.Reviewed;
        document.ReviewedAtUtc = DateTime.UtcNow;
        document.PaymentStatus = report.PaymentStatus;
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(ClientDetails), new { id = document.ClientId });
    }

    private int? GetCurrentAccountantId() => HttpContext.Session.GetInt32(SessionAccountantId);

    private async Task<Document?> GetOwnedDocument(int documentId)
    {
        var accountantId = GetCurrentAccountantId();
        if (accountantId == null)
        {
            return null;
        }

        return await _db.Documents
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Client.AccountantId == accountantId.Value);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 100_000, 256 / 8);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 100_000, 256 / 8);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
