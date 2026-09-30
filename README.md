# AutoShine — Auto Repair & Car Wash Management System (ARWMS)

A full-stack web application for managing daily operations of an auto repair and car wash shop.
See [description.md](description.md) for every workflow per role (Admin, Employee, Customer).

## Architecture

```
AutoShine.sln
├── src/AutoShine.Models       → Domain entities & enums (Class Library)
├── src/AutoShine.Data         → EF Core DbContext, Configurations, Migrations, Seeder
├── src/AutoShine.Repository   → Repository interfaces & implementations, Unit of Work
├── src/AutoShine.Service      → DTOs, Mapster mappings, service interfaces & implementations
├── src/AutoShine              → ASP.NET Core Web API (controllers, middleware, Program.cs)
├── src/AutoShine.UnitTests    → xUnit tests (services & repositories, EF Core InMemory)
└── src/client                 → React + Vite frontend
```

### Dependency Flow (Backend)
```
AutoShine (API) → AutoShine.Service → AutoShine.Repository → AutoShine.Data → AutoShine.Models
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core Web API (.NET 9) |
| ORM | Entity Framework Core 8 + PostgreSQL (Npgsql) |
| Auth | JWT Bearer Tokens (RBAC: Admin / Employee / Customer) |
| Passwords | BCrypt.Net |
| Mapping | Mapster |
| API Docs | Swagger / OpenAPI (Development only) |
| Frontend | React 19 + Vite |
| Routing | React Router 7 |
| HTTP Client | Axios (with JWT interceptor) |
| Notifications | react-hot-toast |
| Icons | lucide-react |

## Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL 14+ (local install, Docker, or a hosted service such as Neon)
- Node.js 20+

---

### 1. Configure (local development)

Local settings, **including secrets**, live in `src/AutoShine/appsettings.json`.
That file is **git-ignored** — create it from the committed template:

```bash
cp src/AutoShine/appsettings.example.json src/AutoShine/appsettings.json
```

Then fill in:

| Setting | Required | Description |
|---------|:--------:|-------------|
| `ConnectionStrings:DefaultConnection` | ✅ | PostgreSQL connection string, e.g. `Host=localhost;Port=5432;Database=autoshine;Username=postgres;Password=...` |
| `Jwt:SecretKey` | ✅ | Random string, **at least 32 characters**. Tokens are signed with it. |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryHours` | | Defaults: `AutoShine.API` / `AutoShine.Client` / `24` |
| `Seed:AdminEmail` / `Seed:AdminPassword` | first run | Creates the first admin account if none exists (password min. 8 characters). |
| `Seed:DemoUsers` / `Seed:DemoPassword` | | Development only: creates two demo employees and a demo customer sharing `DemoPassword`. |
| `Shop:TimeZone` | | Time zone that schedule hours are in, e.g. `Asia/Dhaka`. Empty = the server's local time zone. |

The API refuses to start with a clear error if the connection string or JWT key is missing.

> **Never commit real secrets.** `appsettings.json` is ignored by git; only `appsettings.example.json` (placeholders) is tracked.

---

### 2. Start the .NET API

Migrations and seed data apply automatically on startup.

```bash
cd src/AutoShine
dotnet run
```

API runs at: **http://localhost:5200**
Swagger UI: **http://localhost:5200/swagger**

---

### 3. Start the React frontend

```bash
cd src/client
npm install
npm run dev
```

App runs at: **http://localhost:5173**
(API calls to `/api` are proxied to `http://localhost:5200`.)

---

### 4. Run the tests

```bash
dotnet test
```

---

## Production configuration (environment variables)

In production, don't deploy an `appsettings.json` with secrets — set **environment variables** instead.
ASP.NET Core maps `__` (double underscore) to the `:` separator, and environment variables override any
`appsettings*.json` values.

| Environment variable | Required | Example |
|----------------------|:--------:|---------|
| `ConnectionStrings__DefaultConnection` | ✅ | `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require` |
| `Jwt__SecretKey` | ✅ | 32+ random characters |
| `Seed__AdminEmail` | first deploy | `admin@yourshop.com` |
| `Seed__AdminPassword` | first deploy | strong password (remove it after the admin exists and change it in the app) |
| `Shop__TimeZone` | recommended | `Asia/Dhaka` |
| `ASPNETCORE_ENVIRONMENT` | | `Production` (default) — demo accounts are never created outside Development |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiryHours` | | optional overrides |

Also update the CORS origins in `Program.cs` (`ReactClient` policy) to your production frontend URL.

## Seed Data

- **Inventory items and service packages** are always seeded into an empty database.
- **Admin account**: created from `Seed:AdminEmail` / `Seed:AdminPassword` only when no admin exists yet.
  If they aren't set, a warning is logged and no admin is created.
- **Demo accounts** (Development only, when `Seed:DemoUsers` is `true` and `Seed:DemoPassword` is set):

  | Role | Email |
  |------|-------|
  | Employee | john.mechanic@autoshine.com |
  | Employee | sarah.washer@autoshine.com |
  | Customer | alice@example.com |

  Demo employees get a Mon–Fri 09:00–17:00 schedule (break 13:00–14:00) valid for five years from the
  start of the current year. Passwords are whatever you put in `Seed:DemoPassword` — no passwords are
  stored in this repository.

> Seeding never changes existing accounts. If a database was seeded by an older version of this project
> (which used hard-coded passwords), change those accounts' passwords from the Profile page or deactivate them.

---

## Frontend Pages

| Page | Path | Access |
|------|------|--------|
| Login | `/login` | Public |
| Register | `/register` | Public |
| Dashboard | `/dashboard` | All roles (role-specific view) |
| Bookings | `/bookings` | All roles (Admin: all bookings; Employee: assigned; Customer: own) |
| Profile | `/profile` | All roles |
| Browse Services | `/services` | Customer |
| Book a Service | `/book` | Customer |
| Manage Packages | `/packages` | Admin |
| Manage Users | `/users` | Admin |
| Inventory | `/inventory` | Admin |
| Schedule Management | `/schedule-management` | Admin |
| Review Moderation | `/reviews` | Admin |

---

## API Endpoints

All responses use the envelope `{ success, message, data, errors }`.

### Auth
| Method | Endpoint | Access |
|--------|----------|--------|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |

### Profile
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/profile` | Authenticated |
| PUT | `/api/profile` | Authenticated |
| PUT | `/api/profile/password` | Authenticated |

### Users
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/users?page&pageSize&role&search` | Admin |
| GET | `/api/users/{id}` | Admin |
| POST | `/api/users` | Admin |
| PUT | `/api/users/{id}` | Admin (cannot deactivate/demote self) |
| DELETE | `/api/users/{id}` (deactivate) | Admin (cannot deactivate self) |

### Packages
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/packages?activeOnly` | Public (`activeOnly=false` honoured for Admin only) |
| GET | `/api/packages/{id}` | Public |
| POST | `/api/packages` | Admin |
| PUT | `/api/packages/{id}` | Admin |
| DELETE | `/api/packages/{id}` (deactivate) | Admin |

### Bookings
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/bookings?page&pageSize&status` | Admin |
| GET | `/api/bookings/my` | Customer / Employee |
| GET | `/api/bookings/{id}` | Owning customer, assigned employee, or Admin |
| GET | `/api/bookings/available-slots?packageId&date=yyyy-MM-dd` | Authenticated |
| POST | `/api/bookings` | Customer |
| PATCH | `/api/bookings/{id}/status` | Assigned employee / Admin |
| DELETE | `/api/bookings/{id}` (cancel) | Owning customer, assigned employee, or Admin |

### Inventory
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/inventory?page&pageSize&lowStockOnly` | Admin |
| GET | `/api/inventory/alerts` | Admin |
| GET | `/api/inventory/{id}` | Admin |
| POST | `/api/inventory` | Admin |
| PUT | `/api/inventory/{id}` | Admin |
| DELETE | `/api/inventory/{id}` | Admin (refused while used by a package) |

### Schedules
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/schedule/templates` | Admin |
| GET | `/api/schedule/templates/employee/{employeeId}` | Admin |
| POST | `/api/schedule/templates` | Admin |
| PUT | `/api/schedule/templates/{id}` | Admin |
| DELETE | `/api/schedule/templates/{id}` | Admin |
| GET | `/api/schedule/leaves` | Admin |
| GET | `/api/schedule/leaves/employee/{employeeId}` | Admin |
| POST | `/api/schedule/leaves` | Admin |
| DELETE | `/api/schedule/leaves/{id}` | Admin |

### Reviews
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/reviews/employee/{employeeId}` | Public |
| GET | `/api/reviews/booking/{bookingId}` | Reviewing customer, reviewed employee, or Admin |
| POST | `/api/reviews` | Customer |
| DELETE | `/api/reviews/{id}` | Admin |

---

## Key Business Logic

| Feature | Implementation |
|---------|----------------|
| JWT Auth (3 roles) | `AuthService` — BCrypt hash, HS256 JWT with role claims. Tokens of deactivated users (or users whose role changed) are rejected on every request (`Program.cs`, `OnTokenValidated`). |
| Schedules | `EmployeeScheduleTemplate` (date range, working-day bitmask, hours, optional break) + `EmployeeLeave` days |
| Slot engine | `BookingService.GetAvailableSlotsAsync` — 30-min candidates within employees' template hours, in the shop time zone |
| Least-busy assignment | `BookingService.CreateBookingAsync` — among free employees, the one with the fewest bookings that day |
| Status workflow | `BookingService.UpdateBookingStatusAsync` — strictly Pending → Confirmed → InProgress → Completed |
| Atomic inventory deduction | On Completed — EF transaction, rollback on insufficient stock |
| Double-booking prevention | Overlap check against non-cancelled bookings of the employee |
| Post-service review | `ReviewService.CreateReviewAsync` — only on Completed bookings, one per booking, employee taken from the booking |
| Low-stock alerts | `InventoryRepository.GetLowStockItemsAsync` — stock ≤ threshold |
| Pagination | `GenericRepository.GetPagedAsync` — `page` ≥ 1, `pageSize` clamped to 1–1000 |
| Soft delete | Users and Packages use `IsActive = false` to preserve historical booking data |
| Dates & times | Bookings are stored as UTC instants; template/leave dates are calendar days stored as UTC midnight |
