# AutoShine — Auto Repair & Car Wash Management System (ARWMS)

A full-stack web application for managing daily operations of an auto repair and car wash shop.

## Architecture

```
AutoShine.sln
├── src/AutoShine.Models       → Domain entities & enums (Class Library)
├── src/AutoShine.Data         → EF Core DbContext, Configurations, Migrations, Seeder
├── src/AutoShine.Repository   → Repository interfaces & implementations, Unit of Work
├── src/AutoShine.Service      → DTOs, AutoMapper profiles, service interfaces & implementations
└── src/AutoShine              → ASP.NET Core Web API (controllers, middleware, Program.cs)
```

### Dependency Flow
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
| Docs | Swagger / OpenAPI |
| Frontend | React + Vite (coming soon) |

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (or SQL Server Express / LocalDB)

### Setup
1. Update the connection string in `src/AutoShine/appsettings.json`
2. Run the API — migrations and seed data apply automatically on startup:
   ```bash
   cd src/AutoShine
   dotnet run
   ```
3. Visit Swagger UI at: **http://localhost:5000/swagger**

### Seed Accounts
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@autoshine.com | Admin@123 |
| Employee | john.mechanic@autoshine.com | Employee@123 |
| Employee | sarah.washer@autoshine.com | Employee@123 |
| Customer | alice@example.com | Customer@123 |

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

## Key Business Logic
- **Slot Engine:** Available slots calculated from shop hours (08:00–18:00) in 30-min intervals, filtered by employee schedule and existing bookings
- **Employee Assignment:** Round-robin / least-busy algorithm when "Any Available" is selected
- **Inventory Deduction:** Atomic DB transaction when booking marked `Completed` — rolls back if insufficient stock
- **Double-Booking Prevention:** Overlap query on employee bookings at creation time