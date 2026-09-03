using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Demo.Models;

#nullable disable warnings

public class LoginVM
{
    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; }

    [Required, StringLength(100, MinimumLength = 5), DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }

    [DisplayName("Security Answer")]
    public int Captcha { get; set; }
}

public class RegisterVM
{
    [Required, StringLength(100), EmailAddress, Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [Required, StringLength(100, MinimumLength = 5), DataType(DataType.Password)]
    public string Password { get; set; }

    [Required, StringLength(100, MinimumLength = 5), Compare("Password"), DataType(DataType.Password), DisplayName("Confirm Password")]
    public string ConfirmPassword { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(20), RegularExpression(@"01\d{8,9}", ErrorMessage = "Invalid {0} format (e.g. 0123456789).")]
    public string Phone { get; set; }

    public IFormFile? Photo { get; set; }

    public string? WebcamPhotoBase64 { get; set; }

    [DisplayName("Security Answer")]
    public int Captcha { get; set; }
}

public class VerifyOtpVM
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, StringLength(6, MinimumLength = 6), RegularExpression(@"\d{6}", ErrorMessage = "OTP must be 6 digits.")]
    public string Otp { get; set; }
}

public class UpdatePasswordVM
{
    [Required, StringLength(100, MinimumLength = 5), DataType(DataType.Password), DisplayName("Current Password")]
    public string Current { get; set; }

    [Required, StringLength(100, MinimumLength = 5), DataType(DataType.Password), DisplayName("New Password")]
    public string New { get; set; }

    [Required, StringLength(100, MinimumLength = 5), Compare("New"), DataType(DataType.Password), DisplayName("Confirm New Password")]
    public string ConfirmNew { get; set; }
}

public class ResetPasswordVM
{
    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; }

    [DisplayName("Security Answer")]
    public int Captcha { get; set; }
}

public class BranchInsertVM
{
    [Required, StringLength(6), RegularExpression(@"BR\d{3}", ErrorMessage = "Invalid {0} (must be BRxxx)."), Remote("CheckId", "Branch", ErrorMessage = "Duplicated {0}.")]
    public string Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(200)]
    public string Address { get; set; }

    [Required, StringLength(20)]
    public string Phone { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class BranchUpdateVM
{
    public string Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(200)]
    public string Address { get; set; }

    [Required, StringLength(20)]
    public string Phone { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class StaffInsertVM
{
    [Required, StringLength(100), EmailAddress, Remote("CheckEmail", "Staff", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(20)]
    public string Phone { get; set; }

    [Required, StringLength(100, MinimumLength = 5), DataType(DataType.Password)]
    public string Password { get; set; }

    [Required, StringLength(6), DisplayName("Branch")]
    public string BranchId { get; set; }

    public IFormFile? Photo { get; set; }

    [DisplayName("Assigned Services")]
    public string[] ServiceIds { get; set; } = [];
}

public class StaffUpdateVM
{
    public string Email { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(20)]
    public string Phone { get; set; }

    [Required, StringLength(6), DisplayName("Branch")]
    public string BranchId { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }

    [DisplayName("Assigned Services")]
    public string[] ServiceIds { get; set; } = [];
}

public class ServiceInsertVM
{
    [Required, StringLength(6), RegularExpression(@"SV\d{3}", ErrorMessage = "Invalid {0} (must be SVxxx)."), Remote("CheckId", "Service", ErrorMessage = "Duplicated {0}.")]
    public string Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(500)]
    public string Description { get; set; }

    [Required, Range(0.01, 999.99)]
    public decimal Price { get; set; }

    [Required, Range(10, 240), DisplayName("Duration (Minutes)")]
    public int DurationMinutes { get; set; }

    [Required, StringLength(6), DisplayName("Branch")]
    public string BranchId { get; set; }

    public IFormFile? Photo { get; set; }
}

public class ServiceUpdateVM
{
    public string Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(500)]
    public string Description { get; set; }

    [Required, Range(0.01, 999.99)]
    public decimal Price { get; set; }

    [Required, Range(10, 240), DisplayName("Duration (Minutes)")]
    public int DurationMinutes { get; set; }

    [Required, StringLength(6), DisplayName("Branch")]
    public string BranchId { get; set; }

    public string? PhotoURL { get; set; }
    public IFormFile? Photo { get; set; }
}

public class PetInsertVM
{
    [Required, StringLength(50)]
    public string Name { get; set; }

    [Required, StringLength(30), RegularExpression(@"Dog|Cat|Other", ErrorMessage = "Invalid {0}.")]
    public string Species { get; set; }

    [Required, StringLength(50)]
    public string Breed { get; set; }

    [DisplayName("Date of Birth")]
    public DateOnly? DOB { get; set; }

    public IFormFile? Photo { get; set; }

    public string? WebcamPhotoBase64 { get; set; }
}

public class PetUpdateVM
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; }

    [Required, StringLength(30), RegularExpression(@"Dog|Cat|Other", ErrorMessage = "Invalid {0}.")]
    public string Species { get; set; }

    [Required, StringLength(50)]
    public string Breed { get; set; }

    [DisplayName("Date of Birth")]
    public DateOnly? DOB { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }

    public string? WebcamPhotoBase64 { get; set; }
}

public class AppointmentBookVM
{
    [Required, DisplayName("Pet")]
    public int PetId { get; set; }

    [Required, StringLength(6), DisplayName("Branch")]
    public string BranchId { get; set; }

    [Required, DisplayName("Selected Services")]
    public string[] ServiceIds { get; set; } = [];

    [Required, DisplayName("Appointment Date")]
    public DateOnly Date { get; set; }

    [Required, DisplayName("Preferred Time Slot")]
    public TimeOnly TimeSlot { get; set; }

    [DisplayName("Preferred Staff (Optional)")]
    public string? StaffEmail { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}

public class AppointmentStatusVM
{
    public int Id { get; set; }

    [Required, RegularExpression(@"Pending|Confirmed|Completed|Cancelled", ErrorMessage = "Invalid {0}.")]
    public string Status { get; set; }
}

public class ProductInsertVM
{
    [Required, StringLength(6), RegularExpression(@"PR\d{3}", ErrorMessage = "Invalid {0} (must be PRxxx)."), Remote("CheckId", "Product", ErrorMessage = "Duplicated {0}.")]
    public string Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(500)]
    public string Description { get; set; }

    [Required, Range(0.01, 999.99)]
    public decimal Price { get; set; }

    [Required, Range(0, 9999)]
    public int Stock { get; set; }

    public IFormFile? Photo { get; set; }
}

public class ProductUpdateVM
{
    public string Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(500)]
    public string Description { get; set; }

    [Required, Range(0.01, 999.99)]
    public decimal Price { get; set; }

    [Required, Range(0, 9999)]
    public int Stock { get; set; }

    public string? PhotoURL { get; set; }
    public IFormFile? Photo { get; set; }
}

public class CheckoutVM
{
    [Required, StringLength(200), DisplayName("Delivery Address")]
    public string DeliveryAddress { get; set; }

    [Required, RegularExpression(@"Cash|Card|Deposit", ErrorMessage = "Invalid {0}.")]
    public string PaymentMethod { get; set; } = "Card";

    [DisplayName("Redeem Loyalty Points")]
    public bool RedeemPoints { get; set; }
}

public class ReviewInsertVM
{
    public int AppointmentId { get; set; }

    [Required, Range(1, 5), DisplayName("Rating (1-5 Stars)")]
    public int Rating { get; set; } = 5;

    [StringLength(500)]
    public string? Comment { get; set; }
}
