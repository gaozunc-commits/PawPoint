using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace Demo.Controllers;

[Authorize(Roles = "Customer")]
public class OrderController(DB db, Helper hp) : Controller
{
    // GET: Order/Checkout
    public IActionResult Checkout()
    {
        var cart = hp.GetCart();
        if (cart.Count == 0)
        {
            TempData["Info"] = "Your shopping cart is empty.";
            return RedirectToAction("Catalog", "Product");
        }

        var customer = db.Customers.Find(User.Identity!.Name);
        ViewBag.CustomerPoints = customer?.Points ?? 0;

        return View(new CheckoutVM());
    }

    // POST: Order/Checkout
    [HttpPost]
    public IActionResult Checkout(CheckoutVM vm)
    {
        var cart = hp.GetCart();
        if (cart.Count == 0)
        {
            ModelState.AddModelError("", "Your cart is empty.");
        }

        // Verify stock
        foreach (var (pId, qty) in cart)
        {
            var p = db.Products.Find(pId);
            if (p == null)
            {
                ModelState.AddModelError("", $"Product {pId} is no longer available.");
            }
            else if (p.Stock < qty)
            {
                ModelState.AddModelError("", $"Insufficient stock for {p.Name} (only {p.Stock} available).");
            }
        }

        var customer = db.Customers.Find(User.Identity!.Name);
        ViewBag.CustomerPoints = customer?.Points ?? 0;

        if (ModelState.IsValid && customer != null)
        {
            var order = new Order
            {
                CustomerEmail = customer.Email,
                Date = DateOnly.FromDateTime(DateTime.Today),
                Status = vm.PaymentMethod == "Cash" ? "Pending" : "Paid",
                Paid = vm.PaymentMethod != "Cash",
                DeliveryAddress = vm.DeliveryAddress.Trim()
            };

            db.Orders.Add(order);

            decimal subtotal = 0;
            foreach (var (pId, qty) in cart)
            {
                var p = db.Products.Find(pId);
                if (p == null)
                {
                    ModelState.AddModelError("", $"Product {pId} is no longer available.");
                    continue;
                }
                p.Stock -= qty;

                var line = new OrderLine
                {
                    Order = order,
                    ProductId = p.Id,
                    Quantity = qty,
                    Price = p.Price
                };
                db.OrderLines.Add(line);
                subtotal += p.Price * qty;
            }

            // Loyalty Points Redemption (10 points = RM 1 discount)
            if (vm.RedeemPoints && customer.Points >= 10)
            {
                int maxRedeemPoints = Math.Min(customer.Points, (int)(subtotal * 10));
                customer.Points -= maxRedeemPoints;
                db.RewardTransactions.Add(new RewardTransaction
                {
                    CustomerEmail = customer.Email,
                    Points = -maxRedeemPoints,
                    Reason = $"Redeemed discount on Order #{order.Id}",
                    CreatedAt = DateTime.Now
                });
            }

            // Award loyalty points for order purchase (+1 point per RM 10 spent)
            int earnedPoints = Math.Max(5, (int)(subtotal / 10));
            customer.Points += earnedPoints;
            db.RewardTransactions.Add(new RewardTransaction
            {
                CustomerEmail = customer.Email,
                Points = earnedPoints,
                Reason = $"Points earned from Order #{order.Id} purchase",
                CreatedAt = DateTime.Now
            });

            db.SaveChanges();
            hp.ClearCart();

            // Send Order E-Receipt Email & SMS (Practical 08 AF)
            var loadedOrder = db.Orders.Include(o => o.OrderLines).ThenInclude(l => l.Product).FirstOrDefault(o => o.Id == order.Id);
            if (loadedOrder != null)
            {
                hp.SendOrderReceiptEmail(loadedOrder, customer);
                hp.SendSms(db, customer.Email, $"PawPoint: Order #{order.Id} confirmed! Total RM {subtotal:0.00}. You earned {earnedPoints} loyalty points.");
            }

            TempData["Info"] = $"Order <b>#{order.Id}</b> placed successfully! E-receipt sent to your email.";
            return RedirectToAction("OrderComplete", new { id = order.Id });
        }

        return View(vm);
    }

    // GET: Order/OrderComplete
    public IActionResult OrderComplete(int id)
    {
        var o = db.Orders.Include(x => x.OrderLines).ThenInclude(l => l.Product)
                         .FirstOrDefault(x => x.Id == id && x.CustomerEmail == User.Identity!.Name);
        if (o == null) return RedirectToAction("Catalog", "Product");
        return View(o);
    }

    // GET: Order/OrderHistory
    public IActionResult OrderHistory(int page = 1)
    {
        string email = User.Identity!.Name!;
        var orders = db.Orders
            .Include(o => o.OrderLines).ThenInclude(l => l.Product)
            .Where(o => o.CustomerEmail == email)
            .OrderByDescending(o => o.Id)
            .ToPagedList(page, 5);

        return View(orders);
    }

    // GET: Order/OrderDetail
    public IActionResult OrderDetail(int id)
    {
        var o = db.Orders.Include(x => x.OrderLines).ThenInclude(l => l.Product)
                         .FirstOrDefault(x => x.Id == id && x.CustomerEmail == User.Identity!.Name);
        if (o == null) return RedirectToAction("OrderHistory");
        return View(o);
    }

    // POST: Order/PayDeposit (Stripe / Deposit payment AF)
    [HttpPost]
    public IActionResult PayDeposit(int id)
    {
        var o = db.Orders.Find(id);
        if (o != null && o.CustomerEmail == User.Identity!.Name && o.Status == "Pending")
        {
            o.Paid = true;
            o.Status = "Paid";
            db.SaveChanges();
            TempData["Info"] = $"Online payment for Order <b>#{id}</b> processed successfully.";
        }
        return RedirectToAction("OrderDetail", new { id });
    }
}
