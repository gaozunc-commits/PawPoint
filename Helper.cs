using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Demo;

public class Helper(IWebHostEnvironment en,
                    IHttpContextAccessor ct,
                    IConfiguration cf)
{
    // ------------------------------------------------------------------------
    // Photo Upload & Webcam
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photos are allowed.";
        }
        else if (f.Length > 2 * 1024 * 1024)
        {
            return "Photo size cannot exceed 2MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var dir = Path.Combine(en.WebRootPath, folder);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }

    public string SavePhotoFromBase64(string base64Data, string folder)
    {
        try
        {
            if (string.IsNullOrEmpty(base64Data)) return "photo.jpg";
            var commaIndex = base64Data.IndexOf(',');
            if (commaIndex >= 0) base64Data = base64Data[(commaIndex + 1)..];
            var bytes = Convert.FromBase64String(base64Data);

            var file = Guid.NewGuid().ToString("n") + ".jpg";
            var dir = Path.Combine(en.WebRootPath, folder);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, file);

            var options = new ResizeOptions
            {
                Size = new(200, 200),
                Mode = ResizeMode.Crop,
            };

            using var stream = new MemoryStream(bytes);
            using var img = Image.Load(stream);
            img.Mutate(x => x.Resize(options));
            img.Save(path);

            return file;
        }
        catch
        {
            return "photo.jpg";
        }
    }

    public void DeletePhoto(string? file, string folder)
    {
        if (string.IsNullOrEmpty(file) || file == "photo.jpg" || file == "admin.jpg" || file.StartsWith("m")) return;
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch { }
        }
    }

    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password) => ph.HashPassword(0, password);

    public bool VerifyPassword(string hash, string password) =>
        ph.VerifyHashedPassword(0, hash, password) == PasswordVerificationResult.Success;

    public void SignIn(string email, string role, bool rememberMe)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new(claims, "Cookies");
        ClaimsPrincipal principal = new(identity);
        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null,
        };

        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut() => ct.HttpContext!.SignOutAsync();

    public string RandomPassword(int length = 8)
    {
        const string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Range(0, length).Select(_ => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    public string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    // ------------------------------------------------------------------------
    // Cart Helper Functions
    // ------------------------------------------------------------------------

    public Dictionary<string, int> GetCart() =>
        ct.HttpContext!.Session.Get<Dictionary<string, int>>("Cart") ?? [];

    public void SetCart(Dictionary<string, int>? cart = null)
    {
        if (cart == null || cart.Count == 0)
            ct.HttpContext!.Session.Remove("Cart");
        else
            ct.HttpContext!.Session.Set("Cart", cart);
    }

    public void ClearCart() => ct.HttpContext!.Session.Remove("Cart");

    // ------------------------------------------------------------------------
    // Email Helper Functions (Practical 08)
    // ------------------------------------------------------------------------

    public bool SendEmail(MailMessage mail)
    {
        try
        {
            string user = cf["Smtp:User"] ?? "liawcv1@gmail.com";
            string pass = cf["Smtp:Pass"] ?? "pztq znli gpjg tooe";
            string name = cf["Smtp:Name"] ?? "🐾 PawPoint Super Admin";
            string host = cf["Smtp:Host"] ?? "smtp.gmail.com";
            int port = cf.GetValue<int?>("Smtp:Port") ?? 587;

            mail.From = new MailAddress(user, name);

            using var smtp = new SmtpClient
            {
                Host = host,
                Port = port,
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass),
                Timeout = 10000,
            };

            smtp.Send(mail);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email Error]: {ex.Message}");
            return false;
        }
    }

    public void SendOtpEmail(string email, string name, string otp)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(email, name));
        mail.Subject = "🐾 PawPoint — Registration OTP Verification";
        mail.IsBodyHtml = true;
        mail.Body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 500px;'>
                <h2 style='color: #4CAF50;'>Welcome to PawPoint!</h2>
                <p>Dear <b>{name}</b>,</p>
                <p>Thank you for registering. Please enter the following 6-digit OTP code to verify your account:</p>
                <div style='font-size: 28px; font-weight: bold; color: #333; letter-spacing: 5px; padding: 10px; background: #f3f3f3; text-align: center; border-radius: 5px;'>{otp}</div>
                <p style='color: #666; margin-top: 15px;'>This code is valid for 10 minutes. If you did not make this request, please disregard this email.</p>
                <p>Warm regards,<br><b>PawPoint Team</b></p>
            </div>";
        SendEmail(mail);
    }

    public void SendResetPasswordEmail(User u, string password, string loginUrl)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "🐾 PawPoint — Password Reset Notice";
        mail.IsBodyHtml = true;

        string photoPath = Path.Combine(en.WebRootPath, "images", "favicon.png");
        if (File.Exists(photoPath))
        {
            try
            {
                var att = new Attachment(photoPath);
                att.ContentId = "pawlogo";
                mail.Attachments.Add(att);
            }
            catch { }
        }

        mail.Body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 500px;'>
                <h2 style='color: #d9534f;'>PawPoint Account Recovery</h2>
                <p>Dear <b>{u.Name}</b>,</p>
                <p>Your password has been reset successfully. Your temporary login password is:</p>
                <div style='font-size: 24px; font-weight: bold; color: #d9534f; padding: 10px; background: #fee; text-align: center; border-radius: 5px;'>{password}</div>
                <p style='margin-top: 15px;'>Please <a href='{loginUrl}' style='color: #0275d8;'>click here to login</a> and change your password immediately.</p>
                <p>From,<br>🐾 <b>PawPoint Super Admin</b></p>
            </div>";
        SendEmail(mail);
    }

    public void SendAppointmentConfirmationEmail(Appointment appt, string customerEmail, string customerName)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(customerEmail, customerName));
        mail.Subject = $"🐾 PawPoint — Appointment Booking Confirmed (#{appt.Id})";
        mail.IsBodyHtml = true;
        mail.Body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 500px;'>
                <h2 style='color: #0275d8;'>Appointment Confirmed!</h2>
                <p>Dear <b>{customerName}</b>,</p>
                <p>Your grooming appointment for <b>{appt.Pet?.Name ?? "your pet"}</b> is booked:</p>
                <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
                    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'><b>Appointment ID:</b></td><td style='padding: 6px; border-bottom: 1px solid #eee;'>#{appt.Id}</td></tr>
                    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'><b>Date & Time:</b></td><td style='padding: 6px; border-bottom: 1px solid #eee;'>{appt.Date:yyyy-MM-dd} at {appt.TimeSlot}</td></tr>
                    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'><b>Branch:</b></td><td style='padding: 6px; border-bottom: 1px solid #eee;'>{appt.Branch?.Name}</td></tr>
                    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'><b>Assigned Staff:</b></td><td style='padding: 6px; border-bottom: 1px solid #eee;'>{appt.Staff?.Name ?? "Assigned Staff"}</td></tr>
                    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'><b>Status:</b></td><td style='padding: 6px; border-bottom: 1px solid #eee;'>{appt.Status}</td></tr>
                </table>
                <p style='margin-top: 15px;'>Please arrive 10 minutes prior to your time slot. Show your appointment QR code at reception for quick check-in!</p>
                <p>Thank you for choosing PawPoint!</p>
            </div>";
        SendEmail(mail);
    }

    public void SendAppointmentReminderEmail(Appointment appt, string customerEmail, string customerName)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(customerEmail, customerName));
        mail.Subject = $"🐾 Reminder: Grooming Appointment Tomorrow (#{appt.Id})";
        mail.IsBodyHtml = true;
        mail.Body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 500px;'>
                <h2 style='color: #f0ad4e;'>Upcoming Appointment Reminder</h2>
                <p>Dear <b>{customerName}</b>,</p>
                <p>This is a friendly reminder that your pet <b>{appt.Pet?.Name}</b> has an appointment tomorrow:</p>
                <p><b>Date:</b> {appt.Date:yyyy-MM-dd}<br><b>Time:</b> {appt.TimeSlot}<br><b>Branch:</b> {appt.Branch?.Name}</p>
                <p>See you soon!<br><b>PawPoint Team</b></p>
            </div>";
        SendEmail(mail);
    }

    public void SendOrderReceiptEmail(Order order, Customer customer)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(customer.Email, customer.Name));
        mail.Subject = $"🐾 PawPoint — Order #{order.Id} E-Receipt";
        mail.IsBodyHtml = true;

        decimal total = order.OrderLines.Sum(l => l.Price * l.Quantity);
        var rows = string.Join("", order.OrderLines.Select(l =>
            $"<tr><td style='padding: 6px; border-bottom: 1px solid #eee;'>{l.Product?.Name ?? l.ProductId}</td><td style='padding: 6px; border-bottom: 1px solid #eee; text-align: center;'>{l.Quantity}</td><td style='padding: 6px; border-bottom: 1px solid #eee; text-align: right;'>RM {l.Price:0.00}</td><td style='padding: 6px; border-bottom: 1px solid #eee; text-align: right;'>RM {(l.Price * l.Quantity):0.00}</td></tr>"
        ));

        mail.Body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 550px;'>
                <h2 style='color: #4CAF50;'>Thank You for Your Order!</h2>
                <p>Dear <b>{customer.Name}</b>,</p>
                <p>Here is your official e-receipt for Order <b>#{order.Id}</b> placed on <b>{order.Date:yyyy-MM-dd}</b>:</p>
                <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                    <tr style='background: #f7f7f7;'>
                        <th style='padding: 6px; text-align: left; border-bottom: 2px solid #ddd;'>Product</th>
                        <th style='padding: 6px; text-align: center; border-bottom: 2px solid #ddd;'>Qty</th>
                        <th style='padding: 6px; text-align: right; border-bottom: 2px solid #ddd;'>Price</th>
                        <th style='padding: 6px; text-align: right; border-bottom: 2px solid #ddd;'>Total</th>
                    </tr>
                    {rows}
                    <tr>
                        <td colspan='3' style='padding: 10px 6px; text-align: right; font-weight: bold;'>Grand Total:</td>
                        <td style='padding: 10px 6px; text-align: right; font-weight: bold; color: #4CAF50;'>RM {total:0.00}</td>
                    </tr>
                </table>
                <p style='margin-top: 15px;'><b>Payment Status:</b> {(order.Paid ? "PAID" : "PENDING")}</p>
                <p>PawPoint &middot; High Quality Pet Supplies & Care</p>
            </div>";
        SendEmail(mail);
    }

    public void SendSms(DB db, string customerEmail, string message)
    {
        db.Notifications.Add(new Notification
        {
            CustomerEmail = customerEmail,
            Channel = "SMS",
            Message = message,
            SentAt = DateTime.Now,
        });
        db.SaveChanges();
    }

    // ------------------------------------------------------------------------
    // DateTime Helper Functions
    // ------------------------------------------------------------------------

    public SelectList GetMonthList()
    {
        var list = new List<object>();
        for (int n = 1; n <= 12; n++)
        {
            list.Add(new
            {
                Id = n,
                Name = new DateTime(1, n, 1).ToString("MMMM"),
            });
        }
        return new SelectList(list, "Id", "Name");
    }

    public SelectList GetYearList(int min, int max, bool reverse = false)
    {
        var list = new List<int>();
        for (int n = min; n <= max; n++) list.Add(n);
        if (reverse) list.Reverse();
        return new SelectList(list);
    }
}
