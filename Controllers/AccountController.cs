using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class AccountController(DB db, Helper hp) : Controller
{
    // GET: Account/Login
    public IActionResult Login(string? returnURL)
    {
        SetCaptcha();
        ViewBag.ReturnURL = returnURL;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        int failures = HttpContext.Session.GetInt32("LoginFailures") ?? 0;
        DateTime? blockedUntil = HttpContext.Session.Get<DateTime?>("BlockedUntil");

        if (blockedUntil != null && blockedUntil > DateTime.Now)
        {
            var remaining = (blockedUntil.Value - DateTime.Now).Minutes + 1;
            ModelState.AddModelError("", $"Account login temporarily blocked due to multiple failed attempts. Please retry in {remaining} minute(s).");
            SetCaptcha();
            return View(vm);
        }

        int answer = HttpContext.Session.GetInt32("CaptchaAnswer") ?? -1;
        if (ModelState.IsValid("Captcha") && vm.Captcha != answer)
        {
            ModelState.AddModelError("Captcha", "Incorrect security answer.");
        }

        var u = db.Users.Find(vm.Email);
        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            ModelState.AddModelError("", "Invalid email or password.");
        }

        if (ModelState.IsValid)
        {
            HttpContext.Session.Remove("LoginFailures");
            HttpContext.Session.Remove("BlockedUntil");
            TempData["Info"] = $"Welcome back, <b>{u!.Name}</b> ({u.Role})!";
            hp.SignIn(u.Email, u.Role, vm.RememberMe);
            return !string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL) ? Redirect(returnURL) : RedirectToAction("Index", "Home");
        }

        failures++;
        HttpContext.Session.SetInt32("LoginFailures", failures);
        if (failures >= 3)
        {
            var blockTime = DateTime.Now.AddMinutes(2);
            HttpContext.Session.Set("BlockedUntil", blockTime);
            TempData["Info"] = "Too many failed attempts. Login is locked for 2 minutes.";
        }

        SetCaptcha();
        return View(vm);
    }

    // GET: Account/Logout
    public IActionResult Logout()
    {
        TempData["Info"] = "You have been logged out successfully.";
        hp.SignOut();
        return RedirectToAction("Index", "Home");
    }

    // POST: Account/DemoLogin (demo quick-login, skips captcha)
    [HttpPost, IgnoreAntiforgeryToken]
    public IActionResult DemoLogin(string email, string password)
    {
        int failures = HttpContext.Session.GetInt32("LoginFailures") ?? 0;
        DateTime? blockedUntil = HttpContext.Session.Get<DateTime?>("BlockedUntil");

        if (blockedUntil != null && blockedUntil > DateTime.Now)
            return BadRequest(new { error = "Account temporarily locked. Please wait." });

        var u = db.Users.Find(email);
        if (u == null || !hp.VerifyPassword(u.Hash, password))
        {
            failures++;
            HttpContext.Session.SetInt32("LoginFailures", failures);
            if (failures >= 3)
            {
                HttpContext.Session.Set("BlockedUntil", DateTime.Now.AddMinutes(2));
                return BadRequest(new { error = "Too many failed attempts. Locked for 2 minutes." });
            }
            return BadRequest(new { error = "Invalid demo credentials" });
        }

        HttpContext.Session.Remove("LoginFailures");
        HttpContext.Session.Remove("BlockedUntil");
        hp.SignIn(u.Email, u.Role, false);
        return Json(new { redirect = Url.Action("Index", "Home") });
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied() => View();

    // GET: Account/CheckEmail
    public bool CheckEmail(string email) => !db.Users.Any(u => u.Email == email);

    // GET: Account/Register
    public IActionResult Register()
    {
        SetCaptcha();
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        int answer = HttpContext.Session.GetInt32("CaptchaAnswer") ?? -1;
        if (ModelState.IsValid("Captcha") && vm.Captcha != answer)
        {
            ModelState.AddModelError("Captcha", "Incorrect security answer.");
        }

        if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

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
                photoUrl = hp.SavePhotoFromBase64(vm.WebcamPhotoBase64, "photos");
            }
            else if (vm.Photo != null)
            {
                photoUrl = hp.SavePhoto(vm.Photo, "photos");
            }

            // Generate OTP for email verification (Practical 08 AF)
            string otp = hp.GenerateOtp();
            HttpContext.Session.SetString("Pending_Email", vm.Email);
            HttpContext.Session.SetString("Pending_Name", vm.Name);
            HttpContext.Session.SetString("Pending_Phone", vm.Phone);
            HttpContext.Session.SetString("Pending_Hash", hp.HashPassword(vm.Password));
            HttpContext.Session.SetString("Pending_PhotoURL", photoUrl);
            HttpContext.Session.SetString("Pending_OTP", otp);
            HttpContext.Session.Set("Pending_OTP_Expires", DateTime.Now.AddMinutes(10));

            hp.SendOtpEmail(vm.Email, vm.Name, otp);

            TempData["Info"] = $"Verification OTP sent to <b>{vm.Email}</b>. (Demo Code: {otp})";
            return RedirectToAction("VerifyOtp", new { email = vm.Email });
        }

        SetCaptcha();
        return View(vm);
    }

    // GET: Account/VerifyOtp
    public IActionResult VerifyOtp(string? email)
    {
        email ??= HttpContext.Session.GetString("Pending_Email");
        if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");
        return View(new VerifyOtpVM { Email = email });
    }

    // POST: Account/VerifyOtp
    [HttpPost]
    public IActionResult VerifyOtp(VerifyOtpVM vm)
    {
        string? storedOtp = HttpContext.Session.GetString("Pending_OTP");
        string? pendingEmail = HttpContext.Session.GetString("Pending_Email");
        DateTime? expires = HttpContext.Session.Get<DateTime?>("Pending_OTP_Expires");

        if (string.IsNullOrEmpty(storedOtp) || pendingEmail != vm.Email || (expires != null && expires < DateTime.Now))
        {
            ModelState.AddModelError("", "OTP expired or invalid. Please register again.");
        }
        else if (vm.Otp != storedOtp)
        {
            ModelState.AddModelError("Otp", "Invalid OTP verification code.");
        }

        if (ModelState.IsValid)
        {
            var customer = new Customer
            {
                Email = HttpContext.Session.GetString("Pending_Email")!,
                Name = HttpContext.Session.GetString("Pending_Name")!,
                Phone = HttpContext.Session.GetString("Pending_Phone")!,
                Hash = HttpContext.Session.GetString("Pending_Hash")!,
                PhotoURL = HttpContext.Session.GetString("Pending_PhotoURL") ?? "photo.jpg",
                Points = 50, // Welcome reward bonus
            };

            db.Customers.Add(customer);
            db.RewardTransactions.Add(new RewardTransaction
            {
                CustomerEmail = customer.Email,
                Points = 50,
                Reason = "Welcome bonus registration reward",
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();

            // Clear pending session
            HttpContext.Session.Remove("Pending_Email");
            HttpContext.Session.Remove("Pending_Name");
            HttpContext.Session.Remove("Pending_Phone");
            HttpContext.Session.Remove("Pending_Hash");
            HttpContext.Session.Remove("Pending_PhotoURL");
            HttpContext.Session.Remove("Pending_OTP");
            HttpContext.Session.Remove("Pending_OTP_Expires");

            hp.SignIn(customer.Email, "Customer", false);
            TempData["Info"] = "Registration and verification complete! You earned <b>50 welcome loyalty points</b>.";
            return RedirectToAction("Index", "Home");
        }

        return View(vm);
    }

    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword() => View();

    // POST: Account/UpdatePassword
    [Authorize, HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password does not match.");
        }

        if (ModelState.IsValid)
        {
            u.Hash = hp.HashPassword(vm.New);
            db.SaveChanges();
            TempData["Info"] = "Password updated successfully.";
            return RedirectToAction("Index", "Home");
        }

        return View(vm);
    }

    // GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        SetCaptcha();
        return View();
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        int failures = HttpContext.Session.GetInt32("ResetFailures") ?? 0;
        DateTime? blockedUntil = HttpContext.Session.Get<DateTime?>("ResetBlockedUntil");

        if (blockedUntil != null && blockedUntil > DateTime.Now)
        {
            var remaining = (blockedUntil.Value - DateTime.Now).Minutes + 1;
            ModelState.AddModelError("", $"Too many reset attempts. Please retry in {remaining} minute(s).");
            SetCaptcha();
            return View(vm);
        }

        int answer = HttpContext.Session.GetInt32("CaptchaAnswer") ?? -1;
        if (ModelState.IsValid("Captcha") && vm.Captcha != answer)
        {
            ModelState.AddModelError("Captcha", "Incorrect security answer.");
        }

        var u = db.Users.Find(vm.Email);
        if (u == null)
        {
            ModelState.AddModelError("Email", "If an account exists, reset instructions have been sent.");
        }

        if (ModelState.IsValid)
        {
            HttpContext.Session.Remove("ResetFailures");
            HttpContext.Session.Remove("ResetBlockedUntil");

            var tempPass = hp.RandomPassword(8);
            u!.Hash = hp.HashPassword(tempPass);
            db.SaveChanges();

            var loginUrl = Url.Action("Login", "Account", null, Request.Scheme) ?? "/Account/Login";
            hp.SendResetPasswordEmail(u, tempPass, loginUrl);

            TempData["Info"] = $"Password reset instructions sent to <b>{u.Email}</b>. (Temporary Demo Password: <b>{tempPass}</b>)";
            return RedirectToAction("Login");
        }

        failures++;
        HttpContext.Session.SetInt32("ResetFailures", failures);
        if (failures >= 3)
        {
            HttpContext.Session.Set("ResetBlockedUntil", DateTime.Now.AddMinutes(2));
            TempData["Info"] = "Too many reset attempts. Locked for 2 minutes.";
        }

        SetCaptcha();
        return View(vm);
    }

    private void SetCaptcha()
    {
        int a = Random.Shared.Next(2, 9);
        int b = Random.Shared.Next(2, 9);
        HttpContext.Session.SetInt32("CaptchaAnswer", a + b);
        ViewBag.CaptchaQuestion = $"{a} + {b} = ?";
    }
}
