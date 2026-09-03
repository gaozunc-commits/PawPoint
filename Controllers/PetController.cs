using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

[Authorize(Roles = "Customer")]
public class PetController(DB db, Helper hp) : Controller
{
    // GET: Pet/Index
    public IActionResult Index()
    {
        var pets = db.Pets.Where(x => x.CustomerEmail == User.Identity!.Name).ToList();
        return View(pets);
    }

    // GET: Pet/Insert
    public IActionResult Insert() => View();

    // POST: Pet/Insert
    [HttpPost]
    public IActionResult Insert(PetInsertVM vm)
    {
        if (vm.Photo != null && ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            string photoUrl = "photo.jpg";
            if (!string.IsNullOrEmpty(vm.WebcamPhotoBase64))
            {
                photoUrl = hp.SavePhotoFromBase64(vm.WebcamPhotoBase64, "pets");
            }
            else if (vm.Photo != null)
            {
                photoUrl = hp.SavePhoto(vm.Photo, "pets");
            }

            var pet = new Pet
            {
                Name = vm.Name.Trim(),
                Species = vm.Species,
                Breed = vm.Breed.Trim(),
                DOB = vm.DOB,
                CustomerEmail = User.Identity!.Name!,
                PhotoURL = photoUrl
            };

            db.Pets.Add(pet);
            db.SaveChanges();

            TempData["Info"] = $"Pet profile for <b>{pet.Name}</b> created successfully!";
            return RedirectToAction("Index");
        }

        return View(vm);
    }

    // GET: Pet/Update
    public IActionResult Update(int id)
    {
        var p = db.Pets.Find(id);
        if (p == null || p.CustomerEmail != User.Identity!.Name) return RedirectToAction("Index");

        return View(new PetUpdateVM
        {
            Id = p.Id,
            Name = p.Name,
            Species = p.Species,
            Breed = p.Breed,
            DOB = p.DOB,
            PhotoURL = p.PhotoURL
        });
    }

    // POST: Pet/Update
    [HttpPost]
    public IActionResult Update(PetUpdateVM vm)
    {
        var p = db.Pets.Find(vm.Id);
        if (p == null || p.CustomerEmail != User.Identity!.Name) return RedirectToAction("Index");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            p.Name = vm.Name.Trim();
            p.Species = vm.Species;
            p.Breed = vm.Breed.Trim();
            p.DOB = vm.DOB;

            if (!string.IsNullOrEmpty(vm.WebcamPhotoBase64))
            {
                hp.DeletePhoto(p.PhotoURL, "pets");
                p.PhotoURL = hp.SavePhotoFromBase64(vm.WebcamPhotoBase64, "pets");
            }
            else if (vm.Photo != null)
            {
                hp.DeletePhoto(p.PhotoURL, "pets");
                p.PhotoURL = hp.SavePhoto(vm.Photo, "pets");
            }

            db.SaveChanges();
            TempData["Info"] = $"Pet profile for <b>{p.Name}</b> updated successfully.";
            return RedirectToAction("Index");
        }

        return View(vm);
    }

    // POST: Pet/Delete
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var p = db.Pets.Find(id);
        if (p != null && p.CustomerEmail == User.Identity!.Name)
        {
            if (db.Appointments.Any(a => a.PetId == id && a.Status != "Cancelled" && a.Status != "Completed"))
            {
                TempData["Info"] = $"Cannot delete <b>{p.Name}</b> while there are active appointments scheduled.";
            }
            else
            {
                hp.DeletePhoto(p.PhotoURL, "pets");
                db.Pets.Remove(p);
                db.SaveChanges();
                TempData["Info"] = $"Pet profile for <b>{p.Name}</b> deleted.";
            }
        }
        return RedirectToAction("Index");
    }
}
