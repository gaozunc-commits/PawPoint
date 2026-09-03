using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace Demo.Controllers;

[Authorize]
public class AppointmentController(DB db, Helper hp) : Controller
{
    private void PopulateBookingLists(string? selectedBranch = null, int? selectedPet = null, string? selectedStaff = null, IEnumerable<string>? selectedServices = null)
    {
        string email = User.Identity!.Name!;
        ViewBag.PetList = new SelectList(db.Pets.Where(p => p.CustomerEmail == email).OrderBy(p => p.Name), "Id", "Name", selectedPet);
        ViewBag.BranchList = new SelectList(db.Branches.OrderBy(b => b.Name), "Id", "Name", selectedBranch);

        var staffQuery = db.Staffs.AsQueryable();
        if (!string.IsNullOrEmpty(selectedBranch))
        {
            staffQuery = staffQuery.Where(s => s.BranchId == selectedBranch);
        }
        ViewBag.StaffList = new SelectList(staffQuery.OrderBy(s => s.Name), "Email", "Name", selectedStaff);

        var serviceQuery = db.Services.AsQueryable();
        if (!string.IsNullOrEmpty(selectedBranch))
        {
            serviceQuery = serviceQuery.Where(s => s.BranchId == selectedBranch);
        }
        ViewBag.ServiceList = new MultiSelectList(serviceQuery.OrderBy(s => s.Name), "Id", "Name", selectedServices);
    }

    // GET: Appointment/Book
    [Authorize(Roles = "Customer")]
    public IActionResult Book(string? branchId)
    {
        var firstBranch = branchId ?? db.Branches.Select(b => b.Id).FirstOrDefault() ?? "BR001";
        PopulateBookingLists(firstBranch);

        var vm = new AppointmentBookVM
        {
            BranchId = firstBranch,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSlot = new TimeOnly(9, 0)
        };
        return View(vm);
    }

    // POST: Appointment/Book
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult Book(AppointmentBookVM vm)
    {
        if (ModelState.IsValid("Date") && vm.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("Date", "Appointment date cannot be in the past.");
        }

        // C1: Validate time slot
        var allowedSlots = new[] { new TimeOnly(9, 0), new TimeOnly(10, 0), new TimeOnly(11, 0), new TimeOnly(14, 0), new TimeOnly(15, 0), new TimeOnly(16, 0) };
        if (!allowedSlots.Contains(vm.TimeSlot))
        {
            ModelState.AddModelError("TimeSlot", "Invalid time slot selected.");
        }

        // C2: Validate pet ownership
        string email = User.Identity!.Name!;
        if (!db.Pets.Any(p => p.Id == vm.PetId && p.CustomerEmail == email))
        {
            ModelState.AddModelError("PetId", "Selected pet does not belong to your account.");
        }

        if (vm.ServiceIds == null || vm.ServiceIds.Length == 0)
        {
            ModelState.AddModelError("ServiceIds", "Please select at least one grooming service.");
        }

        // C3: Validate services belong to selected branch
        if (vm.ServiceIds != null && vm.ServiceIds.Length > 0 && !string.IsNullOrEmpty(vm.BranchId))
        {
            var branchServiceIds = db.Services.Where(s => s.BranchId == vm.BranchId).Select(s => s.Id).ToList();
            var invalidServices = vm.ServiceIds.Where(s => !branchServiceIds.Contains(s)).ToList();
            if (invalidServices.Count > 0)
            {
                ModelState.AddModelError("ServiceIds", "One or more selected services are not available at the chosen branch.");
            }
        }

        // Auto-assign staff if customer didn't choose one
        if (string.IsNullOrEmpty(vm.StaffEmail))
        {
            var availableStaff = db.Staffs.Where(s => s.BranchId == vm.BranchId).ToList();
            if (availableStaff.Count > 0)
            {
                var bookedStaff = db.Appointments
                    .Where(a => a.BranchId == vm.BranchId && a.Date == vm.Date && a.TimeSlot == vm.TimeSlot && a.Status != "Cancelled")
                    .Select(a => a.StaffEmail)
                    .ToList();

                var freeStaff = availableStaff.FirstOrDefault(s => !bookedStaff.Contains(s.Email));
                vm.StaffEmail = freeStaff?.Email ?? availableStaff.First().Email;
            }
        }

        if (string.IsNullOrEmpty(vm.StaffEmail))
        {
            ModelState.AddModelError("StaffEmail", "No staff available for this branch.");
        }
        else
        {
            bool collision = db.Appointments.Any(a =>
                a.StaffEmail == vm.StaffEmail &&
                a.Date == vm.Date &&
                a.TimeSlot == vm.TimeSlot &&
                a.Status != "Cancelled");

            if (collision)
            {
                ModelState.AddModelError("TimeSlot", "The selected staff is already booked for this time slot. Please choose another time or staff.");
            }
        }

        if (ModelState.IsValid)
        {
            var appt = new Appointment
            {
                PetId = vm.PetId,
                BranchId = vm.BranchId,
                StaffEmail = vm.StaffEmail!,
                Date = vm.Date,
                TimeSlot = vm.TimeSlot,
                Status = "Pending",
                Notes = vm.Notes?.Trim(),
                CreatedAt = DateTime.Now
            };

            db.Appointments.Add(appt);

            foreach (var svId in vm.ServiceIds!)
            {
                var service = db.Services.FirstOrDefault(s => s.Id == svId && s.BranchId == vm.BranchId);
                if (service != null)
                {
                    db.AppointmentServices.Add(new AppointmentService
                    {
                        Appointment = appt,
                        ServiceId = service.Id,
                        Price = service.Price
                    });
                }
            }

            db.SaveChanges();

            // Send Confirmation Email & SMS Notification (Practical 08 AF)
            var customer = db.Customers.Find(User.Identity!.Name);
            var loadedAppt = db.Appointments
                .Include(a => a.Pet)
                .Include(a => a.Branch)
                .Include(a => a.Staff)
                .FirstOrDefault(a => a.Id == appt.Id);

            if (customer != null && loadedAppt != null)
            {
                hp.SendAppointmentConfirmationEmail(loadedAppt, customer.Email, customer.Name);
                hp.SendSms(db, customer.Email, $"PawPoint: Appointment #{appt.Id} for {loadedAppt.Pet?.Name} on {appt.Date:dd/MM} at {appt.TimeSlot} is received (Status: Pending).");
            }

            TempData["Info"] = $"Appointment <b>#{appt.Id}</b> booked successfully! A confirmation email has been dispatched.";
            return RedirectToAction("MyAppointments");
        }

        PopulateBookingLists(vm.BranchId, vm.PetId, vm.StaffEmail, vm.ServiceIds);
        return View(vm);
    }

    // GET: Appointment/Availability (Slot Matrix Grid - Practical 10 AF)
    public IActionResult Availability(string? branchId, DateOnly? date)
    {
        branchId ??= db.Branches.Select(b => b.Id).FirstOrDefault() ?? "BR001";
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        ViewBag.Branches = db.Branches.ToList();
        ViewBag.SelectedBranchId = branchId;
        ViewBag.Date = selectedDate;

        TimeOnly[] slots = [new(9, 0), new(10, 0), new(11, 0), new(14, 0), new(15, 0), new(16, 0)];
        ViewBag.Slots = slots;

        var staffList = db.Staffs.Where(s => s.BranchId == branchId).OrderBy(s => s.Name).ToList();

        var booked = db.Appointments
            .Where(a => a.BranchId == branchId && a.Date == selectedDate && a.Status != "Cancelled")
            .Select(a => new { a.StaffEmail, a.TimeSlot })
            .ToList();

        var matrix = new Dictionary<Staff, List<TimeOnly>>();
        foreach (var st in staffList)
        {
            var freeSlots = slots.Where(t => !booked.Any(b => b.StaffEmail == st.Email && b.TimeSlot == t)).ToList();
            matrix[st] = freeSlots;
        }

        return View(matrix);
    }

    // GET: Appointment/MyAppointments
    [Authorize(Roles = "Customer")]
    public IActionResult MyAppointments(string? status, int page = 1)
    {
        string email = User.Identity!.Name!;
        var q = db.Appointments
            .Include(a => a.Pet)
            .Include(a => a.Branch)
            .Include(a => a.Staff)
            .Include(a => a.Review)
            .Include(a => a.AppointmentServices).ThenInclude(x => x.Service)
            .Where(a => a.Pet.CustomerEmail == email && (a.Status == status || status == null));

        var m = q.OrderByDescending(a => a.Date).ThenByDescending(a => a.TimeSlot).ToPagedList(page, 5);
        return View(m);
    }

    // POST: Appointment/Cancel
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult Cancel(int id)
    {
        var appt = db.Appointments.Include(a => a.Pet).FirstOrDefault(a => a.Id == id);
        if (appt != null && appt.Pet.CustomerEmail == User.Identity!.Name)
        {
            if (appt.Status == "Completed")
            {
                TempData["Info"] = "Completed appointments cannot be cancelled.";
            }
            else
            {
                appt.Status = "Cancelled";
                db.SaveChanges();
                hp.SendSms(db, appt.Pet.CustomerEmail, $"PawPoint: Your appointment #{appt.Id} has been cancelled.");
                TempData["Info"] = $"Appointment <b>#{appt.Id}</b> has been cancelled.";
            }
        }
        return RedirectToAction("MyAppointments");
    }

    // GET: Appointment/StaffIndex
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult StaffIndex(string? branchId, string? status, int page = 1)
    {
        ViewBag.Branches = db.Branches.ToList();

        var q = db.Appointments
            .Include(a => a.Pet)
            .Include(a => a.Branch)
            .Include(a => a.Staff)
            .Include(a => a.AppointmentServices).ThenInclude(x => x.Service)
            .Where(a => (a.BranchId == branchId || branchId == null) && (a.Status == status || status == null));

        if (User.IsInRole("Staff"))
        {
            var staffEmail = User.Identity!.Name;
            q = q.Where(a => a.StaffEmail == staffEmail);
        }

        var m = q.OrderByDescending(a => a.Date).ThenByDescending(a => a.TimeSlot).ToPagedList(page, 6);
        return View(m);
    }

    // GET: Appointment/UpdateStatus
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult UpdateStatus(int id)
    {
        var a = db.Appointments.Include(x => x.Pet).Include(x => x.Staff).FirstOrDefault(x => x.Id == id);
        if (a == null) return RedirectToAction("StaffIndex");

        return View(new AppointmentStatusVM { Id = a.Id, Status = a.Status });
    }

    // POST: Appointment/UpdateStatus
    [Authorize(Roles = "Staff,Admin"), HttpPost]
    public IActionResult UpdateStatus(AppointmentStatusVM vm)
    {
        var a = db.Appointments.Include(x => x.Pet).Include(x => x.Branch).FirstOrDefault(x => x.Id == vm.Id);
        if (a == null) return RedirectToAction("StaffIndex");

        if (ModelState.IsValid)
        {
            a.Status = vm.Status;
            db.SaveChanges();

            // Dispatch SMS / Notification update
            if (a.Pet?.CustomerEmail != null)
            {
                hp.SendSms(db, a.Pet.CustomerEmail, $"PawPoint: Status of Appointment #{a.Id} is updated to '{a.Status}'.");
            }

            TempData["Info"] = $"Appointment <b>#{a.Id}</b> status updated to <b>{a.Status}</b>.";
            return RedirectToAction("StaffIndex");
        }

        return View(vm);
    }

    // GET: Appointment/Detail
    public IActionResult Detail(int id)
    {
        var a = db.Appointments
            .Include(x => x.Pet)
            .Include(x => x.Branch)
            .Include(x => x.Staff)
            .Include(x => x.Review)
            .Include(x => x.AppointmentServices).ThenInclude(x => x.Service)
            .FirstOrDefault(x => x.Id == id);

        if (a == null) return RedirectToAction(User.IsInRole("Staff") || User.IsInRole("Admin") ? "StaffIndex" : "MyAppointments");

        if (User.IsInRole("Customer") && a.Pet?.CustomerEmail != User.Identity!.Name)
            return RedirectToAction("MyAppointments");

        if (User.IsInRole("Staff"))
        {
            var staff = db.Staffs.Find(User.Identity!.Name);
            if (staff == null || staff.BranchId != a.BranchId)
                return RedirectToAction("StaffIndex");
        }

        return View(a);
    }

    // GET: Appointment/QrCheckIn (QR Check-In AF)
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult QrCheckIn(int id)
    {
        var a = db.Appointments.Include(x => x.Pet).Include(x => x.Branch).FirstOrDefault(x => x.Id == id);
        if (a == null)
        {
            TempData["Info"] = $"Appointment <b>#{id}</b> not found.";
            return RedirectToAction("StaffIndex");
        }

        if (User.IsInRole("Staff"))
        {
            var staff = db.Staffs.Find(User.Identity!.Name);
            if (staff == null || staff.BranchId != a.BranchId)
            {
                TempData["Info"] = "You do not have access to this appointment.";
                return RedirectToAction("StaffIndex");
            }
        }

        if (a.Status == "Pending")
        {
            a.Status = "Confirmed";
            db.SaveChanges();
            TempData["Info"] = $"<b>QR Check-In Successful!</b> Appointment <b>#{id}</b> is now <b>Confirmed</b>.";
        }
        else if (a.Status == "Confirmed")
        {
            a.Status = "Completed";
            db.SaveChanges();
            TempData["Info"] = $"<b>QR Check-In Successful!</b> Appointment <b>#{id}</b> is now <b>Completed</b>.";
        }
        else
        {
            TempData["Info"] = $"Appointment <b>#{id}</b> is already <b>{a.Status}</b>. No status change.";
        }

        return RedirectToAction("Detail", new { id });
    }
}
