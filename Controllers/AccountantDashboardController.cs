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
public class AccountantDashboardController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;

    public AccountantDashboardController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, INotificationService notifications)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var accountant = await _dbContext.Accountants
            .Include(item => item.Clients)
            .FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);

        if (accountant is null)
        {
            return RedirectToAction(nameof(CreateProfile));
        }

        var documents = await _dbContext.Documents
            .Include(document => document.Client)
            .Where(document => document.AccountantId == accountant.Id)
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return View(new AccountantDashboardViewModel
        {
            Accountant = accountant,
            Clients = accountant.Clients.OrderBy(client => client.DisplayName).ToList(),
            PendingDocuments = documents.Where(document => document.Status is DocumentStatus.Pending or DocumentStatus.Unpaid).ToList(),
            PaidDocuments = documents.Where(document => document.Status == DocumentStatus.Paid).ToList(),
            Notifications = await _notifications.UnreadForUserAsync(user.Id, cancellationToken)
        });
    }

    public async Task<IActionResult> MyClients(CancellationToken cancellationToken)
    {
        var accountant = await GetCurrentAccountantAsync(cancellationToken);
        if (accountant is null)
        {
            return RedirectToAction(nameof(CreateProfile));
        }

        var clients = await _dbContext.Clients
            .Include(client => client.Documents)
                .ThenInclude(document => document.Reports)
            .Where(client => client.AccountantId == accountant.Id)
            .OrderBy(client => client.DisplayName)
            .ToListAsync(cancellationToken);

        return View(clients);
    }

    public IActionResult CreateProfile()
    {
        return View(new Accountant());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProfile(Accountant accountant, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        ModelState.Remove(nameof(Accountant.ApplicationUserId));
        if (!ModelState.IsValid)
        {
            return View(accountant);
        }

        accountant.ApplicationUserId = user.Id;
        _dbContext.Accountants.Add(accountant);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task<Accountant?> GetCurrentAccountantAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return null;
        }

        return await _dbContext.Accountants.FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
    }
}
