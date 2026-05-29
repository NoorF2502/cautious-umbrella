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
public class ClientDashboardController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;

    public ClientDashboardController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, INotificationService notifications)
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

        var client = await _dbContext.Clients
            .Include(item => item.Accountant)
            .Include(item => item.Documents.OrderByDescending(document => document.UploadedAtUtc).Take(5))
            .Include(item => item.Reports.OrderByDescending(report => report.CreatedAtUtc).Take(5))
            .FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);

        if (client is null)
        {
            return RedirectToAction(nameof(CreateProfile));
        }

        return View(new ClientDashboardViewModel
        {
            Client = client,
            Accountant = client.Accountant,
            RecentDocuments = client.Documents.OrderByDescending(document => document.UploadedAtUtc).ToList(),
            Reports = client.Reports.OrderByDescending(report => report.CreatedAtUtc).ToList(),
            Notifications = await _notifications.UnreadForUserAsync(user.Id, cancellationToken)
        });
    }

    public IActionResult CreateProfile()
    {
        return View(new Client());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProfile(Client client, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        ModelState.Remove(nameof(Client.ApplicationUserId));
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        client.ApplicationUserId = user.Id;
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
