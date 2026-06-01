using CautiousUmbrella.Data;
using CautiousUmbrella.Models;
using CautiousUmbrella.ViewModels;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CautiousUmbrella.Controllers;

public class ClientController : Controller
{
    private const string SessionClientId = "ClientId";
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".gif", ".xlsx", ".xls", ".csv", ".doc", ".docx", ".txt", ".qbo", ".ofx"];
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ClientController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        return View(new ClientRegisterViewModel
        {
            AvailableAccountants = await _db.Accountants.OrderBy(a => a.FullName).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(ClientRegisterViewModel model)
    {
        var email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (await _db.Clients.AnyAsync(c => c.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "An account already exists for this email.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableAccountants = await _db.Accountants.OrderBy(a => a.FullName).ToListAsync();
            return View(model);
        }

        var client = new Client
        {
            FullName = model.FullName,
            Email = email,
            PasswordHash = HashPassword(model.Password),
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            AccountantId = model.AccountantId,
            LastActivityAtUtc = DateTime.UtcNow
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        HttpContext.Session.SetInt32(SessionClientId, client.Id);
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
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == email);
        if (client == null || !VerifyPassword(model.Password, client.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid client login.");
            return View(model);
        }

        client.LastActivityAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        HttpContext.Session.SetInt32(SessionClientId, client.Id);
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(SessionClientId);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Dashboard()
    {
        var client = await GetCurrentClientQuery()
            .Include(c => c.Accountant)
            .Include(c => c.Documents.OrderByDescending(d => d.UploadedAtUtc))
            .Include(c => c.Reports.OrderByDescending(r => r.CreatedAtUtc))
                .ThenInclude(r => r.Document)
            .FirstOrDefaultAsync();

        if (client == null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ClientDashboardViewModel
        {
            Client = client,
            Accountant = client.Accountant,
            Documents = client.Documents.OrderByDescending(d => d.UploadedAtUtc).ToList(),
            Reports = client.Reports.OrderByDescending(r => r.CreatedAtUtc).ToList()
        });
    }

    public async Task<IActionResult> Documents()
    {
        var clientId = GetCurrentClientId();
        if (clientId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var documents = await _db.Documents
            .Where(d => d.ClientId == clientId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
        return View(documents);
    }

    [HttpGet]
    public IActionResult UploadDocument()
    {
        if (GetCurrentClientId() == null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new DocumentUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(DocumentUploadViewModel model)
    {
        var clientId = GetCurrentClientId();
        if (clientId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Choose a file to upload.");
        }
        else if (!AllowedExtensions.Contains(Path.GetExtension(model.File.FileName).ToLowerInvariant()))
        {
            ModelState.AddModelError(nameof(model.File), "Unsupported file type.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadRoot);
        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(model.File.FileName).ToLowerInvariant()}";
        var path = Path.Combine(uploadRoot, storedFileName);
        await using (var stream = System.IO.File.Create(path))
        {
            await model.File.CopyToAsync(stream);
        }

        var document = new Document
        {
            ClientId = clientId.Value,
            OriginalFileName = Path.GetFileName(model.File.FileName),
            StoredFileName = storedFileName,
            ContentType = model.File.ContentType,
            FileSizeBytes = model.File.Length,
            Description = model.Description,
            Status = DocumentStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid
        };
        _db.Documents.Add(document);

        var client = await _db.Clients.FindAsync(clientId.Value);
        if (client != null)
        {
            client.LastActivityAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Documents));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var clientId = GetCurrentClientId();
        if (clientId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.ClientId == clientId);
        if (document == null)
        {
            return NotFound();
        }

        if (document.Status != DocumentStatus.Pending)
        {
            TempData["Message"] = "Reviewed documents cannot be deleted.";
            return RedirectToAction(nameof(Documents));
        }

        var path = Path.Combine(_environment.WebRootPath, "uploads", document.StoredFileName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Documents));
    }

    public async Task<IActionResult> Reports()
    {
        var clientId = GetCurrentClientId();
        if (clientId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var reports = await _db.Reports
            .Include(r => r.Document)
            .Where(r => r.ClientId == clientId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();
        return View(reports);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaymentCompleted(int reportId)
    {
        var clientId = GetCurrentClientId();
        if (clientId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var report = await _db.Reports.Include(r => r.Document).FirstOrDefaultAsync(r => r.Id == reportId && r.ClientId == clientId);
        if (report == null)
        {
            return NotFound();
        }

        report.PaymentStatus = PaymentStatus.Paid;
        report.PaidAtUtc = DateTime.UtcNow;
        report.Document.PaymentStatus = PaymentStatus.Paid;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Reports));
    }

    private int? GetCurrentClientId() => HttpContext.Session.GetInt32(SessionClientId);

    private IQueryable<Client> GetCurrentClientQuery()
    {
        var clientId = GetCurrentClientId();
        return clientId == null ? _db.Clients.Where(c => false) : _db.Clients.Where(c => c.Id == clientId.Value);
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
