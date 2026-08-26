# PAWPOINT: Multi-Branch Pet Grooming & Pet Care Appointment Management System
**Course:** AMIT2014 Web and Mobile Systems  
**Stack:** .NET 10 &middot; ASP.NET Core MVC &middot; Entity Framework Core &middot; SQL Server LocalDB &middot; Cookie Auth &middot; jQuery Unobtrusive AJAX &middot; SignalR

---

## 4.1 System Modules Outline

The system is decomposed into four core pillars, with balanced module distribution and 3–4 Additional Features `[AF]` per Person-In-Charge (PIC).

| PIC (Member) | Core Modules & Functions | Additional Features `[AF]` |
| :--- | :--- | :--- |
| **Member 1 (Security & Master Maintenance)** | • **User Authentication & Profiles**: Login, Logout, Role-based Navigation, Password Update.<br>• **Branch Maintenance**: Full CRUD for branch salons with GPS coordinates.<br>• **Staff & Qualification Maintenance**: Full CRUD for groomers and M:M service qualifications (`StaffService`). | `[AF]` Captcha Security Math Challenge on Login/Register.<br>`[AF]` Account Lockout after 3 failed login attempts.<br>`[AF]` Email OTP 6-Digit Registration Verification.<br>`[AF]` AJAX Searching, Sorting & Paging on Staff/Branch Index. |
| **Member 2 (Pet Profile & Grooming Appointments)** | • **Pet Profile Management**: Full CRUD for Customer pet records with breed and DOB.<br>• **Multi-Service Appointment Booking**: Multi-select grooming packages (`AppointmentService`) with price snapshots.<br>• **Staff Booking Schedule**: Staff/Admin branch appointment management and status lifecycle. | `[AF]` Live HTML5 Webcam Snapshot Capture for Pet/Profile Photos.<br>`[AF]` Staff vs Time-Slot Availability Matrix Grid (O/X).<br>`[AF]` Automated Appointment Confirmation & Reminder Emails via SMTP.<br>`[AF]` Dynamic QR Code Check-In for Reception. |
| **Member 3 (Supplies Catalog & E-Commerce Cart)** | • **Grooming Supplies Catalog**: Searchable product inventory with stock tracking.<br>• **Product Administration**: Admin CRUD for retail products with image cropping.<br>• **Session Shopping Cart**: Multi-item cart with quantity adjustment and stock checks.<br>• **Checkout & Order History**: Order placement, stock deduction, and order detail review. | `[AF]` AJAX Real-Time Search-As-You-Type on Products Catalog.<br>`[AF]` AJAX Table Sorting & Pagination.<br>`[AF]` Order E-Receipt Generation & Automatic Email Dispatch.<br>`[AF]` Online Deposit / Card Payment Simulation Interface. |
| **Member 4 (Reviews, Loyalty, Live Chat & Analytics)** | • **Customer Review & Ratings**: Post-appointment 1–5 star ratings and reviews.<br>• **Loyalty Points System**: Customer points ledger with earn/redeem mechanics.<br>• **Business Intelligence & Analytics**: Interactive revenue, booking density, and customer loyalty reports.<br>• **Customer Support**: Live communication between customers and salon staff. | `[AF]` Loyalty Reward Points Accrual (+10 pts per review, points earned per RM spent).<br>`[AF]` Points Redemption at Checkout (10 pts = RM 1.00 discount).<br>`[AF]` Real-Time Bi-Directional Support Chat Room via SignalR.<br>`[AF]` On-Screen Dynamic Visual KPI Charts (Revenue, Bookings, Loyalty). |

---

## 4.2 Entity Class Diagram & Data Layer Specification

The PawPoint relational schema is implemented in Entity Framework Core using Table-Per-Hierarchy (TPH) inheritance for user management and explicit Many-to-Many junction entities for accurate price and qualification history.

### Relational Entity Summary
1. **`User` (TPH Base Entity)**: `Email` (PK, varchar(100)), `Hash` (varchar(100)), `Name` (varchar(100)), `Phone` (varchar(20)), `Role` (Discriminator).
   - **`Admin`**: Inherits `User`.
   - **`Staff`**: Inherits `User`, `BranchId` (FK &rarr; `Branch`), `PhotoURL` (varchar(100)).
   - **`Customer`**: Inherits `User`, `Points` (int, default 0), `PhotoURL` (varchar(100)).
2. **`Branch`**: `Id` (PK, varchar(6), regex `BR\d{3}`), `Name` (varchar(100)), `Address` (varchar(200)), `Phone` (varchar(20)), `Latitude` (float), `Longitude` (float).
3. **`Service`**: `Id` (PK, varchar(6), regex `SV\d{3}`), `Name` (varchar(100)), `Description` (varchar(500)), `Price` (decimal(8,2)), `DurationMinutes` (int), `PhotoURL` (varchar(100)), `BranchId` (FK &rarr; `Branch`).
4. **`StaffService` (M:M Junction)**: `StaffEmail` (PK, FK &rarr; `Staff`), `ServiceId` (PK, FK &rarr; `Service`).
5. **`Pet`**: `Id` (PK, Identity int), `Name` (varchar(50)), `Species` (varchar(30)), `Breed` (varchar(50)), `DOB` (date), `PhotoURL` (varchar(100)), `CustomerEmail` (FK &rarr; `Customer`).
6. **`Appointment`**: `Id` (PK, Identity int), `PetId` (FK &rarr; `Pet`), `StaffEmail` (FK &rarr; `Staff`), `BranchId` (FK &rarr; `Branch`), `Date` (date), `TimeSlot` (time), `Status` (varchar(20): Pending, Confirmed, Completed, Cancelled), `Notes` (varchar(300)), `CreatedAt` (datetime).
7. **`AppointmentService` (M:M Junction with Price Snapshot)**: `AppointmentId` (PK, FK &rarr; `Appointment`), `ServiceId` (PK, FK &rarr; `Service`), `Price` (decimal(8,2)).
8. **`Product`**: `Id` (PK, varchar(6), regex `PR\d{3}`), `Name` (varchar(100)), `Description` (varchar(500)), `Price` (decimal(8,2)), `Stock` (int), `PhotoURL` (varchar(100)).
9. **`Order`**: `Id` (PK, Identity int), `CustomerEmail` (FK &rarr; `Customer`), `Date` (date), `Status` (varchar(20)), `Paid` (bit), `DeliveryAddress` (varchar(200)).
10. **`OrderLine`**: `Id` (PK, Identity int), `OrderId` (FK &rarr; `Order`), `ProductId` (FK &rarr; `Product`), `Quantity` (int), `Price` (decimal(8,2) snapshot).
11. **`Review`**: `Id` (PK, Identity int), `AppointmentId` (FK &rarr; `Appointment`, 1:1), `CustomerEmail` (FK &rarr; `Customer`), `Rating` (int, 1–5), `Comment` (varchar(500)), `CreatedAt` (datetime).
12. **`Notification`**: `Id` (PK, Identity int), `CustomerEmail` (FK &rarr; `Customer`), `Channel` (varchar(10): Email/SMS), `Message` (varchar(500)), `SentAt` (datetime).
13. **`RewardTransaction`**: `Id` (PK, Identity int), `CustomerEmail` (FK &rarr; `Customer`), `Points` (int, &plusmn; delta), `Reason` (varchar(200)), `CreatedAt` (datetime).

---

## 4.3 Monetization Models & Numeric Projections

PawPoint employs a diversified, highly scalable monetization architecture across direct service sales, retail margins, subscription memberships, and brand placements.

### Model 1: Grooming Service Direct Margin & Platform Fee
* **Mechanism**: Revenue earned directly from appointment bookings across basic grooming, full styling, spa baths, and feline care. PawPoint retains a 40% gross margin after groomer compensation and direct salon overhead.
* **Numeric Projection**:
  - 3 Operating Branches &times; 4 Groomers per branch = 12 Active Groomers.
  - Average capacity: 5 grooming sessions per groomer/day &times; 26 operating days/month = 1,560 appointments/month.
  - Average appointment basket value: RM 75.00.
  - **Monthly Gross Bookings**: RM 75.00 &times; 1,560 = **RM 117,000 / month**.
  - **Net Platform Gross Profit (40%)**: RM 117,000 &times; 40% = **RM 46,800 / month** (RM 561,600 / year).

### Model 2: E-Commerce Grooming Supplies Retail Markup
* **Mechanism**: Direct retail sales of premium hypoallergenic shampoos, slicker brushes, dental chews, and paw balms. Products are purchased wholesale and sold at a 55% retail markup.
* **Numeric Projection**:
  - Estimated 600 monthly online retail orders across customer base.
  - Average cart value: RM 65.00.
  - **Monthly Retail Sales**: 600 &times; RM 65.00 = **RM 39,000 / month**.
  - **Gross Profit Margin (35% net margin)**: RM 39,000 &times; 35% = **RM 13,650 / month** (RM 163,800 / year).

### Model 3: "PawPoint VIP Care" Recurring Subscription Membership
* **Mechanism**: Pet owners subscribe to a recurring monthly VIP membership tier (RM 49.00/month) that unlocks 1 free basic wash per month, 15% discount on all retail supplies, priority slot booking, and 2&times; loyalty point accumulation.
* **Numeric Projection**:
  - Target 400 active subscribers across 3 branches.
  - Monthly subscription fee: RM 49.00 / user.
  - **Monthly Recurring Revenue (MRR)**: 400 &times; RM 49.00 = **RM 19,600 / month**.
  - Direct fulfillment cost of free wash: RM 15.00/user.
  - **Net Subscription Profit**: 400 &times; (RM 49.00 &minus; RM 15.00) = **RM 13,600 / month** (RM 163,200 / year).

### Model 4: Brand Sponsorship & Featured Product Placements
* **Mechanism**: Third-party premium veterinary and pet food brands (e.g. Royal Canin, Orijen, Furminator) pay a monthly placement fee to feature sponsored banner products on the Service & Supplies catalogs.
* **Numeric Projection**:
  - 4 Featured Brand Partners &times; RM 1,500.00 / month sponsorship fee.
  - **Monthly Sponsorship Revenue**: **RM 6,000 / month** (RM 72,000 / year at 95% margin).

### Total Projected Annualized Revenue
$$\text{Total Annual Gross Revenue} = (\text{RM } 117,000 + 39,000 + 19,600 + 6,000) \times 12 = \mathbf{\text{RM } 2,179,200 \text{ / year}}$$
$$\text{Total Annual Net Platform Profit} = (\text{RM } 46,800 + 13,650 + 13,600 + 5,700) \times 12 = \mathbf{\text{RM } 957,000 \text{ / year}}$$

---

## 4.4 System Features & Submission Sanity Checklist

* **Zero Warning / Zero Error Build**: Solution compiles cleanly under .NET 10.
* **Role-Based Authorization Verified**: `[Authorize(Roles = "...")]` audited across all 12 controllers.
* **Database & Seed Data Verified**: 1 Admin, 3 Branches, 12 Staff, 10 Services, 10 Products, 8 Customers, 10 Pets, 20 Appointments, Reviews, and Orders initialized automatically.
* **Deliverables Ready**: Solution structure, README.txt with credentials, and clean directories.
