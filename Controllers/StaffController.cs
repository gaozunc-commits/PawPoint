using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace Demo.Controllers;

[Authorize(Roles = "Admin")]
public class StaffController(DB db, Helper hp) : Controller
{
    private void PopulateLookups(string? selectedBranch = null, IEnumerable<string>? selectedServices = null)
    {
        ViewBag.BranchList = new SelectList(db.Branches.OrderBy(b => b.Name), "Id", "Name", selectedBranch);
        ViewBag.ServiceList = new MultiSelectList(db.Services.OrderBy(s => s.Name), "Id", "Name", selectedServices);
    }

    // GET: Staff/Index
    public IActionResult Index(string? name, string? branchId, string? sort, string? dir, int page = 1)
    {
        name = name?.Trim() ?? "";
        ViewBag.Branches = db.Branches.ToList();

        var q = db.Staffs.Include(s => s.Branch)
                         .Include(s => s.StaffServices).ThenInclude(ss => ss.Service)
                         .Where(s => (s.Name.Contains(name) || s.Email.Contains(name)) && (s.BranchId == branchId || branchId == null));

        Func<Staff, object> fn = sort switch
        {
            "Name" => x => x.Name,
            "Email" => x => x.Email,
            "Branch" => x => x.Branch?.Name ?? "",
            _ => x => x.Email
        };

        var list = dir == "des" ? q.OrderByDescending(fn) : q.OrderBy(fn);
        var m = list.ToPagedList(page, 6);

        if (page < 1) return RedirectToAction(null, new { name, branchId, sort, dir, page = 1 });
        if (page > m.PageCount && m.PageCount > 0) return RedirectToAction(null, new { name, branchId, sort, dir, page = m.PageCount });

        if (Request.IsAjax()) return PartialView("_A", m);
        return View(m);
    }

    // GET: Staff/CheckEmail
    public bool CheckEmail(string email) => !db.Users.Any(u => u.Email == email);

    // GET: Staff/Insert
    public IActionResult Insert()
    {
        PopulateLookups();
        return View();
    }

    // POST: Staff/Insert
    [HttpPost]
    public IActionResult Insert(StaffInsertVM vm)
    {
        if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Staff Email.");
        }

        if (vm.Photo != null && ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            var staff = new Staff
            {
                Email = vm.Email.Trim(),
                Name = vm.Name.Trim(),
                Phone = vm.Phone.Trim(),
                BranchId = vm.BranchId,
                Hash = hp.HashPassword(vm.Password),
                PhotoURL = vm.Photo == null ? "photo.jpg" : hp.SavePhoto(vm.Photo, "staff")
            };

            db.Staffs.Add(staff);

            // M:M StaffService junction assignment
            foreach (var svId in vm.ServiceIds)
            {
                db.StaffServices.Add(new StaffService
                {
                    StaffEmail = staff.Email,
                    ServiceId = svId
                });
            }

            db.SaveChanges();
            TempData["Info"] = $"Staff member <b>{staff.Name}</b> inserted successfully.";
            return RedirectToAction("Index");
        }

        PopulateLookups(vm.BranchId, vm.ServiceIds);
        return View(vm);
    }

    // GET: Staff/Update
    public IActionResult Update(string? id)
    {
        var s = db.Staffs.Include(x => x.StaffServices).FirstOrDefault(x => x.Email == id);
        if (s == null) return RedirectToAction("Index");

        var selected = s.StaffServices.Select(ss => ss.ServiceId).ToArray();
        PopulateLookups(s.BranchId, selected);

        return View(new StaffUpdateVM
        {
            Email = s.Email,
            Name = s.Name,
            Phone = s.Phone,
            BranchId = s.BranchId,
            PhotoURL = s.PhotoURL,
            ServiceIds = selected
        });
    }

    // POST: Staff/Update
    [HttpPost]
    public IActionResult Update(StaffUpdateVM vm)
    {
        var s = db.Staffs.Find(vm.Email);
        if (s == null) return RedirectToAction("Index");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            s.Name = vm.Name.Trim();
            s.Phone = vm.Phone.Trim();
            s.BranchId = vm.BranchId;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(s.PhotoURL, "staff");
                s.PhotoURL = hp.SavePhoto(vm.Photo, "staff");
            }

            // Update M:M StaffServices
            db.StaffServices.Where(ss => ss.StaffEmail == s.Email).ExecuteDelete();
            foreach (var svId in vm.ServiceIds)
            {
                db.StaffServices.Add(new StaffService
                {
                    StaffEmail = s.Email,
                    ServiceId = svId
                });
            }

            db.SaveChanges();
            TempData["Info"] = $"Staff <b>{s.Name}</b> updated successfully.";
            return RedirectToAction("Index");
        }

        PopulateLookups(vm.BranchId, vm.ServiceIds);
        return View(vm);
    }

    // POST: Staff/Delete
    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var s = db.Staffs.Find(id);
        if (s != null)
        {
            if (db.Appointments.Any(a => a.StaffEmail == id && a.Status != "Cancelled" && a.Status != "Completed"))
            {
                TempData["Info"] = $"Cannot delete Staff <b>{s.Name}</b> with active appointments.";
            }
            else
            {
                hp.DeletePhoto(s.PhotoURL, "staff");
                db.StaffServices.Where(ss => ss.StaffEmail == id).ExecuteDelete();
                db.Staffs.Remove(s);
                db.SaveChanges();
                TempData["Info"] = $"Staff <b>{s.Name}</b> deleted successfully.";
            }
        }
        return RedirectToAction("Index");
    }
}
