using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

[Authorize]
public class ChatController(DB db) : Controller
{
    // GET: Chat/Index
    public IActionResult Index()
    {
        var u = db.Users.Find(User.Identity!.Name);
        ViewBag.UserName = u?.Name ?? User.Identity!.Name;
        ViewBag.UserRole = u?.Role ?? "Guest";
        return View();
    }
}
