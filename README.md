# AutoShine — Auto Repair & Car Wash Management System

A full-stack web application for managing the daily operations of an auto repair and car wash shop:
service packages, online booking with staff scheduling, inventory tracking, and customer reviews.

- [description.md](description.md) — workflows for each role (Admin, Employee, Customer)
- [DEPLOYMENT.md](DEPLOYMENT.md) — free cloud deployment with Docker, Render and Neon

## Architecture

```
AutoShine.sln
├── src/AutoShine.Models       → Domain entities & enums
├── src/AutoShine.Data         → EF Core DbContext, configurations, migrations, seeder
├── src/AutoShine.Repository   → Repositories & Unit of Work
├── src/AutoShine.Service      → DTOs, Mapster mappings, business services
├── src/AutoShine              → ASP.NET Core Web API (controllers, middleware, Program.cs)
├── src/AutoShine.UnitTests    → xUnit tests (EF Core InMemory)
└── src/client                 → React + Vite frontend
```

Backend dependency flow:
```
AutoShine (API) → AutoShine.Service → AutoShine.Repository → AutoShine.Data → AutoShine.Models
```

The Docker image bundles the built React app into the API's `wwwroot`, so the application runs as a
single container serving both the UI and `/api` from one origin.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core Web API (.NET 9) |
| ORM / Database | Entity Framework Core 8 + PostgreSQL (Npgsql) |
| Auth | JWT bearer tokens, role-based (Admin / Employee / Customer) |
| Passwords | BCrypt |
| Mapping | Mapster |
| API docs | Swagger / OpenAPI (Development environment) |
| Frontend | React 19, Vite, React Router 7, Axios, react-hot-toast, lucide-react |
| Container | Docker (multi-stage build), Docker Compose |

---

## Configuration

Settings are read from `src/AutoShine/appsettings.json` and environment variables (environment
variables take precedence). In environment variables, `:` becomes `__`, e.g. `Jwt:SecretKey` → `Jwt__SecretKey`.

| Setting | Required | Description |
|---------|:--------:|-------------|
| `ConnectionStrings:DefaultConnection` | ✅ | PostgreSQL connection string, e.g. `Host=localhost;Port=5432;Database=autoshine;Username=postgres;Password=...` |
| `Jwt:SecretKey` | ✅ | Token signing key, at least 32 characters |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryHours` | | Defaults: `AutoShine.API` / `AutoShine.Client` / `24` |
| `Seed:AdminEmail` / `Seed:AdminPassword` | first run | Creates the first Admin account when none exists (password min. 8 characters) |
| `Seed:DemoUsers` / `Seed:DemoPassword` | | Development environment only: creates demo employees and a demo customer |
| `Shop:TimeZone` | | Time zone of the shop's working hours (IANA id, e.g. `Asia/Dhaka`). Default: the server's time zone |
| `Cors:AllowedOrigins` | | Comma-separated origins allowed to call the API from another domain. Default: `http://localhost:5173,http://localhost:3000` |
| `PORT` (env var) | | Port to listen on, as set by most hosting platforms. Docker default: `8080` |

`src/AutoShine/appsettings.example.json` contains the full structure with placeholder values.
The app stops at startup with an explanatory error if the connection string or JWT key is missing.

---

## Running with Docker (recommended)

Prerequisite: Docker with Compose.

```bash
cp .env.example .env          # fill in POSTGRES_PASSWORD, JWT_SECRET_KEY, SEED_ADMIN_PASSWORD, ...
docker compose up --build
```

- App (UI + API): **http://localhost:8080**
- Health check: **http://localhost:8080/health**

Compose starts PostgreSQL and the app. Database migrations and seed data are applied automatically on
startup. Data is kept in the `pgdata` volume; `docker compose down -v` removes it.

To build and run the image on its own (e.g. against an external database):

```bash
docker build -t autoshine .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..." \
  -e Jwt__SecretKey="..." \
  -e Seed__AdminEmail="admin@example.com" -e Seed__AdminPassword="..." \
  autoshine
```

---

## Running locally without Docker

Prerequisites: .NET 9 SDK, Node.js 20+, and a PostgreSQL 14+ database.

**1. Configure** — copy the template and fill in the values:
```bash
cp src/AutoShine/appsettings.example.json src/AutoShine/appsettings.json
```

**2. Start the API** (migrations and seeding run on startup):
```bash
cd src/AutoShine
dotnet run
```
API: **http://localhost:5200** · Swagger: **http://localhost:5200/swagger**

**3. Start the frontend:**
```bash
cd src/client
npm install
npm run dev
```
App: **http://localhost:5173** (requests to `/api` are proxied to the API)

**4. Run the tests:**
```bash
dotnet test
```

---

## Seed Data

- **Inventory items and service packages** are seeded into an empty database.
- **Admin account** — created from `Seed:AdminEmail` / `Seed:AdminPassword` when no Admin exists.
  Otherwise a warning is logged at startup.
- **Demo accounts** — only in the Development environment, when `Seed:DemoUsers` is `true` and
  `Seed:DemoPassword` is set. They all use `Seed:DemoPassword`:

  | Role | Email |
  |------|-------|
  | Employee | john.mechanic@autoshine.com |
  | Employee | sarah.washer@autoshine.com |
  | Customer | alice@example.com |

  Demo employees get a Mon–Fri 09:00–17:00 schedule with a 13:00–14:00 break, valid for five years
  from the start of the current year.

Seeding never modifies existing accounts.

---

## Frontend Pages

| Page | Path | Access |
|------|------|--------|
| Login | `/login` | Public |
| Register | `/register` | Public |
| Dashboard | `/dashboard` | All roles (role-specific view) |
| Bookings | `/bookings` | All roles (Admin: all; Employee: assigned; Customer: own) |
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

Responses use the envelope `{ success, message, data, errors }`.

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
| PUT | `/api/users/{id}` | Admin (not own deactivation/demotion) |
| DELETE | `/api/users/{id}` (deactivate) | Admin (not self) |

### Packages
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/packages?activeOnly` | Public (`activeOnly=false` applies to Admins only) |
| GET | `/api/packages/{id}` | Public |
| POST | `/api/packages` | Admin |
| PUT | `/api/packages/{id}` | Admin |
| DELETE | `/api/packages/{id}` (deactivate) | Admin |

### Bookings
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/bookings?page&pageSize&status` | Admin |
| GET | `/api/bookings/my` | Customer / Employee |
| GET | `/api/bookings/{id}` | Booking's customer, assigned employee, or Admin |
| GET | `/api/bookings/available-slots?packageId&date=yyyy-MM-dd` | Authenticated |
| POST | `/api/bookings` | Customer |
| PATCH | `/api/bookings/{id}/status` | Assigned employee or Admin |
| DELETE | `/api/bookings/{id}` (cancel) | Booking's customer, assigned employee, or Admin |

### Inventory
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/api/inventory?page&pageSize&lowStockOnly` | Admin |
| GET | `/api/inventory/alerts` | Admin |
| GET | `/api/inventory/{id}` | Admin |
| POST | `/api/inventory` | Admin |
| PUT | `/api/inventory/{id}` | Admin |
| DELETE | `/api/inventory/{id}` | Admin (only items not used by a package) |

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

### Health
| Method | Endpoint | Access |
|--------|----------|--------|
| GET | `/health` | Public |

---

## Key Business Logic

| Feature | Implementation |
|---------|----------------|
| Authentication | `AuthService` — BCrypt password hashes, HS256 JWT with role claims. Each request re-checks that the user is still active and has the same role. |
| Staff schedules | `EmployeeScheduleTemplate` (date range, working-day bitmask, hours, optional break) and `EmployeeLeave` days |
| Slot engine | `BookingService.GetAvailableSlotsAsync` — 30-minute slots within employees' working hours, in the shop time zone |
| Staff assignment | `BookingService.CreateBookingAsync` — chosen employee if free, otherwise the least-busy free employee that day |
| Status workflow | `BookingService.UpdateBookingStatusAsync` — Pending → Confirmed → InProgress → Completed, one step at a time |
| Inventory deduction | Deducted in a transaction when a booking is Completed; rolled back on insufficient stock |
| Double-booking prevention | Overlap check against the employee's non-cancelled bookings |
| Reviews | `ReviewService.CreateReviewAsync` — Completed bookings only, one per booking, for the assigned employee |
| Low-stock alerts | `InventoryRepository.GetLowStockItemsAsync` — stock ≤ threshold |
| Pagination | `page` ≥ 1, `pageSize` 1–1000 |
| Soft delete | Users and packages are deactivated (`IsActive = false`) to keep booking history |
| Dates & times | Bookings are stored as UTC instants; template and leave dates are calendar dates |
