using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace Demo.Controllers;

public class ProductController(DB db, Helper hp) : Controller
{
    // GET: Product/Catalog
    public IActionResult Catalog(string? name, string? sort, string? dir, int page = 1)
    {
        name = name?.Trim() ?? "";
        var q = db.Products.Where(x => x.Name.Contains(name) || x.Description.Contains(name));

        Func<Product, object> fn = sort switch
        {
            "Name" => x => x.Name,
            "Price" => x => x.Price,
            "Stock" => x => x.Stock,
            _ => x => x.Id
        };

        var list = dir == "des" ? q.OrderByDescending(fn) : q.OrderBy(fn);
        var m = list.ToPagedList(page, 6);

        if (page < 1) return RedirectToAction(null, new { name, sort, dir, page = 1 });
        if (page > m.PageCount && m.PageCount > 0) return RedirectToAction(null, new { name, sort, dir, page = m.PageCount });

        if (Request.IsAjax()) return PartialView("_A", m);
        return View(m);
    }

    // GET: Product/CheckId
    [Authorize(Roles = "Admin")]
    public bool CheckId(string id) => !db.Products.Any(x => x.Id == id);

    // GET: Product/Insert
    [Authorize(Roles = "Admin")]
    public IActionResult Insert() => View();

    // POST: Product/Insert
    [Authorize(Roles = "Admin"), HttpPost]
    public IActionResult Insert(ProductInsertVM vm)
    {
        if (ModelState.IsValid("Id") && db.Products.Any(x => x.Id == vm.Id))
        {
            ModelState.AddModelError("Id", "Duplicated Product Id.");
        }

        if (vm.Photo != null && ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            var p = new Product
            {
                Id = vm.Id.ToUpper().Trim(),
                Name = vm.Name.Trim(),
                Description = vm.Description.Trim(),
                Price = vm.Price,
                Stock = vm.Stock,
                PhotoURL = vm.Photo == null ? "photo.jpg" : hp.SavePhoto(vm.Photo, "products")
            };

            db.Products.Add(p);
            db.SaveChanges();

            TempData["Info"] = $"Product <b>{p.Name}</b> added to catalog.";
            return RedirectToAction("Catalog");
        }

        return View(vm);
    }

    // GET: Product/Update
    [Authorize(Roles = "Admin")]
    public IActionResult Update(string? id)
    {
        var p = db.Products.Find(id);
        if (p == null) return RedirectToAction("Catalog");

        return View(new ProductUpdateVM
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            PhotoURL = p.PhotoURL
        });
    }

    // POST: Product/Update
    [Authorize(Roles = "Admin"), HttpPost]
    public IActionResult Update(ProductUpdateVM vm)
    {
        var p = db.Products.Find(vm.Id);
        if (p == null) return RedirectToAction("Catalog");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            p.Name = vm.Name.Trim();
            p.Description = vm.Description.Trim();
            p.Price = vm.Price;
            p.Stock = vm.Stock;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(p.PhotoURL, "products");
                p.PhotoURL = hp.SavePhoto(vm.Photo, "products");
            }

            db.SaveChanges();
            TempData["Info"] = $"Product <b>{p.Name}</b> updated successfully.";
            return RedirectToAction("Catalog");
        }

        return View(vm);
    }

    // POST: Product/Delete
    [Authorize(Roles = "Admin"), HttpPost]
    public IActionResult Delete(string? id)
    {
        var p = db.Products.Find(id);
        if (p != null)
        {
            if (db.OrderLines.Any(ol => ol.ProductId == id))
            {
                TempData["Info"] = $"Cannot delete product <b>{p.Name}</b> because it exists in past order records.";
            }
            else
            {
                hp.DeletePhoto(p.PhotoURL, "products");
                db.Products.Remove(p);
                db.SaveChanges();
                TempData["Info"] = $"Product <b>{p.Name}</b> deleted.";
            }
        }
        return RedirectToAction("Catalog");
    }

    // POST: Product/AddToCart
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult AddToCart(string id)
    {
        var p = db.Products.Find(id);
        if (p == null || p.Stock <= 0)
        {
            TempData["Info"] = "Sorry, this product is currently out of stock.";
            return RedirectToAction("Catalog");
        }

        var cart = hp.GetCart();
        int currentQty = cart.GetValueOrDefault(id, 0);

        if (currentQty + 1 > p.Stock)
        {
            TempData["Info"] = $"Cannot add more. Only {p.Stock} unit(s) available.";
        }
        else
        {
            cart[id] = currentQty + 1;
            hp.SetCart(cart);
            TempData["Info"] = $"Added <b>{p.Name}</b> to cart ({cart[id]} in cart).";
        }

        return RedirectToAction("Catalog");
    }

    // GET: Product/Cart
    [Authorize(Roles = "Customer")]
    public IActionResult Cart()
    {
        var cart = hp.GetCart();
        var productIds = cart.Keys.ToList();
        var products = db.Products.Where(p => productIds.Contains(p.Id)).ToList();
        return View(products);
    }

    // POST: Product/UpdateCart
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult UpdateCart(string id, int quantity)
    {
        var cart = hp.GetCart();
        var p = db.Products.Find(id);

        if (quantity <= 0)
        {
            cart.Remove(id);
            TempData["Info"] = "Item removed from cart.";
        }
        else if (p != null)
        {
            int allowed = Math.Min(Math.Min(quantity, 20), p.Stock);
            cart[id] = allowed;
            TempData["Info"] = allowed < quantity
                ? $"Cart updated. Quantity adjusted to {allowed} (only {p.Stock} in stock)."
                : "Cart updated.";
        }

        hp.SetCart(cart);
        return RedirectToAction("Cart");
    }

    // POST: Product/RemoveFromCart
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult RemoveFromCart(string id)
    {
        var cart = hp.GetCart();
        cart.Remove(id);
        hp.SetCart(cart);
        TempData["Info"] = "Item removed from cart.";
        return RedirectToAction("Cart");
    }
}
