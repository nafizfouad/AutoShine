# AutoShine — System Workflow Documentation

AutoShine is a car-washing and auto-detailing management platform with three distinct user roles: **Admin**, **Employee**, and **Customer**. This document describes every workflow each role can perform.

---

## 1. Authentication (All Roles)

### Register
Any visitor can create a new account at /register.
- Provide first name, last name, email, password, and phone.
- New accounts are created as **Customer** by default.
- Duplicate emails are rejected.

### Login
Any registered user can sign in at /login.
- Provide email and password.
- On success, a JWT token is issued and stored in the browser.
- The token carries the user role and ID, which the app uses to show role-specific navigation.

### Sign Out
Available from the sidebar on any authenticated page. Clears the local token and redirects to login.

---

## 2. Admin Workflows

### 2.1 Dashboard (/dashboard)
- See at-a-glance stats: Total Users, Total Bookings, Low Stock Alerts, Active Packages.
- View the 5 most recent bookings across all customers.
- View inventory items currently below threshold (low-stock alerts).

### 2.2 Manage Packages (/packages)
- View all packages (active and inactive).
- Create package: name, description, price, duration, active flag, and linked inventory items with quantities.
- Edit any existing package.
- Deactivate / Activate a package (inactive packages are hidden from customers).
- Delete a package permanently.

> Linked inventory items are consumed (deducted) when a booking is marked Completed.

### 2.3 Manage Users (/users)
- View all users, paginated, with search by name/email and filter by role.
- Create user: name, email, password, phone, role (Admin / Employee / Customer).
- Edit any user: name, email, phone, role, active status.
- Deactivate user: sets IsActive = false so the user cannot log in.

### 2.4 Manage Bookings (/bookings)
- View all bookings, filterable by status.
- Advance booking status: Pending -> Confirmed -> InProgress -> Completed.
- Cancel any booking that is not Completed or already Cancelled.

> Marking Completed auto-deducts inventory linked to the package in a database transaction.

### 2.5 Inventory Management (/inventory)
- View all items with optional low-stock-only filter.
- Add item: name, SKU, current stock, minimum threshold, unit.
- Edit or delete any item.

### 2.6 Schedule Management (/schedule-management)
Templates (Recurring Availability):
- View all templates (employee, date range, working days, work hours, break window).
- Create template: pick employee, date range, working days bitmask, work start/end, optional break.
- Edit template: pencil icon opens modal pre-filled with existing values.
- Delete template.

Leave Days (One-off Absence):
- View all leave days across all employees.
- Add leave: employee + date + optional reason.
- Delete leave to restore availability.

### 2.7 Review Moderation (/reviews)
- Browse reviews per employee, see average rating and total count.
- Delete any review.

### 2.8 Profile (/profile)
- Edit own name and phone.
- Change password (requires current password).

---

## 3. Employee Workflows

### 3.1 Dashboard (/dashboard)
- See today job count and status breakdown.
- View next 5 upcoming assigned bookings.

### 3.2 My Bookings (/bookings)
- View all assigned bookings, filterable by status.
- Advance status: Pending -> Confirmed -> InProgress -> Completed.
- Cancel a booking.

### 3.3 Profile (/profile)
- Edit own name and phone.
- Change password.

---

## 4. Customer Workflows

### 4.1 Dashboard (/dashboard)
- Quick stats: Upcoming, Completed, Total Bookings.
- Table of upcoming appointments.

### 4.2 Browse Services (/services)
- View all active packages with name, description, price, duration, and materials.
- Click Book This to jump to the booking wizard with that package pre-selected.

### 4.3 Book a Service (/book) — 4-step wizard
Step 1 — Service: choose a package.
Step 2 — Date: pick a date from the calendar.
Step 3 — Time and Staff: see available slots for the chosen date and package; each slot shows available employees. Pick a preferred employee or No Preference for auto-assign.
Step 4 — Confirm: review the full summary and add optional notes, then confirm. Booking is created as Pending.

### 4.4 My Bookings (/bookings)
- View all own bookings, filterable by status.
- Cancel any non-completed, non-cancelled booking.
- Leave a review on Completed bookings that have an assigned employee.

### 4.5 Leave a Review
- Rate the employee 1-5 stars and add an optional comment.
- One review per booking.

### 4.6 Profile (/profile)
- Edit name and phone.
- Change password (email is read-only).

---

## 5. Booking Lifecycle

  Pending -> Confirmed -> InProgress -> Completed (inventory deducted here)
  Any non-completed stage -> Cancelled

| Status    | Who Sets It          | Meaning                              |
|-----------|----------------------|--------------------------------------|
| Pending   | System on creation   | Awaiting confirmation                |
| Confirmed | Employee / Admin     | Appointment confirmed                |
| InProgress| Employee / Admin     | Car is being serviced                |
| Completed | Employee / Admin     | Job done; inventory auto-deducted    |
| Cancelled | Customer/Employee/Admin | Booking void                      |

---

## 6. Slot Availability Algorithm

When a customer picks a date and package, available time slots are computed:
1. Load active schedule templates whose date range covers the chosen date.
2. Filter to templates whose workingDays bitmask includes the day-of-week.
3. Exclude employees on leave that date.
4. Generate 30-minute candidate slots from earliest work-start to latest work-end.
5. For each slot and each employee: verify work window covers the slot, break window does not overlap, and no existing booking conflicts.
6. Slots with at least one free employee are returned.

---

## 7. Inventory and Stock Management

- Each package lists inventory items with required quantities.
- On Completed, stock is atomically deducted in a transaction; rolled back if any item has insufficient stock.
- Low-stock alerts fire when currentStock <= minimumThreshold (visible on Admin dashboard and Inventory page).

---

## 8. Role Capability Summary

| Capability                   | Admin | Employee | Customer |
|------------------------------|:-----:|:--------:|:--------:|
| Register / Login             |  YES  |   YES    |   YES    |
| View and edit own profile    |  YES  |   YES    |   YES    |
| Browse services              |   -   |    -     |   YES    |
| Book a service               |   -   |    -     |   YES    |
| View own bookings            |  YES  |   YES    |   YES    |
| Cancel a booking             |  YES  |   YES    |   YES    |
| Advance booking status       |  YES  |   YES    |    -     |
| Leave a review               |   -   |    -     |   YES    |
| Manage all users             |  YES  |    -     |    -     |
| Manage packages              |  YES  |    -     |    -     |
| View all bookings            |  YES  |    -     |    -     |
| Manage inventory             |  YES  |    -     |    -     |
| Manage schedules and leaves  |  YES  |    -     |    -     |
| Moderate reviews             |  YES  |    -     |    -     |
| View admin dashboard stats   |  YES  |    -     |    -     |
