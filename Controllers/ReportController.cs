using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class ReportController(DB db) : Controller
{
    // GET: Report/RevenueChart
    public IActionResult RevenueChart()
    {
        var orderLines = db.OrderLines.Include(l => l.Product).Include(l => l.Order).ToList();
        var apptServices = db.AppointmentServices.Include(s => s.Service).Include(s => s.Appointment).ToList();

        decimal productRevenue = orderLines.Where(l => l.Order?.Paid == true).Sum(l => l.Price * l.Quantity);
        decimal serviceRevenue = apptServices.Where(a => a.Appointment?.Status == "Completed").Sum(a => a.Price);

        ViewBag.ProductRevenue = productRevenue;
        ViewBag.ServiceRevenue = serviceRevenue;
        ViewBag.TotalRevenue = productRevenue + serviceRevenue;

        // Group by product sales
        var topProducts = orderLines
            .GroupBy(l => l.Product?.Name ?? l.ProductId)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Price * x.Quantity), Qty = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Total)
            .ToList();

        ViewBag.TopProducts = topProducts;

        // Group by service sales
        var topServices = apptServices
            .GroupBy(s => s.Service?.Name ?? s.ServiceId)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Price), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .ToList();

        ViewBag.TopServices = topServices;

        return View();
    }

    // GET: Report/BookingsChart
    public IActionResult BookingsChart()
    {
        var appts = db.Appointments.Include(a => a.Branch).ToList();

        var byStatus = appts
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        var byBranch = appts
            .GroupBy(a => a.Branch?.Name ?? a.BranchId)
            .Select(g => new { Branch = g.Key, Count = g.Count() })
            .ToList();

        ViewBag.ByStatus = byStatus;
        ViewBag.ByBranch = byBranch;
        ViewBag.TotalBookings = appts.Count;

        return View();
    }

    // GET: Report/LoyaltyChart
    public IActionResult LoyaltyChart()
    {
        var customers = db.Customers.OrderByDescending(c => c.Points).ToList();
        var transactions = db.RewardTransactions.OrderByDescending(t => t.CreatedAt).Take(20).ToList();

        ViewBag.TotalPointsIssued = db.RewardTransactions.Where(t => t.Points > 0).Sum(t => (int?)t.Points) ?? 0;
        ViewBag.TotalPointsRedeemed = Math.Abs(db.RewardTransactions.Where(t => t.Points < 0).Sum(t => (int?)t.Points) ?? 0);
        ViewBag.Transactions = transactions;

        return View(customers);
    }
}
