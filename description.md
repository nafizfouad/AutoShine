# AutoShine — System Workflow Documentation

AutoShine is a car-wash and auto-service management platform with three user roles: **Admin**, **Employee**, and **Customer**. This document describes the workflows available to each role.

---

## 1. Authentication

### Register (`/register`)
Visitors can create their own account.
- Provide first name, last name, email, phone, and a password (minimum 8 characters).
- Self-registered accounts are always **Customers**. Admin and Employee accounts are created by an Admin.
- Each email address can be registered only once.

### Login (`/login`)
- Sign in with email and password. Deactivated accounts cannot sign in.
- On success, a JWT is issued and stored in the browser. It carries the user's ID and role, which drive the role-specific navigation.
- If an Admin deactivates a user or changes their role, that user's current session ends and they must sign in again.

### Sign Out
Available from the sidebar on every authenticated page. Clears the session and returns to the login page.

---

## 2. Admin Workflows

### 2.1 Dashboard (`/dashboard`)
- Stats: total users, total bookings, low-stock alerts, active packages.
- The 5 most recent bookings across all customers.
- Inventory items at or below their minimum threshold.

### 2.2 Manage Packages (`/packages`)
- View all packages, active and inactive, with their linked inventory items.
- Create a package: name, description, price, duration, active flag, and inventory items with quantities.
- Edit any package.
- Deactivate or re-activate a package. Inactive packages are hidden from customers and cannot be booked.
- Packages are deactivated rather than deleted, so existing bookings keep their history.

> A package's linked inventory items are deducted from stock when a booking for it is marked Completed.

### 2.3 Manage Users (`/users`)
- View all users with pagination, case-insensitive search by name or email, and a role filter.
- Create a user: name, email, password, phone, and role (Admin / Employee / Customer).
- Edit a user: name, email, phone, role, and active status.
- Deactivate a user so they can no longer sign in; re-activate them by editing the user.
- Admins cannot deactivate their own account or remove their own Admin role.

### 2.4 Manage Bookings (`/bookings`)
- View all bookings, filterable by status.
- Advance a booking one step at a time: Pending → Confirmed → InProgress → Completed. Steps cannot be skipped or reversed.
- Cancel any booking that is not Completed or already Cancelled.

> Marking a booking Completed deducts the package's inventory in a single database transaction.

### 2.5 Inventory (`/inventory`)
- View all items, optionally only those with low stock.
- Add an item: name, SKU (unique), current stock, minimum threshold, and unit.
- Edit any item.
- Delete an item, provided no package uses it.

### 2.6 Schedule Management (`/schedule-management`)
**Schedule templates (recurring availability)**
- View all templates: employee, date range, working days, working hours, and break.
- Create a template: employee, date range, working days, start and end time, and an optional break.
  Times are in the shop's configured time zone. The break must fall within working hours.
- Edit or delete a template.

**Leave days (one-off absence)**
- View all leave days across employees.
- Add a leave day: employee, date, and optional reason (one per employee per day).
- Delete a leave day to restore availability.

### 2.7 Review Moderation (`/reviews`)
- Browse reviews per employee, with average rating and review count.
- Delete any review.

### 2.8 Profile (`/profile`)
- Edit own name and phone.
- Change password (requires the current password).

---

## 3. Employee Workflows

### 3.1 Dashboard (`/dashboard`)
- Today's job count and a breakdown by status.
- The next 5 upcoming assigned bookings.

### 3.2 My Bookings (`/bookings`)
- View all bookings assigned to them, filterable by status.
- Advance their assigned bookings: Pending → Confirmed → InProgress → Completed.
- Cancel an assigned booking.

### 3.3 Profile (`/profile`)
- Edit own name and phone.
- Change password.

---

## 4. Customer Workflows

### 4.1 Dashboard (`/dashboard`)
- Stats: upcoming, completed, and total bookings.
- A table of upcoming appointments.

### 4.2 Browse Services (`/services`)
- View all active packages with name, description, price, duration, and included materials.
- **Book This** opens the booking wizard with that package pre-selected.

### 4.3 Book a Service (`/book`) — 4-step wizard
1. **Service** — choose a package.
2. **Date** — pick a date from the calendar.
3. **Time & Staff** — choose one of the available time slots for that date and package, then pick a specific employee or **No Preference**.
4. **Confirm** — review the summary, add optional notes, and confirm. The booking is created as **Pending**.

### 4.4 My Bookings (`/bookings`)
- View own bookings, filterable by status.
- Cancel own bookings that are not Completed or Cancelled.
- Leave a review on Completed bookings that have an assigned employee.

### 4.5 Leave a Review
- Rate the employee who performed the service from 1 to 5 stars, with an optional comment.
- One review per booking.

### 4.6 Profile (`/profile`)
- Edit name and phone (email is read-only).
- Change password.

---

## 5. Booking Lifecycle

```
Pending → Confirmed → InProgress → Completed   (inventory deducted on Completed)
Pending / Confirmed / InProgress → Cancelled
```

| Status     | Set by                                       | Meaning                           |
|------------|----------------------------------------------|-----------------------------------|
| Pending    | System, on creation                          | Awaiting confirmation             |
| Confirmed  | Assigned employee or Admin                   | Appointment confirmed             |
| InProgress | Assigned employee or Admin                   | Vehicle is being serviced         |
| Completed  | Assigned employee or Admin                   | Job done; inventory deducted      |
| Cancelled  | Booking's customer, assigned employee, or Admin | Booking void                   |

---

## 6. Availability and Assignment

Available slots for a date and package are calculated in the shop's time zone:
1. Take the active schedule templates of active employees whose date range includes the date and whose working days include that weekday.
2. Exclude employees on leave that day.
3. Generate 30-minute candidate start times between the earliest start and the latest end of those templates, skipping times that have already passed.
4. An employee is free for a slot if their working hours cover the whole slot, the slot does not overlap their break, and they have no other non-cancelled booking at that time.
5. Every slot with at least one free employee is offered, together with the list of free employees.

When a booking is created, the same rules are checked again:
- **Specific employee** — the booking succeeds only if that employee is free for the slot.
- **No Preference** — the free employee with the fewest bookings that day is assigned.

---

## 7. Inventory and Stock

- Each package lists the inventory items it consumes and their quantities.
- When a booking is marked Completed, stock for all of the package's items is deducted in one transaction. If any item has insufficient stock, nothing is deducted and the booking stays InProgress.
- An item is low on stock when its current stock is at or below its minimum threshold. Low-stock items appear on the Admin dashboard and can be filtered on the Inventory page.

---

## 8. Role Capability Summary

| Capability                     | Admin | Employee | Customer |
|--------------------------------|:-----:|:--------:|:--------:|
| Self-register                  |   -   |    -     |   YES    |
| Log in                         |  YES  |   YES    |   YES    |
| View and edit own profile      |  YES  |   YES    |   YES    |
| Browse services                |   -   |    -     |   YES    |
| Book a service                 |   -   |    -     |   YES    |
| View bookings                  |  All  | Assigned |   Own    |
| Cancel a booking               |  All  | Assigned |   Own    |
| Advance booking status         |  All  | Assigned |    -     |
| Leave a review                 |   -   |    -     |   YES    |
| Manage users                   |  YES  |    -     |    -     |
| Manage packages                |  YES  |    -     |    -     |
| Manage inventory               |  YES  |    -     |    -     |
| Manage schedules and leave     |  YES  |    -     |    -     |
| Moderate reviews               |  YES  |    -     |    -     |
| View admin dashboard stats     |  YES  |    -     |    -     |
