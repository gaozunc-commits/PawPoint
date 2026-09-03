using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace Demo.Controllers;

[Authorize(Roles = "Admin")]
public class BranchController(DB db) : Controller
{
    // GET: Branch/Index
    public IActionResult Index(string? name, string? sort, string? dir, int page = 1)
    {
        name = name?.Trim() ?? "";
        var q = db.Branches.Where(x => x.Name.Contains(name) || x.Address.Contains(name));

        Func<Branch, object> fn = sort switch
        {
            "Name" => x => x.Name,
            "Address" => x => x.Address,
            "Phone" => x => x.Phone,
            _ => x => x.Id
        };

        var list = dir == "des" ? q.OrderByDescending(fn) : q.OrderBy(fn);
        var m = list.ToPagedList(page, 5);

        if (page < 1) return RedirectToAction(null, new { name, sort, dir, page = 1 });
        if (page > m.PageCount && m.PageCount > 0) return RedirectToAction(null, new { name, sort, dir, page = m.PageCount });

        if (Request.IsAjax()) return PartialView("_A", m);
        return View(m);
    }

    // GET: Branch/CheckId
    public bool CheckId(string id) => !db.Branches.Any(x => x.Id == id);

    // GET: Branch/Insert
    public IActionResult Insert() => View();

    // POST: Branch/Insert
    [HttpPost]
    public IActionResult Insert(BranchInsertVM vm)
    {
        if (ModelState.IsValid("Id") && db.Branches.Any(x => x.Id == vm.Id))
        {
            ModelState.AddModelError("Id", "Duplicated Branch Id.");
        }

        if (ModelState.IsValid)
        {
            db.Branches.Add(new Branch
            {
                Id = vm.Id.ToUpper().Trim(),
                Name = vm.Name.Trim(),
                Address = vm.Address.Trim(),
                Phone = vm.Phone.Trim(),
                Latitude = vm.Latitude,
                Longitude = vm.Longitude
            });
            db.SaveChanges();
            TempData["Info"] = $"Branch <b>{vm.Id}</b> inserted successfully.";
            return RedirectToAction("Index");
        }

        return View(vm);
    }

    // GET: Branch/Update
    public IActionResult Update(string? id)
    {
        var b = db.Branches.Find(id);
        if (b == null) return RedirectToAction("Index");

        return View(new BranchUpdateVM
        {
            Id = b.Id,
            Name = b.Name,
            Address = b.Address,
            Phone = b.Phone,
            Latitude = b.Latitude,
            Longitude = b.Longitude
        });
    }

    // POST: Branch/Update
    [HttpPost]
    public IActionResult Update(BranchUpdateVM vm)
    {
        var b = db.Branches.Find(vm.Id);
        if (b == null) return RedirectToAction("Index");

        if (ModelState.IsValid)
        {
            b.Name = vm.Name.Trim();
            b.Address = vm.Address.Trim();
            b.Phone = vm.Phone.Trim();
            b.Latitude = vm.Latitude;
            b.Longitude = vm.Longitude;
            db.SaveChanges();

            TempData["Info"] = $"Branch <b>{b.Id}</b> updated successfully.";
            return RedirectToAction("Index");
        }

        return View(vm);
    }

    // POST: Branch/Delete
    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var b = db.Branches.Find(id);
        if (b != null)
        {
            if (db.Staffs.Any(s => s.BranchId == id) || db.Appointments.Any(a => a.BranchId == id))
            {
                TempData["Info"] = $"Cannot delete Branch <b>{id}</b> because it has linked staff or appointments.";
            }
            else
            {
                db.Branches.Remove(b);
                db.SaveChanges();
                TempData["Info"] = $"Branch <b>{id}</b> deleted successfully.";
            }
        }
        return RedirectToAction("Index");
    }
}
