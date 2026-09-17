# 🏨 EMA Hotel — Hotel Management & Booking System

EMA Hotel is a full-stack hotel management and booking system built with **ASP.NET Core MVC** and **SQL Server**.

The project is designed to simulate a real-world hotel platform where guests can search and book rooms, while receptionists and administrators can manage hotel operations, bookings, rooms, pricing, and reviews.

---

## 📌 Project Overview

EMA Hotel provides different experiences based on the user's role:

* **Guest** — Browse rooms, search availability, make bookings, manage bookings, and submit reviews.
* **Receptionist** — Manage arrivals, departures, check-in/check-out, walk-in guests, and current guests.
* **Admin** — Manage rooms, room types, pricing, bookings, reviews, and hotel statistics.

The system follows a relational database design and uses **ASP.NET Core Identity** for authentication and role-based authorization.

---

## ✨ Main Features

### 👤 Authentication & Authorization

* User registration
* User login/logout
* Profile management
* ASP.NET Core Identity
* Role-based authorization
* Supported roles:

  * Guest
  * Receptionist
  * Admin

---

### 🔎 Room Search & Availability

Guests can search for available rooms based on:

* Check-in date
* Check-out date
* Room type
* Capacity
* Price
* View

The system prevents overlapping bookings for the same room.

---

### 🛏️ Room Management

Administrators can manage:

* Rooms
* Room types
* Room images
* Room status
* Room pricing

---

### 📅 Booking Management

The booking system supports:

* Creating reservations
* Multiple rooms per booking
* Booking reference generation
* Check-in/check-out dates
* Total stay price
* Booking status
* Payment status
* Payment method
* Cancellation handling
* Additional extras

---

### 🧾 Hotel Extras

Bookings can include additional services such as:

* Breakfast
* Airport pickup
* Other hotel services

Extras can be priced either per booking or according to the configured pricing model.

---

### 👨‍💼 Reception Dashboard

Receptionists can manage daily hotel operations:

* Arrivals
* Departures
* Check-in
* Check-out
* Walk-in guests
* Current guests
* Booking search

---

### 📊 Admin Dashboard

The administration area provides hotel management functionality including:

* Hotel statistics
* Booking management
* Room management
* Room type management
* Pricing management
* Reviews management
* Occupancy information

---

### ⭐ Reviews

Guests can submit reviews related to completed bookings.

The system ensures that a booking can have at most one review.

---

## 🏗️ Architecture

The project follows the **ASP.NET Core MVC** architecture:

```text
Hotel_MVC
│
├── Controllers
│
├── Models
│
├── ViewModels
│
├── Views
│
├── Data
│   ├── AppDbContext
│   ├── Configurations
│   └── Seed
│
├── Services
│
├── Migrations
│
├── Docs
│   └── ERD
│
└── wwwroot
```

---

## 🛠️ Technology Stack

### Backend

* ASP.NET Core MVC
* .NET 10
* C#
* Entity Framework Core
* ASP.NET Core Identity

### Database

* Microsoft SQL Server
* Entity Framework Core Migrations

### Frontend

* HTML5
* CSS3
* JavaScript
* Razor Views
* Tailwind CSS / project UI components

### Development Tools

* Visual Studio
* Git
* GitHub
* SQL Server
* GitHub Pull Requests

---

## 🗄️ Database Design

The current core domain model includes:

* `ApplicationUser`
* `Booking`
* `BookingExtra`
* `Extra`
* `Review`
* `Room`
* `RoomImage`
* `RoomType`

ASP.NET Core Identity also provides its required identity tables.

### Main Relationships

```text
ApplicationUser
      │
      ├──────────< Booking
      │               │
      │               ├──────< BookingExtra >────── Extra
      │               │
      │               ├──────< Review
      │               │
      │               └────── Rooms
      │
      └────────── Roles

RoomType
   │
   ├──────────< Room
   │              │
   │              └──────────< RoomImage
   │
   └────────── Room Images
```

### Important Database Rules

* A user can have multiple roles.
* A booking can contain multiple rooms.
* A booking can contain multiple extras.
* A room belongs to a room type.
* A room can have multiple images.
* A booking can have zero or one review.
* Room availability must prevent overlapping bookings.
* Booking totals are calculated based on the selected rooms, stay duration, and extras.

---

## 📐 ERD & Database Documentation

The database documentation is available inside:

```text
Docs/
└── ERD/
    └── ERD.excalidraw
```

The ERD is maintained in an editable **Excalidraw** format.

---

## 🔐 Security

The application uses:

* ASP.NET Core Identity
* Password hashing
* Authentication
* Role-based authorization
* Protected admin/receptionist functionality
* Server-side validation
* Entity Framework Core parameterized database access

---

## 🚀 Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Mohamedsayed101/HotelMvc.git
```

### 2. Navigate to the project

```bash
cd HotelMvc
```

### 3. Configure the database

Update the connection string in:

```text
appsettings.json
```

Example:

```json
"ConnectionStrings": {
  "MyConnection": "Server=.;Database=HotelMVC;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Use your local SQL Server configuration if the server name is different.

---

### 4. Apply Entity Framework migrations

Open the Package Manager Console:

```powershell
Update-Database
```

Or using the .NET CLI:

```bash
dotnet ef database update
```

---

### 5. Run the application

```bash
dotnet run
```

The application will be available on the local URL configured in `launchSettings.json`.

---

## 🌱 Database Seeding

The application includes initial data seeding for:

* Roles
* Initial application configuration
* Required startup data

The main roles are:

```text
Admin
Receptionist
User
```

---

## 🔄 Development Workflow

The project uses Git and GitHub for collaborative development.

### Branching

Feature development should be done on dedicated branches:

```text
feature/<feature-name>
```

Bug fixes:

```text
fix/<bug-name>
```

Project/setup changes:

```text
setup/<change-name>
```

### Pull Requests

Changes should be submitted through Pull Requests before being merged into the protected main branch.

---

## 📂 Documentation

Project documentation is organized under:

```text
Docs/
```

Current documentation includes:

```text
Docs/
└── ERD/
    └── ERD.excalidraw
```

Additional documentation will be added as the project develops.

---

## 👥 Team

### EMA Hotel Team

* **Mohamed Sayed** — Team Leader
* **Adam Mohamed** — Team Member
* **Esraa Hamdy** — Team Member

The team collaborates using GitHub branches, commits, and Pull Requests.

---

## 🎯 Project Goals

The project aims to provide a realistic hotel management platform while applying practical software engineering concepts:

* Requirements analysis
* Database design
* ERD modeling
* Relational database mapping
* ASP.NET Core MVC
* Entity Framework Core
* Authentication & Authorization
* CRUD operations
* Role-based access control
* Booking and availability logic
* Git/GitHub collaboration
* Clean and maintainable project structure

---

## 🔮 Future Enhancements

Potential future improvements include:

* Payment Gateway integration
* Email service integration
* AI hotel assistant
* Occupancy calendar
* Discount codes
* Housekeeping management
* Voucher generation
* Arabic / English localization
* Advanced hotel analytics
* AI-powered review summaries

---

## 📄 License

This project is developed for educational and portfolio purposes.

---

## 👨‍💻 Author

**EMA Team**

Computer Science Student
Fayoum University

GitHub: `Mohamedsayed101`
