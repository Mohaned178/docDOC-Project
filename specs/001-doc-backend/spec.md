# Feature Specification: doc-backend

**Feature Branch**: `001-doc-backend`
**Created**: 2026-02-28
**Status**: Reviewed & Updated (v1.2.0)
**Input**: DOC Constitution v1.1.0

---

## User Scenarios & Testing *(mandatory)*

### Feature: Auth

#### User Story 1 — Patient & Doctor Unified Registration (Priority: P1)

As a new user (Patient or Doctor), I want to register through a single unified endpoint by specifying my role, so that I am routed to the correct profile table and given the right access.

**Why this priority**: Core entry point into the system. Required for all other features.
**Independent Test**: Create a Patient and a Doctor account, verify JWT claims, and confirm records are routed to separate database tables.

**Acceptance Scenarios**:

1. **Given** valid registration details with `role="Patient"`, **When** the registration is submitted, **Then** the user is created in the `patients` table and login returns a JWT with `role: "Patient"` and `userType: "Patient"` claims.
2. **Given** valid registration details with `role="Doctor"`, **When** the registration is submitted, **Then** the user is created in the `doctors` table and login returns a JWT with `role: "Doctor"` and `userType: "Doctor"` claims.
3. **Given** a login request, **When** it is submitted, **Then** it MUST include the `role` field alongside email and password — the backend performs a direct single-table lookup using the role to avoid cross-table searching.
4. **Given** a logged-in user, **When** they logout, **Then** the JWT `jti` claim is blacklisted in Redis and all subsequent requests using that token are rejected.

> **Claim Distinction**: `role` is read by ASP.NET Core's `[Authorize(Roles)]` middleware for endpoint-level access control. `userType` is read by Application layer handlers via `ICurrentUserService` to route queries to the correct database table. Both always carry the same value in MVP but serve different layers and must never be merged into one claim.

---

#### User Story 1b — Silent Token Refresh (Priority: P1)

As a Patient or Doctor, I want my session to be silently renewed before my access token expires, so that I am never interrupted mid-session by an unexpected logout.

**Why this priority**: Without this, every user gets forcibly logged out every 15 minutes. This is a usability blocker.
**Independent Test**: Issue a login, wait for the access token to expire, call the refresh endpoint with a valid refresh token, verify a new access token is returned with a fresh `jti`.

**Acceptance Scenarios**:

1. **Given** a valid non-expired refresh token, **When** `POST /api/auth/refresh-token` is called, **Then** a new access token (15 min expiry) and a new refresh token (7 day expiry) are returned and the old refresh token is revoked.
2. **Given** an expired refresh token, **When** `POST /api/auth/refresh-token` is called, **Then** the server returns `401 Unauthorized` and the user must log in again.
3. **Given** a revoked refresh token (from a previous logout or refresh), **When** the client attempts to use it, **Then** the server returns `401 Unauthorized` even if it has not expired by date.
4. **Given** a successful token refresh, **When** the new access token is issued, **Then** it carries the same `role`, `userType`, and `sub` claims as the original but with a new `jti`.

**Token Lifetime Rules**:

| Token | Lifetime | Revocation |
|-------|----------|------------|
| Access Token | 15 minutes | Redis blacklist via `jti` on logout |
| Refresh Token | 7 days | `is_revoked = true` on use or logout |

---

### Feature: Doctor Discovery

#### User Story 2 — Patient Discovers Nearby Doctors (Priority: P1)

As a Patient, I want to find available doctors near my current GPS location, so that I can find medical help quickly.

**Why this priority**: Primary value proposition for Patients.
**Independent Test**: Mock doctor locations in Redis/SQL Server and verify search results based on given coordinates.

**Acceptance Scenarios**:

1. **Given** the Patient provides valid GPS coordinates, **When** they search for nearby doctors, **Then** the system returns available doctors sorted by distance using Redis GEO as the fast path.
2. **Given** Redis GEO is temporarily unavailable or out of sync, **When** the Patient searches, **Then** SQL Server Spatial calculates distances accurately as a fallback.
3. **Given** multiple doctors nearby, **When** the Patient applies a speciality filter (e.g., Pediatric), **Then** only doctors matching that speciality are returned.

---

### Feature: Appointments

#### User Story 3 — Patient Books an Appointment (Priority: P1)

As a Patient, I want to book a 30-minute time slot with a doctor, so that I can schedule an in-person or online consultation.

**Why this priority**: Core conversion metric and primary interaction between roles.
**Independent Test**: Select an available slot and confirm booking succeeds; attempt a parallel booking for the same slot and confirm it fails with `409 Conflict`.

**Acceptance Scenarios**:

1. **Given** working hours are 08:00–17:00 Mon–Sat, **When** a Patient views available slots, **Then** only slots without a non-`Cancelled` appointment for that doctor are shown.
2. **Given** a selected available slot, **When** the Patient confirms the booking, **Then** the appointment is created with `Pending` status and a notification is triggered to the Doctor.
3. **Given** two Patients attempt to book the exact same slot simultaneously, **When** both requests reach the system, **Then** only one succeeds and the other receives `409 Conflict` — enforced by both a partial unique database index on `(doctor_id, appointment_date, appointment_time) WHERE status != 'Cancelled'` AND an explicit slot check inside a Unit of Work transaction.
4. **Given** a successful booking, **When** the transaction commits, **Then** a Hangfire reminder job is scheduled to fire one day before the appointment date.

---

#### User Story 4 — Appointment Lifecycle Management (Priority: P1)

As a Doctor, I want to confirm, complete, or cancel appointments, so that I can manage my consultation schedule accurately.
As a Patient, I want to cancel an appointment I no longer need, so that the slot becomes available for others.

**Why this priority**: Required for the appointment lifecycle to advance and for both parties to manage their schedules.
**Independent Test**: Trigger each status transition via the appropriate role's endpoint and verify status change, notification delivery, and side effects.

**Acceptance Scenarios**:

1. **Given** a `Pending` appointment, **When** the Doctor confirms it, **Then** the status changes to `Confirmed` and the Patient receives an `appointment_confirmed` notification via SignalR, persisted to SQL Server.
2. **Given** a `Confirmed` appointment, **When** the Doctor marks it as completed, **Then** the status changes to `Completed`, the Patient receives an `appointment_completed` notification, and the Patient becomes eligible to submit a review.
3. **Given** a `Pending` or `Confirmed` appointment, **When** the Doctor cancels it, **Then** the status changes to `Cancelled`, the Hangfire reminder job is deleted, and the Patient receives an `appointment_cancelled` notification persisted to SQL Server.
4. **Given** a `Pending` or `Confirmed` appointment, **When** the Patient cancels it, **Then** the status changes to `Cancelled`, the Hangfire reminder job is deleted, and the Doctor receives an `appointment_cancelled` notification persisted to SQL Server.
5. **Given** a `Completed` appointment, **When** either party attempts to cancel it, **Then** the system returns `409 Conflict` and the status remains `Completed`.
6. **Given** a cancelled appointment was the only qualifying appointment between Patient and Doctor, **When** the cancellation is processed, **Then** the ChatRoom is soft-closed — message history is preserved but new messages are rejected with `403 Forbidden`.
7. **Given** a cancelled appointment but another `Confirmed` or `Completed` appointment exists between the same Patient and Doctor, **When** the cancellation is processed, **Then** the ChatRoom remains fully open.

**Status Transition Rules**:

```
Pending ──► Confirmed ──► Completed  (final, immutable)
   │              │
   └──────────────┴──► Cancelled     (final, immutable)
```

---

### Feature: Chat

#### User Story 5 — Real-Time Chat (Priority: P2)

As a Patient or Doctor, I want to send and receive real-time messages within the app, so that I can communicate during an active consultation lifecycle.

**Why this priority**: Solves immediate remote communication without external tools.
**Independent Test**: Connect two SignalR clients with valid credentials and verify message delivery and persistence.

**Acceptance Scenarios**:

1. **Given** a Patient and Doctor with NO prior `Confirmed` or `Completed` appointment, **When** either attempts to open a ChatRoom, **Then** the attempt is rejected with `403 Forbidden` at the Application layer.
2. **Given** a valid ChatRoom between a Patient and Doctor, **When** a user sends a message, **Then** it is delivered in real-time via SignalR and immediately persisted to SQL Server.
3. **Given** an active chat, **When** the other party is typing, **Then** a typing indicator event is broadcast via SignalR.
4. **Given** a user reads messages in a room, **When** they trigger the read event, **Then** message statuses update to `Read` and the other party receives a `MessagesRead` event.

---

### Feature: Notifications

#### User Story 6 — Reliable In-App Notifications (Priority: P2)

As a User, I want to receive in-app notifications for important events, so that I am always informed even if I was offline when the event occurred.

**Why this priority**: User retention and timely responses depend on reliable notification delivery.
**Independent Test**: Trigger an appointment status change and verify both SignalR delivery and `GET /api/notifications` response.

**Acceptance Scenarios**:

1. **Given** a critical event occurs while the User is online, **When** the event fires, **Then** a real-time SignalR notification is delivered AND the notification is persisted to SQL Server.
2. **Given** the User was offline during critical events, **When** the User launches the app and calls `GET /api/notifications`, **Then** all unread notifications are returned from SQL Server.

---

### Feature: Reviews

#### User Story 7 — Post-Consultation Reviews (Priority: P3)

As a Patient, I want to rate and review my doctor after a completed appointment, so that I can share my experience and help others make informed decisions.

**Why this priority**: Important for marketplace trust but not a blocker for MVP core loop.
**Independent Test**: Submit a rating against a completed appointment and verify the doctor's average rating recalculation.

**Acceptance Scenarios**:

1. **Given** a `Completed` appointment, **When** the Patient submits a 1–5 star rating and review body, **Then** the review is saved and linked to that appointment.
2. **Given** a new review is saved, **When** the transaction commits, **Then** the Doctor's average rating is dynamically recalculated.
3. **Given** an appointment that already has a review, **When** the Patient attempts to submit another, **Then** the system rejects it with `409 Conflict`.

---

### Edge Cases

- **Race condition on booking**: Two Patients booking the same slot simultaneously — handled by a partial unique index at DB level AND a UoW transaction check at application level. Both layers are mandatory.
- **Doctor goes offline mid-chat**: SignalR connection drops — the notification is already persisted in SQL Server and will be returned on next app launch via `GET /api/notifications`.
- **Hangfire server restarts before sending reminder**: Hangfire's SQL Server-persisted job queue ensures the reminder fires upon server recovery without duplication.
- **Appointment cancelled after chat opened**: ChatRoom is soft-closed if no other qualifying appointment exists — history preserved, new messages rejected.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support user registration and authentication through a single `/register` and `/login` endpoint accepting the `role` field.
- **FR-002**: The system MUST persist Patient and Doctor profiles in completely separate tables.
- **FR-003**: The system MUST issue JWT tokens containing `sub`, `email`, `role`, `userType`, and `jti` claims. `role` is used by ASP.NET Core `[Authorize]` middleware for endpoint access control. `userType` is used by Application layer handlers for internal database table routing. Both claims carry the same value in MVP but serve different layers and must not be merged.
- **FR-004**: The system MUST blacklist tokens in Redis on logout using the `jti` claim. Blacklisted tokens MUST be rejected on all subsequent requests.
- **FR-005**: The system MUST allow Doctors to toggle online status and update GPS coordinates, syncing to both SQL Server and Redis GEO simultaneously.
- **FR-006**: The system MUST serve nearby Doctor searches using Redis GEO as the fast path and SQL Server Spatial as the accurate fallback.
- **FR-007**: The system MUST enforce 30-minute appointment slots within 08:00–17:00, Monday through Saturday.
- **FR-008**: The system MUST gate ChatRoom creation behind a `Confirmed` or `Completed` appointment check enforced at the Application layer. Unauthorized attempts MUST return `403 Forbidden`.
- **FR-009**: The system MUST persist all chat messages and notifications to SQL Server regardless of SignalR delivery success.
- **FR-010**: The system MUST schedule a Hangfire reminder job upon successful appointment booking to fire one day before the appointment date.
- **FR-011**: The system MUST recalculate a Doctor's average rating upon submission of a new Patient review.
- **FR-012**: The system MUST support a refresh token flow. Upon a valid refresh request, the old refresh token MUST be revoked and a new access token plus new refresh token MUST be issued. Expired or revoked refresh tokens MUST return `401 Unauthorized`.
- **FR-013**: When an appointment is cancelled by either party, the system MUST delete the associated Hangfire reminder job.
- **FR-014**: When an appointment is cancelled, the system MUST evaluate ChatRoom validity. If no other qualifying appointment exists between the two parties, the ChatRoom MUST be soft-closed — history preserved, new messages rejected with `403 Forbidden`.
- **FR-015**: The `Completed` appointment status is immutable. No transition out of `Completed` is permitted for any actor. Attempts MUST return `409 Conflict`.
- **FR-016**: Double-booking prevention MUST be enforced through two mandatory layers — a partial unique index on `(doctor_id, appointment_date, appointment_time) WHERE status != 'Cancelled'` at the database level, AND an explicit availability check inside a Unit of Work transaction at the Application layer. A `409 Conflict` MUST be returned when a slot is already taken.

### Key Entities

- **Patient**: Profile details, credentials, date of birth, gender.
- **Doctor**: Profile details, credentials, speciality, hospital, GPS coordinates, average rating, online status.
- **Appointment**: Patient ID, Doctor ID, date, time, type (In-Person/Online), status (`Pending`, `Confirmed`, `Completed`, `Cancelled`).
- **ChatRoom**: Authorized link between Patient and Doctor — requires qualifying appointment. Supports soft-close state.
- **Message**: Text content, sender ID, sender type, read status, timestamp.
- **Notification**: User ID, user type, event type, content, read status, reference ID.
- **Review**: Appointment ID, star rating (1–5), written content. One per appointment.
- **RefreshToken**: User ID, user type, token hash, expiry, revocation status.

---

## Success Criteria *(mandatory)*

- **SC-001**: Nearby Doctor Discovery (Redis GEO fast path) responds in under 100ms.
- **SC-002**: 100% of real-time messages are durably persisted to SQL Server without data loss, even during SignalR transient failures.
- **SC-003**: Appointment double-booking rate is 0%, guaranteed by two mandatory layers — a partial unique index on `(doctor_id, appointment_date, appointment_time) WHERE status != 'Cancelled'` at the database level AND an explicit availability check inside a Unit of Work transaction at the Application layer. Both layers are required. Neither is optional.
- **SC-004**: Logged-out JWT tokens are rejected within under 10ms via Redis blacklist validation.
- **SC-005**: Chat access is 100% restricted — no user can create a ChatRoom without a qualifying `Confirmed` or `Completed` appointment.

---

**Version**: 1.2.0 | **Created**: 2026-02-28 | **Last Updated**: 2026-02-28
