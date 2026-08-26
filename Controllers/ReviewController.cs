using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;

public class ReviewController(DB db) : Controller
{
    // GET: Review/Index
    public IActionResult Index(int? rating)
    {
        var q = db.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Appointment).ThenInclude(a => a.Pet)
            .Include(r => r.Appointment).ThenInclude(a => a.Staff)
            .Include(r => r.Appointment).ThenInclude(a => a.AppointmentServices).ThenInclude(x => x.Service)
            .Where(r => rating == null || r.Rating == rating);

        var list = q.OrderByDescending(r => r.CreatedAt).ToList();
        ViewBag.AverageRating = list.Count > 0 ? list.Average(r => r.Rating) : 5.0;
        ViewBag.TotalReviews = list.Count;
        ViewBag.SelectedRating = rating;

        return View(list);
    }

    // GET: Review/Insert
    [Authorize(Roles = "Customer")]
    public IActionResult Insert(int appointmentId)
    {
        var appt = db.Appointments.Include(a => a.Pet).FirstOrDefault(a => a.Id == appointmentId);
        if (appt == null || appt.Pet.CustomerEmail != User.Identity!.Name)
        {
            TempData["Info"] = "Appointment not found.";
            return RedirectToAction("MyAppointments", "Appointment");
        }

        if (appt.Status != "Completed")
        {
            TempData["Info"] = "You can only leave reviews for completed appointments.";
            return RedirectToAction("MyAppointments", "Appointment");
        }

        if (db.Reviews.Any(r => r.AppointmentId == appointmentId))
        {
            TempData["Info"] = "You have already reviewed this appointment.";
            return RedirectToAction("MyAppointments", "Appointment");
        }

        ViewBag.Appointment = appt;
        return View(new ReviewInsertVM { AppointmentId = appointmentId, Rating = 5 });
    }

    // POST: Review/Insert
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult Insert(ReviewInsertVM vm)
    {
        var appt = db.Appointments.Include(a => a.Pet).FirstOrDefault(a => a.Id == vm.AppointmentId);
        if (appt == null || appt.Pet.CustomerEmail != User.Identity!.Name)
        {
            return RedirectToAction("MyAppointments", "Appointment");
        }

        if (db.Reviews.Any(r => r.AppointmentId == vm.AppointmentId))
        {
            ModelState.AddModelError("AppointmentId", "Review already submitted for this appointment.");
        }

        if (ModelState.IsValid)
        {
            var rev = new Review
            {
                AppointmentId = vm.AppointmentId,
                CustomerEmail = User.Identity!.Name!,
                Rating = vm.Rating,
                Comment = vm.Comment?.Trim(),
                CreatedAt = DateTime.Now
            };

            db.Reviews.Add(rev);

            // Award 10 Loyalty Points for Review (Spec §3.5 AF)
            var customer = db.Customers.Find(User.Identity!.Name);
            if (customer != null)
            {
                customer.Points += 10;
                db.RewardTransactions.Add(new RewardTransaction
                {
                    CustomerEmail = customer.Email,
                    Points = 10,
                    Reason = $"Review submission reward for Appointment #{vm.AppointmentId}",
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();
            TempData["Info"] = "Thank you for your feedback! You earned <b>+10 loyalty points</b>.";
            return RedirectToAction("Index");
        }

        ViewBag.Appointment = appt;
        return View(vm);
    }
}
