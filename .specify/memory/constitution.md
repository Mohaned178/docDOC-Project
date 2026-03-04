# DOC Constitution

## Core Principles

### I. Architecture First (Clean Architecture & CQRS)

The backend MUST be built using Clean Architecture principles, CQRS for separating read and write models, and Unit of Work for transactional integrity. No cross-layer contamination is permitted — Infrastructure must never be referenced directly from Domain or Application layers.

### II. Performance and Scalability

The system MUST be horizontally scalable from day one. Redis MUST be used as a backplane for SignalR and as the fast path for nearby doctor discovery using Redis GEO. PostGIS MUST be utilized as the accurate fallback and source of truth for all geo data. The database schema MUST stay normalized, well-indexed, and easy to evolve with migrations.

### III. Real-Time Communication

Real-time text chat and in-app notifications MUST be delivered with minimal latency using SignalR. All real-time messages and notifications MUST be persisted in PostgreSQL regardless of delivery success, ensuring no data is lost.

### IV. Unified but Separated User Management

Patients and Doctors MUST register and login through a single endpoint to avoid duplication, but MUST be stored in separate tables — since their profiles, fields, and business rules fundamentally differ — to keep the schema clean and avoid nullable columns. The login request MUST include the `role` field alongside email and password to enable direct single-table lookup and avoid cross-table searching. Role differentiation MUST be communicated via JWT claims (`sub`, `email`, `role`, `userType`, `jti`) and the login response body simultaneously. The `role` claim is consumed by ASP.NET Core's `[Authorize(Roles)]` middleware for endpoint-level access control. The `userType` claim is consumed by Application layer handlers via `ICurrentUserService` to route queries to the correct database table. Both claims carry the same value in MVP but serve different layers and MUST NOT be merged into one claim. JWT `jti` claims MUST be used for explicit Redis-based token blacklisting on logout to prevent reuse before expiry. The system MUST support a refresh token flow — access tokens expire in 15 minutes, refresh tokens expire in 7 days and are revoked upon use or logout.

### V. Asynchronous Processing

Background jobs MUST be processed automatically using Hangfire to ensure reliability. Appointment reminders MUST be scheduled at booking time and delivered one day before the appointment date. No time-sensitive business logic should block the request thread.

### VI. Chat Access Control

A ChatRoom MUST only be created between a Patient and a Doctor who share at least one `Confirmed` or `Completed` appointment. Direct chat requests without a prior qualifying appointment MUST be rejected with a `403 Forbidden` response. This rule MUST be enforced at the Application layer, not the controller layer. When an appointment is cancelled, the system MUST re-evaluate ChatRoom validity — if no other qualifying appointment exists between the two parties, the ChatRoom MUST be soft-closed: message history is preserved but new messages are rejected with `403 Forbidden`.

### VII. Notification Reliability

All notifications MUST be persisted in PostgreSQL regardless of the user's SignalR connection state. SignalR handles real-time delivery when the user is online. The mobile client MUST call `GET /api/notifications` on every app launch to recover any notifications missed during offline periods. This two-layer approach compensates for the absence of FCM in the current version.

### VIII. Appointment Slot Rules (MVP)

Working hours are fixed at **08:00–17:00, Monday through Saturday**. Each slot is **30 minutes**. A slot is considered unavailable if a non-`Cancelled` appointment already exists for that doctor at that date and time. Double-booking prevention MUST be enforced through two mandatory layers — a partial unique index on `(doctor_id, appointment_date, appointment_time) WHERE status != 'Cancelled'` at the database level, AND an explicit availability check inside a Unit of Work transaction at the Application layer. Both layers are required. Neither is optional. When an appointment is cancelled, the associated Hangfire reminder job MUST be deleted. The `Completed` status is immutable — no transition out of `Completed` is permitted for any actor. This is a configuration-level constraint for MVP and will be replaced by a dynamic doctor schedule management feature in a future version.

---

## Core Features & Requirements

- **Doctor Discovery**: Patients locate nearby available doctors via GPS coordinates. Redis GEO is the fast path. PostGIS `ST_DWithin` is the accurate fallback. Results are sorted by distance and limited to available doctors only.
- **Speciality Browsing**: Doctors are categorized into specialities such as Neurologic, Pediatric, and Radiology. Patients can filter the doctor list by speciality.
- **Appointment Booking**: Patients book 30-minute slots within working hours as either In-Person or Online. Full status lifecycle is tracked: `Pending → Confirmed → Completed / Cancelled`.
- **Chat & Notifications**: SignalR powers real-time messaging with typing indicators, read receipts, online presence, and cursor-based message history. Chat access is gated behind a confirmed appointment.
- **Ratings & Reviews**: Patients submit 1–5 star ratings with written reviews after a completed appointment. Doctor average rating is dynamically recalculated after every new review. One review is allowed per appointment.
- **Doctor Availability**: Doctors toggle their online/offline status and update their GPS location in real time. Location changes are synced to both PostgreSQL and Redis GEO simultaneously.

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Backend Framework | ASP.NET Core  |
| Architecture Pattern | Clean Architecture + CQRS + Unit of Work |
| Primary Database | PostgreSQL with PostGIS extension |
| Caching & Real-time State | Redis |
| Real-time Communication | SignalR with Redis backplane |
| Background Jobs | Hangfire with PostgreSQL persistence |
| Authentication | JWT + Refresh Tokens |
| Validation | FluentValidation |
| Logging | Serilog |
| Mapping | AutoMapper |

---

## Out of Scope (Version 1)

- Push notifications to device (FCM) — planned for Version 2
- Video or voice calls
- Payment and billing processing
- Admin dashboard
- Profile photo uploads
- Dynamic doctor schedule management
- Multi-language support

---

## Governance

- Changes to the core architecture, including database schema strategy, caching layer, or authentication design, MUST be documented in an updated Constitution before implementation begins.
- All PRs MUST ensure Clean Architecture layers are respected. No cross-layer contamination is permitted.
- Real-time features MUST use the Redis backplane and SignalR. Direct HTTP polling is not an acceptable substitute.
- All Application layer use cases MUST be unit testable without requiring a database or infrastructure dependency. Integration tests MUST cover critical flows: auth, booking, chat, and notifications.
- The `role` field is immutable after registration. A user cannot change their role.

---

**Version**: 1.2.0 | **Ratified**: 2026-02-28 | **Last Amended**: 2026-02-28