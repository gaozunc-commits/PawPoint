using Microsoft.EntityFrameworkCore;

namespace Demo.Models;

public static class SeedData
{
    public static void Initialize(DB db, Helper hp)
    {
        if (db.Users.Any()) return;

        // 1. Branches
        db.Branches.AddRange(
            new Branch { Id = "BR001", Name = "City Centre Branch", Address = "10 Jalan Bukit Bintang, 55100 Kuala Lumpur", Phone = "03-21418888", Latitude = 3.1478, Longitude = 101.6953 },
            new Branch { Id = "BR002", Name = "Garden Mall Branch", Address = "Lot 202, The Gardens Mall, Mid Valley City, 59200 Kuala Lumpur", Phone = "03-22829999", Latitude = 3.1189, Longitude = 101.6761 },
            new Branch { Id = "BR003", Name = "Lake Park Branch", Address = "8 Persiaran Tasik, SS15, 47500 Subang Jaya", Phone = "03-56337777", Latitude = 3.0738, Longitude = 101.5183 }
        );

        // 2. Admin
        db.Admins.Add(new Admin
        {
            Email = "admin@pawpoint.test",
            Hash = hp.HashPassword("password"),
            Name = "PawPoint Admin",
            Phone = "0101234567"
        });

        // 3. Customers
        string[] customerNames = ["Casey Tan", "Alya Rahman", "Ben Lee", "Chloe Wong", "Daniel Lim", "Esha Kumar", "Farah Noor", "Gavin Ong"];
        for (int i = 0; i < customerNames.Length; i++)
        {
            db.Customers.Add(new Customer
            {
                Email = $"customer{i + 1}@pawpoint.test",
                Hash = hp.HashPassword("password"),
                Name = customerNames[i],
                Phone = $"01234567{i:00}",
                PhotoURL = "photo.jpg",
                Points = 50 + i * 20,
            });
        }
        db.Customers.Add(new Customer
        {
            Email = "customer@pawpoint.test",
            Hash = hp.HashPassword("password"),
            Name = "Demo Customer",
            Phone = "0123456789",
            PhotoURL = "photo.jpg",
            Points = 150
        });

        // 4. Staff
        string[] branches = ["BR001", "BR002", "BR003"];
        string[] staffNames = ["Sam Staff", "Mia Groomer", "Nora Stylist", "Leo Care", "Ivy Groomer", "Kai Handler", "June Vet Tech", "Omar Stylist", "Rina Care", "Tara Groomer", "Vik Handler", "Wen Stylist"];
        for (int i = 0; i < staffNames.Length; i++)
        {
            db.Staffs.Add(new Staff
            {
                Email = i == 0 ? "staff@pawpoint.test" : $"staff{i + 1}@pawpoint.test",
                Hash = hp.HashPassword("password"),
                Name = staffNames[i],
                Phone = $"01111111{i:00}",
                BranchId = branches[i % branches.Length],
                PhotoURL = "photo.jpg",
            });
        }

        // 5. Services
        db.Services.AddRange(
            new Service { Id = "SV001", Name = "Basic Grooming", Description = "Shampoo bath, blow dry, nail clip, ear cleaning, and sanitary trim.", Price = 55.00m, DurationMinutes = 60, BranchId = "BR001", PhotoURL = "photo.jpg" },
            new Service { Id = "SV002", Name = "Full Styling & Scissoring", Description = "Custom breed haircut, full scissor styling, paw massage, and cologne finish.", Price = 120.00m, DurationMinutes = 120, BranchId = "BR001", PhotoURL = "photo.jpg" },
            new Service { Id = "SV003", Name = "Nail & Paw Care", Description = "Precision nail clipping, filing, paw hair trim, and soothing paw balm.", Price = 25.00m, DurationMinutes = 20, BranchId = "BR002", PhotoURL = "photo.jpg" },
            new Service { Id = "SV004", Name = "De-Shedding Treatment", Description = "Undercoat blowout, deep brush de-shedding, and shedding control shampoo.", Price = 85.00m, DurationMinutes = 75, BranchId = "BR002", PhotoURL = "photo.jpg" },
            new Service { Id = "SV005", Name = "Aromatherapy Spa Bath", Description = "Gentle herbal massage bath with soothing lavender oil for sensitive skin.", Price = 70.00m, DurationMinutes = 60, BranchId = "BR003", PhotoURL = "photo.jpg" },
            new Service { Id = "SV006", Name = "Ear & Eye Hygiene Care", Description = "Deep ear canal wash, tear stain removal, and facial fluff wipe.", Price = 20.00m, DurationMinutes = 15, BranchId = "BR001", PhotoURL = "photo.jpg" },
            new Service { Id = "SV007", Name = "Ultrasonic Teeth Cleansing", Description = "Gentle surface ultrasonic tartar removal and mint breath freshening.", Price = 45.00m, DurationMinutes = 30, BranchId = "BR002", PhotoURL = "photo.jpg" },
            new Service { Id = "SV008", Name = "Puppy Gentle Intro Session", Description = "Low-stress first groom experience with treats, bath, and gentle brush.", Price = 65.00m, DurationMinutes = 60, BranchId = "BR003", PhotoURL = "photo.jpg" },
            new Service { Id = "SV009", Name = "Feline Deluxe Bath & Brush", Description = "Cat-specialist waterless or water bath, lion cut or brush out.", Price = 95.00m, DurationMinutes = 90, BranchId = "BR003", PhotoURL = "photo.jpg" },
            new Service { Id = "SV010", Name = "Medicated Flea & Tick Bath", Description = "Veterinary-approved anti-parasitic treatment and protective coat rinse.", Price = 75.00m, DurationMinutes = 60, BranchId = "BR001", PhotoURL = "photo.jpg" }
        );

        // 6. Products
        db.Products.AddRange(
            new Product { Id = "PR001", Name = "Hypoallergenic Oatmeal Shampoo", Description = "Soothing natural oatmeal shampoo for sensitive or itchy skin.", Price = 32.00m, Stock = 45, PhotoURL = "photo.jpg" },
            new Product { Id = "PR002", Name = "Self-Cleaning Slicker Brush", Description = "Ergonomic fine-wire brush with push-button fur ejection.", Price = 38.00m, Stock = 28, PhotoURL = "photo.jpg" },
            new Product { Id = "PR003", Name = "Enzymatic Dental Chews (30pk)", Description = "Veterinarian recommended tartar reducing dental chews.", Price = 24.50m, Stock = 60, PhotoURL = "photo.jpg" },
            new Product { Id = "PR004", Name = "Organic Beeswax Paw Balm", Description = "All-natural paw pad moisturiser and heat protectant.", Price = 22.00m, Stock = 35, PhotoURL = "photo.jpg" },
            new Product { Id = "PR005", Name = "Aloe Vera Ear Cleaning Wipes (50ct)", Description = "Gentle pre-soaked wipes for daily ear hygiene.", Price = 18.00m, Stock = 50, PhotoURL = "photo.jpg" },
            new Product { Id = "PR006", Name = "Silk Protein Detangling Spray", Description = "Leave-in detangler spray that leaves coats glossy and silky.", Price = 29.00m, Stock = 40, PhotoURL = "photo.jpg" },
            new Product { Id = "PR007", Name = "Quick-Stop Safety Nail Clipper", Description = "Stainless steel clippers with safety guard and LED light.", Price = 36.00m, Stock = 25, PhotoURL = "photo.jpg" },
            new Product { Id = "PR008", Name = "Hands-Free Training Treat Pouch", Description = "Magnetic closure waist pouch with poop bag dispenser.", Price = 28.00m, Stock = 30, PhotoURL = "photo.jpg" },
            new Product { Id = "PR009", Name = "Dual-Sided Undercoat De-Matting Rake", Description = "Rounded teeth rake for safe removal of stubborn mats.", Price = 34.00m, Stock = 22, PhotoURL = "photo.jpg" },
            new Product { Id = "PR010", Name = "Microfibre Quick-Dry Bath Robe", Description = "Super absorbent hooded towel robe for dogs and cats.", Price = 42.00m, Stock = 32, PhotoURL = "photo.jpg" }
        );

        db.SaveChanges();

        // 7. StaffService M:M Assignments
        var serviceList = db.Services.ToList();
        foreach (var s in db.Staffs.ToList())
        {
            var assigned = serviceList.Where(sv => sv.BranchId == s.BranchId).ToList();
            if (assigned.Count == 0) assigned = serviceList.Take(3).ToList();
            foreach (var sv in assigned)
            {
                db.StaffServices.Add(new StaffService { StaffEmail = s.Email, ServiceId = sv.Id });
            }
        }

        // 8. Pets
        var customers = db.Customers.ToList();
        string[] petNames = ["Milo", "Coco", "Bella", "Luna", "Charlie", "Max", "Bailey", "Teddy", "Rocky", "Mochi"];
        string[] breeds = ["Golden Retriever", "Poodle", "British Shorthair", "Persian Cat", "Corgi", "Pug", "Shiba Inu", "Ragdoll Cat", "Beagle", "Maltese"];
        for (int i = 0; i < petNames.Length; i++)
        {
            var cust = customers[i % customers.Count];
            db.Pets.Add(new Pet
            {
                Name = petNames[i],
                Species = i % 3 == 2 ? "Cat" : "Dog",
                Breed = breeds[i],
                DOB = DateOnly.FromDateTime(DateTime.Today.AddYears(-(1 + i % 5)).AddMonths(-i)),
                PhotoURL = "photo.jpg",
                CustomerEmail = cust.Email
            });
        }
        db.SaveChanges();

        // 9. Appointments (20 records across statuses)
        var pets = db.Pets.ToList();
        var allStaff = db.Staffs.ToList();
        string[] statuses = ["Pending", "Confirmed", "Completed", "Cancelled"];
        TimeOnly[] slots = [new(9, 0), new(10, 0), new(11, 0), new(14, 0), new(15, 0), new(16, 0)];

        for (int i = 0; i < 20; i++)
        {
            var staff = allStaff[i % allStaff.Count];
            var pet = pets[i % pets.Count];
            var appt = new Appointment
            {
                PetId = pet.Id,
                StaffEmail = staff.Email,
                BranchId = staff.BranchId,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(i - 10)),
                TimeSlot = slots[i % slots.Length],
                Status = statuses[i % statuses.Length],
                Notes = i % 2 == 0 ? "Extra careful around ears." : "Please trim nails extra short.",
                CreatedAt = DateTime.Now.AddDays(-15 + i),
            };
            db.Appointments.Add(appt);

            var sv = serviceList[i % serviceList.Count];
            db.AppointmentServices.Add(new AppointmentService
            {
                Appointment = appt,
                ServiceId = sv.Id,
                Price = sv.Price
            });
        }
        db.SaveChanges();

        // 10. Reviews & Loyalty Transactions
        var completedAppts = db.Appointments.Include(a => a.Pet).Where(a => a.Status == "Completed").ToList();
        string[] reviewComments = [
            "Fantastic grooming! Milo looks like a champion.",
            "Very gentle with my nervous cat. Highly recommend!",
            "Fast, clean, and friendly service at City Centre.",
            "Loved the lavender spa bath. Smells incredible!",
            "Great haircut and teeth cleaning. 5 stars."
        ];

        for (int i = 0; i < completedAppts.Count && i < reviewComments.Length; i++)
        {
            var appt = completedAppts[i];
            db.Reviews.Add(new Review
            {
                AppointmentId = appt.Id,
                CustomerEmail = appt.Pet.CustomerEmail,
                Rating = 4 + (i % 2),
                Comment = reviewComments[i],
                CreatedAt = DateTime.Now.AddDays(-i - 1)
            });

            db.RewardTransactions.Add(new RewardTransaction
            {
                CustomerEmail = appt.Pet.CustomerEmail,
                Points = 10,
                Reason = $"Review reward for Appointment #{appt.Id}",
                CreatedAt = DateTime.Now.AddDays(-i - 1)
            });
        }

        // 11. Orders & OrderLines
        var products = db.Products.ToList();
        for (int i = 0; i < 6; i++)
        {
            var cust = customers[i % customers.Count];
            var order = new Order
            {
                CustomerEmail = cust.Email,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-i)),
                Status = i == 0 ? "Pending" : "Paid",
                Paid = i != 0,
                DeliveryAddress = $"{10 + i} Jalan Melati, 50000 Kuala Lumpur",
            };
            db.Orders.Add(order);

            var p1 = products[i % products.Count];
            var p2 = products[(i + 3) % products.Count];
            db.OrderLines.Add(new OrderLine { Order = order, ProductId = p1.Id, Quantity = 1 + (i % 2), Price = p1.Price });
            db.OrderLines.Add(new OrderLine { Order = order, ProductId = p2.Id, Quantity = 1, Price = p2.Price });
        }

        db.SaveChanges();
    }
}
