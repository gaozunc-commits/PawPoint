using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace Demo.Controllers;

public class HomeController(DB db) : Controller
{
    // GET: Home/Index
    public IActionResult Index() => View();

    // GET: Home/About
    public IActionResult About() => View();

    // GET: Home/ServiceCatalog
    public IActionResult ServiceCatalog(string? name, string? branchId, string? sort, string? dir, int page = 1)
    {
        name = name?.Trim() ?? "";
        ViewBag.Branches = db.Branches.ToList();

        var q = db.Services.Include(s => s.Branch)
                           .Where(s => s.Name.Contains(name) && (s.BranchId == branchId || branchId == null));

        Func<Service, object> fn = sort switch
        {
            "Name" => x => x.Name,
            "Price" => x => x.Price,
            "Duration" => x => x.DurationMinutes,
            "Branch" => x => x.Branch?.Name ?? "",
            _ => x => x.Id
        };

        var list = dir == "des" ? q.OrderByDescending(fn) : q.OrderBy(fn);
        var m = list.ToPagedList(page, 6);

        if (page < 1) return RedirectToAction(null, new { name, branchId, sort, dir, page = 1 });
        if (page > m.PageCount && m.PageCount > 0) return RedirectToAction(null, new { name, branchId, sort, dir, page = m.PageCount });

        if (Request.IsAjax())
        {
            return PartialView("_ServiceCatalog", m);
        }

        return View(m);
    }
}
