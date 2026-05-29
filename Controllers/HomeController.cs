using Microsoft.AspNetCore.Mvc;

namespace AccountantPortal.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
