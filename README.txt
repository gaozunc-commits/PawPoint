========================================================================
PAWPOINT — Multi-Branch Pet Grooming & Supplies Management System
Course: AMIT2014 Web and Mobile Systems
Framework: .NET 10 / ASP.NET Core MVC (C# Razor)
Database: SQL Server Express / LocalDB (PawPointDB.mdf)
Authentication: Custom Cookie Authentication (TPH User Hierarchy)
========================================================================

1. TEST LOGIN CREDENTIALS
------------------------------------------------------------------------
Role         Email                     Password     Access Privileges
------------------------------------------------------------------------
Admin        admin@pawpoint.test       password     Full System Administration, Branches, Staff (M:M), Services, Products, All Appointments, Analytics Reports, Live Support Chat
Staff        staff@pawpoint.test       password     Branch Appointments Schedule, Status Updates, Service Qualifications, Analytics Reports, Live Support Chat
Customer     customer@pawpoint.test    password     Pet Profile Management, Multi-Service Booking, Availability Matrix, Session Shopping Cart, Checkout with Loyalty Points, Review System, Live Support Chat

* Note: Additional test customer accounts (customer1@pawpoint.test to customer8@pawpoint.test, password: password) are pre-seeded.

2. RUNNING THE APPLICATION
------------------------------------------------------------------------
Method A (Visual Studio):
1. Open PawPoint.slnx in Visual Studio 2026.
2. Press Ctrl + F5 (or F5) to run. The database (PawPointDB.mdf) and sample seed data will initialize automatically on startup.

Method B (.NET CLI):
1. Open Terminal in the PawPoint project directory.
2. Run: dotnet run
3. Open browser and navigate to: https://localhost:7000 or http://localhost:5000

3. KEY IMPLEMENTED FEATURES & ADDITIONAL FEATURES (AF)
------------------------------------------------------------------------
• Practical 03 (AJAX): AJAX Search, AJAX Sort, and AJAX Paging on Service Catalog, Supplies Catalog, Branches Index, and Staff Index.
• Practical 05 (M:M): Many-to-many relationship for Staff-Service qualifications and Multi-Service appointment booking via _CheckBoxList.
• Practical 06 (Photo & Webcam): Photo upload with ImageSharp auto-crop/resize and live HTML5 Webcam capture for customer and pet profiles.
• Practical 07 (Security): Table-per-Hierarchy (TPH) user claims, Captcha verification challenge, and account lockout protection after 3 failed login attempts.
• Practical 08 (Email & DateTime): Live SMTP email dispatching for registration OTP verification, password reset tokens, appointment confirmation, appointment reminder, order e-receipts, and SMS notifications.
• Practical 09 (Cart & Checkout): Session-backed shopping cart, stock deduction, loyalty points discount redemption, and online deposit payment.
• Practical 10 (Matrix Booking): Multi-branch Staff vs Time-Slot availability matrix grid (O/X).
• Advanced Features:
  - Real-Time Support Chat Room powered by SignalR (/chatHub).
  - Dynamic QR Code Generation and Contactless Check-In (/Appointment/QrCheckIn).
  - Interactive On-Screen Visual KPI Analytics Reports (Revenue, Bookings, Loyalty Points).
  - Customer Loyalty Points Accrual & Redemption Ledger.
  - GPS Coordinates for Branch Locations.
========================================================================
