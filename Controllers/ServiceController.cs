using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace Demo.Controllers;

[Authorize(Roles = "Admin")]
public class ServiceController(DB db, Helper hp) : Controller
{
    private void PopulateBranchList(string? selected = null)
    {
        ViewBag.BranchId = new SelectList(db.Branches.OrderBy(b => b.Name), "Id", "Name", selected);
    }

    // GET: Service/Index
    public IActionResult Index(string? name, string? branchId, string? sort, string? dir, int page = 1)
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

        if (Request.IsAjax()) return PartialView("_A", m);
        return View(m);
    }

    // GET: Service/CheckId
    public bool CheckId(string id) => !db.Services.Any(s => s.Id == id);

    // GET: Service/Insert
    public IActionResult Insert()
    {
        PopulateBranchList();
        return View();
    }

    // POST: Service/Insert
    [HttpPost]
    public IActionResult Insert(ServiceInsertVM vm)
    {
        if (ModelState.IsValid("Id") && db.Services.Any(s => s.Id == vm.Id))
        {
            ModelState.AddModelError("Id", "Duplicated Service Id.");
        }

        if (vm.Photo != null && ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            var sv = new Service
            {
                Id = vm.Id.ToUpper().Trim(),
                Name = vm.Name.Trim(),
                Description = vm.Description.Trim(),
                Price = vm.Price,
                DurationMinutes = vm.DurationMinutes,
                BranchId = vm.BranchId,
                PhotoURL = vm.Photo == null ? "photo.jpg" : hp.SavePhoto(vm.Photo, "services")
            };

            db.Services.Add(sv);
            db.SaveChanges();

            TempData["Info"] = $"Service <b>{sv.Name}</b> inserted successfully.";
            return RedirectToAction("Index");
        }

        PopulateBranchList(vm.BranchId);
        return View(vm);
    }

    // GET: Service/Update
    public IActionResult Update(string? id)
    {
        var s = db.Services.Find(id);
        if (s == null) return RedirectToAction("Index");

        PopulateBranchList(s.BranchId);
        return View(new ServiceUpdateVM
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Price = s.Price,
            DurationMinutes = s.DurationMinutes,
            BranchId = s.BranchId,
            PhotoURL = s.PhotoURL
        });
    }

    // POST: Service/Update
    [HttpPost]
    public IActionResult Update(ServiceUpdateVM vm)
    {
        var s = db.Services.Find(vm.Id);
        if (s == null) return RedirectToAction("Index");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            s.Name = vm.Name.Trim();
            s.Description = vm.Description.Trim();
            s.Price = vm.Price;
            s.DurationMinutes = vm.DurationMinutes;
            s.BranchId = vm.BranchId;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(s.PhotoURL, "services");
                s.PhotoURL = hp.SavePhoto(vm.Photo, "services");
            }

            db.SaveChanges();
            TempData["Info"] = $"Service <b>{s.Name}</b> updated successfully.";
            return RedirectToAction("Index");
        }

        PopulateBranchList(vm.BranchId);
        return View(vm);
    }

    // POST: Service/Delete
    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var s = db.Services.Find(id);
        if (s != null)
        {
            if (db.AppointmentServices.Any(a => a.ServiceId == id))
            {
                TempData["Info"] = $"Cannot delete Service <b>{s.Name}</b> because it is referenced in appointment records.";
            }
            else
            {
                hp.DeletePhoto(s.PhotoURL, "services");
                db.StaffServices.Where(ss => ss.ServiceId == id).ExecuteDelete();
                db.Services.Remove(s);
                db.SaveChanges();
                TempData["Info"] = $"Service <b>{s.Name}</b> deleted successfully.";
            }
        }
        return RedirectToAction("Index");
    }
}
