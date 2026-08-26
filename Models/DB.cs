using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<StaffService> StaffServices { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentService> AppointmentServices { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<RewardTransaction> RewardTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.Entity<StaffService>().HasKey(x => new { x.StaffEmail, x.ServiceId });
        mb.Entity<StaffService>().HasOne(x => x.Staff).WithMany(x => x.StaffServices).HasForeignKey(x => x.StaffEmail).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<StaffService>().HasOne(x => x.Service).WithMany(x => x.StaffServices).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        
        mb.Entity<AppointmentService>().HasKey(x => new { x.AppointmentId, x.ServiceId });
        mb.Entity<AppointmentService>().HasOne(x => x.Appointment).WithMany(x => x.AppointmentServices).HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<AppointmentService>().HasOne(x => x.Service).WithMany(x => x.AppointmentServices).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        
        mb.Entity<Appointment>().HasOne(x => x.Staff).WithMany(x => x.Appointments).HasForeignKey(x => x.StaffEmail).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Appointment>().HasOne(x => x.Branch).WithMany(x => x.Appointments).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Appointment>().HasOne(x => x.Review).WithOne(x => x.Appointment).HasForeignKey<Review>(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        
        mb.Entity<Review>().HasOne(x => x.Customer).WithMany(x => x.Reviews).HasForeignKey(x => x.CustomerEmail).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<OrderLine>().HasOne(x => x.Order).WithMany(x => x.OrderLines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<OrderLine>().HasOne(x => x.Product).WithMany(x => x.OrderLines).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class User
{
    [Key, MaxLength(100)] public string Email { get; set; }
    [MaxLength(100)] public string Hash { get; set; }
    [MaxLength(100)] public string Name { get; set; }
    [MaxLength(20)] public string Phone { get; set; }
    [NotMapped] public string Role => GetType().Name;
}

public class Admin : User { }

public class Staff : User
{
    [MaxLength(100)] public string PhotoURL { get; set; }
    [MaxLength(6)] public string BranchId { get; set; }
    public Branch Branch { get; set; }
    public ICollection<StaffService> StaffServices { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}

public class Customer : User
{
    [MaxLength(100)] public string PhotoURL { get; set; }
    public int Points { get; set; }
    public ICollection<Pet> Pets { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}

public class Branch
{
    [Key, MaxLength(6), RegularExpression(@"BR\d{3}", ErrorMessage = "Invalid {0}.")]
    public string Id { get; set; }
    [MaxLength(100)] public string Name { get; set; }
    [MaxLength(200)] public string Address { get; set; }
    [MaxLength(20)] public string Phone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public ICollection<Staff> Staff { get; set; } = [];
    public ICollection<Service> Services { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}

public class Service
{
    [Key, MaxLength(6), RegularExpression(@"SV\d{3}", ErrorMessage = "Invalid {0}.")]
    public string Id { get; set; }
    [MaxLength(100)] public string Name { get; set; }
    [MaxLength(500)] public string Description { get; set; }
    [Column(TypeName = "decimal(8,2)"), Range(0.01, 999.99)]
    public decimal Price { get; set; }
    [Range(10, 240)] public int DurationMinutes { get; set; }
    [MaxLength(100)] public string PhotoURL { get; set; }
    [MaxLength(6)] public string BranchId { get; set; }
    public Branch Branch { get; set; }
    public ICollection<StaffService> StaffServices { get; set; } = [];
    public ICollection<AppointmentService> AppointmentServices { get; set; } = [];
}

public class StaffService
{
    [Key, Column(Order = 0), MaxLength(100)] public string StaffEmail { get; set; }
    [Key, Column(Order = 1), MaxLength(6)] public string ServiceId { get; set; }
    public Staff Staff { get; set; }
    public Service Service { get; set; }
}

public class Pet
{
    [Key] public int Id { get; set; }
    [MaxLength(50)] public string Name { get; set; }
    [MaxLength(30)] public string Species { get; set; }
    [MaxLength(50)] public string Breed { get; set; }
    public DateOnly? DOB { get; set; }
    [MaxLength(100)] public string PhotoURL { get; set; }
    [MaxLength(100)] public string CustomerEmail { get; set; }
    public Customer Customer { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = [];
}

public class Appointment
{
    [Key] public int Id { get; set; }
    public int PetId { get; set; }
    [MaxLength(100)] public string StaffEmail { get; set; }
    [MaxLength(6)] public string BranchId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly TimeSlot { get; set; }
    [RegularExpression(@"Pending|Confirmed|Completed|Cancelled", ErrorMessage = "Invalid {0}.")]
    public string Status { get; set; }
    [MaxLength(300)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Pet Pet { get; set; }
    public Staff Staff { get; set; }
    public Branch Branch { get; set; }
    public ICollection<AppointmentService> AppointmentServices { get; set; } = [];
    public Review? Review { get; set; }
}

public class AppointmentService
{
    [Key, Column(Order = 0)] public int AppointmentId { get; set; }
    [Key, Column(Order = 1), MaxLength(6)] public string ServiceId { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal Price { get; set; }
    public Appointment Appointment { get; set; }
    public Service Service { get; set; }
}

public class Product
{
    [Key, MaxLength(6), RegularExpression(@"PR\d{3}", ErrorMessage = "Invalid {0}.")]
    public string Id { get; set; }
    [MaxLength(100)] public string Name { get; set; }
    [MaxLength(500)] public string Description { get; set; }
    [Column(TypeName = "decimal(8,2)"), Range(0.01, 999.99)]
    public decimal Price { get; set; }
    [Range(0, 9999)] public int Stock { get; set; }
    [MaxLength(100)] public string PhotoURL { get; set; }
    public ICollection<OrderLine> OrderLines { get; set; } = [];
}

public class Order
{
    [Key] public int Id { get; set; }
    [MaxLength(100)] public string CustomerEmail { get; set; }
    public DateOnly Date { get; set; }
    [RegularExpression(@"Pending|Paid|Completed|Cancelled", ErrorMessage = "Invalid {0}.")]
    public string Status { get; set; }
    public bool Paid { get; set; }
    [MaxLength(200)] public string? DeliveryAddress { get; set; }
    public Customer Customer { get; set; }
    public ICollection<OrderLine> OrderLines { get; set; } = [];
}

public class OrderLine
{
    [Key] public int Id { get; set; }
    public int OrderId { get; set; }
    [MaxLength(6)] public string ProductId { get; set; }
    [Range(1, 20)] public int Quantity { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal Price { get; set; }
    public Order Order { get; set; }
    public Product Product { get; set; }
}

public class Review
{
    [Key] public int Id { get; set; }
    public int AppointmentId { get; set; }
    [MaxLength(100)] public string CustomerEmail { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [MaxLength(500)] public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public Appointment Appointment { get; set; }
    public Customer Customer { get; set; }
}

public class Notification
{
    [Key] public int Id { get; set; }
    [MaxLength(100)] public string CustomerEmail { get; set; }
    [RegularExpression(@"Email|SMS", ErrorMessage = "Invalid {0}.")]
    public string Channel { get; set; }
    [MaxLength(500)] public string Message { get; set; }
    public DateTime SentAt { get; set; }
}

public class RewardTransaction
{
    [Key] public int Id { get; set; }
    [MaxLength(100)] public string CustomerEmail { get; set; }
    public int Points { get; set; }
    [MaxLength(200)] public string Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
