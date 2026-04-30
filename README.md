# AutoShine — Auto Repair & Car Wash Management System (ARWMS)

A full-stack web application for managing daily operations of an auto repair and car wash shop.

## Architecture

```
AutoShine.sln
├── src/AutoShine.Models       → Domain entities & enums (Class Library)
├── src/AutoShine.Data         → EF Core DbContext, Configurations, Migrations, Seeder
├── src/AutoShine.Repository   → Repository interfaces & implementations, Unit of Work
├── src/AutoShine.Service      → DTOs, AutoMapper profiles, service interfaces & implementations
├── src/AutoShine              → ASP.NET Core Web API (controllers, middleware, Program.cs)
└── src/client                 → React + Vite frontend
```

### Dependency Flow (Backend)
```
AutoShine (API) → AutoShine.Service → AutoShine.Repository → AutoShine.Data → AutoShine.Models
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 + SQL Server |
| Auth | JWT Bearer Tokens (RBAC: Admin / Employee / Customer) |
| Passwords | BCrypt.Net |
| Mapping | AutoMapper |
| API Docs | Swagger / OpenAPI |
| Frontend | React 18 + Vite |
| Routing | React Router v6 |
| HTTP Client | Axios (with JWT interceptor) |
| Notifications | react-hot-toast |
| Icons | lucide-react |

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (or SQL Server Express / LocalDB)
- Node.js 18+

---

### 1. Configure the database

Edit `src/AutoShine/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=AutoShineDB;Trusted_Connection=True;TrustServerCertificate=True"
}
```

---

### 2. Start the .NET API

Migrations and seed data apply automatically on first run.

```bash
cd src/AutoShine
dotnet run
```

API runs at: **http://localhost:5000**  
Swagger UI: **http://localhost:5000/swagger**

---

### 3. Start the React frontend

```bash
cd src/client
npm install
npm run dev
```

App runs at: **http://localhost:5173**  
(API calls are proxied to `http://localhost:5000` automatically)

---

## Seed Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@autoshine.com | Admin@123 |
| Employee | john.mechanic@autoshine.com | Employee@123 |
| Employee | sarah.washer@autoshine.com | Employee@123 |
| Customer | alice@example.com | Customer@123 |

> Quick login buttons are shown on the login page for convenience.

---

## Frontend Pages

| Page | Path | Access |
|------|------|--------|
| Login | `/login` | Public |
| Register | `/register` | Public |
| Dashboard | `/dashboard` | All roles (role-specific view) |
| My Bookings | `/bookings` | All roles |
| Book a Service | `/book` | Customer |
| Browse Services | `/services` | All roles |
| Manage Packages | `/packages` | Admin |
| Manage Users | `/users` | Admin |
| Inventory | `/inventory` | Admin |
| Review Moderation | `/reviews` | Admin |

---

## API Endpoints

### Auth
| Method | Endpoint | Access |
|--------|----------|--------|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |

### Users
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/users?page&pageSize&role&search` | Admin |
| GET | `/api/users/{id}` | Admin |
| POST | `/api/users` | Admin |
| PUT | `/api/users/{id}` | Admin |
| DELETE | `/api/users/{id}` | Admin |

### Packages
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/packages` | Public |
| GET | `/api/packages/{id}` | Public |
| POST | `/api/packages` | Admin |
| PUT | `/api/packages/{id}` | Admin |
| DELETE | `/api/packages/{id}` | Admin |

### Bookings
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/bookings?page&pageSize&status` | Admin |
| GET | `/api/bookings/my` | Customer / Employee |
| GET | `/api/bookings/{id}` | Authenticated |
| GET | `/api/bookings/available-slots?packageId&date` | Authenticated |
| POST | `/api/bookings` | Customer |
| PATCH | `/api/bookings/{id}/status` | Employee / Admin |
| DELETE | `/api/bookings/{id}` | Authenticated |

### Inventory
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/inventory?page&pageSize&lowStockOnly` | Admin |
| GET | `/api/inventory/alerts` | Admin |
| GET | `/api/inventory/{id}` | Admin |
| POST | `/api/inventory` | Admin |
| PUT | `/api/inventory/{id}` | Admin |
| DELETE | `/api/inventory/{id}` | Admin |

### Reviews
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/reviews/employee/{employeeId}` | Public |
| GET | `/api/reviews/booking/{bookingId}` | Authenticated |
| POST | `/api/reviews` | Customer |
| DELETE | `/api/reviews/{id}` | Admin |

---

## Key Business Logic

| Feature | Implementation |
|---------|----------------|
| JWT Auth (3 roles) | `AuthService` — BCrypt hash, HS256 JWT with role claims |
| Slot engine | `BookingService.GetAvailableSlotsAsync` — 30-min intervals, 08:00–18:00 |
| Least-busy assignment | `BookingService.CreateBookingAsync` — round-robin by daily booking count |
| Atomic inventory deduction | `BookingService.UpdateBookingStatusAsync` — EF transaction, rollback on insufficient stock |
| Double-booking prevention | `BookingRepository.IsEmployeeAvailableAsync` — overlap interval query |
| Post-service review | `ReviewService.CreateReviewAsync` — only on Completed bookings, one per booking |
| Low-stock alerts | `InventoryRepository.GetLowStockItemsAsync` — stock ≤ threshold |
| Pagination | `GenericRepository.GetPagedAsync` — all list endpoints support `page` + `pageSize` |
| Soft delete | Users and Packages use `IsActive = false` to preserve historical booking data |