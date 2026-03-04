# API Contracts & CQRS Definitions: docDOC

**Feature**: 001-doc-backend
**Date**: 2026-02-28
**Version**: 1.1.0 (Reviewed & Fixed)

---

## Changes from v1.0.0

- `POST /api/auth/register` response changed from token to `201 Created` with profile — registration and login are separate concerns per Constitution Principle IV
- `LoginUserQuery` renamed to `LoginUserCommand` — login creates a refresh token (write operation), making it a Command not a Query
- Added full Chat REST contracts — `POST /api/chats`, `GET /api/chats`, `GET /api/chats/{id}/messages`
- Added role-to-transition mapping on `PATCH /api/appointments/{id}/status` — explicit about who can do what
- Added missing Notification write endpoints — `PUT /api/notifications/{id}/read` and `PUT /api/notifications/read-all`
- Added `GET /api/doctors/{id}/availability` — required for the booking calendar
- Added `GET /api/specialities` — required for doctor filtering UI

---

## 1. Authentication (`AuthController`)

---

### `POST /api/auth/register` — Public

Creates a new Patient or Doctor account. Returns the created profile only. The client must call `/login` separately to receive tokens.

**CQRS**: `RegisterUserCommand`

**Request**:

```json
{
  "email": "user@test.com",
  "password": "StrongPassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Patient",
  "specialityId": "1",
  "hospital": "City Hospital",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "dateOfBirth": "1990-05-15",
  "gender": "Male"
}
```

**Response `201 Created`**:

```json
{
  "id": "1",
  "email": "user@test.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Patient"
}
```

**Error Responses**:

| Code | Reason |
|------|--------|
| `400 Bad Request` | Validation failure (missing fields, weak password) |
| `409 Conflict` | Email already registered |

---

### `POST /api/auth/login` — Public

Authenticates a user and returns tokens. The `role` field is required to perform a direct single-table lookup.

**CQRS**: `LoginUserCommand` *(Command — creates a RefreshToken record)*

**Request**:

```json
{
  "email": "user@test.com",
  "password": "StrongPassword123!",
  "role": "Patient"
}
```

**Response `200 OK`**:

```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "raw-refresh-token-string",
  "expiresIn": 900,
  "user": {
    "id": "1",
    "firstName": "John",
    "lastName": "Doe",
    "email": "user@test.com",
    "role": "Patient"
  }
}
```

**Error Responses**:

| Code | Reason |
|------|--------|
| `400 Bad Request` | Missing role, email, or password |
| `401 Unauthorized` | Invalid credentials |

---

### `POST /api/auth/refresh-token` — Public

Issues new tokens. Revokes the used refresh token immediately.

**CQRS**: `RefreshTokenCommand`

**Request**:

```json
{
  "refreshToken": "raw-refresh-token-string"
}
```

**Response `200 OK`**:

```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "new-raw-refresh-token-string",
  "expiresIn": 900
}
```

**Error Responses**:

| Code | Reason |
|------|--------|
| `401 Unauthorized` | Token expired, revoked, or not found |

---

### `POST /api/auth/logout` — Authorized (Any role)

Blacklists the current JWT via its `jti` claim in Redis. Revokes all active refresh tokens for the user.

**CQRS**: `LogoutUserCommand`

**Request**: Empty body. JWT carried in `Authorization: Bearer` header.

**Response `204 No Content`**

---

## 2. Specialities (`SpecialitiesController`)

---

### `GET /api/specialities` — Public

Returns all available doctor specialities for use in filters and registration.

**CQRS**: `GetSpecialitiesQuery`

**Response `200 OK`**:

```json
[
  { "id": "1", "name": "General",    "iconCode": "general"    },
  { "id": "1", "name": "Neurologic", "iconCode": "neurologic" },
  { "id": "1", "name": "Pediatric",  "iconCode": "pediatric"  },
  { "id": "1", "name": "Radiology",  "iconCode": "radiology"  }
]
```

---

## 3. Doctor Discovery (`DoctorsController`)

---

### `GET /api/doctors/nearby` — Authorized: Patient

Find available doctors near the patient's GPS location. Redis GEO is the fast path. SQL Server Spatial is the fallback.

**CQRS**: `GetNearbyDoctorsQuery`

**Query Parameters**:

| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `lat` | double | Yes | — | Patient latitude |
| `lon` | double | Yes | — | Patient longitude |
| `radiusKm` | int | No | 10 | Search radius in km |
| `specialityId` | int | No | — | Filter by speciality |

**Response `200 OK`**:

```json
[
  {
    "id": "1",
    "firstName": "Jane",
    "lastName": "Smith",
    "speciality": { "id": "1", "name": "Pediatric" },
    "hospital": "City Hospital",
    "averageRating": 4.8,
    "totalReviews": 34,
    "distanceKm": 2.4,
    "isOnline": true
  }
]
```

---

### `GET /api/doctors/{id}/availability` — Authorized: Patient

Returns available 30-minute time slots for a specific doctor on a given date.

**CQRS**: `GetDoctorAvailabilityQuery`

**Query Parameters**:

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `date` | DateOnly | Yes | Date to check slots for (Mon–Sat only) |

**Response `200 OK`**:

```json
{
  "doctorId": "1",
  "date": "2026-03-10",
  "availableSlots": [
    "08:00", "08:30", "09:00", "09:30",
    "11:00", "14:30", "16:00"
  ]
}
```

> Slots are omitted if a non-`Cancelled` appointment already exists for that doctor at that time. Sunday and slots outside 08:00–17:00 are never returned.

---

### `PUT /api/doctors/location` — Authorized: Doctor

Updates the doctor's GPS location and online status. Syncs to both SQL Server and Redis GEO simultaneously.

**CQRS**: `UpdateDoctorLocationCommand`

**Request**:

```json
{
  "latitude": 40.7128,
  "longitude": -74.0060,
  "isOnline": true
}
```

**Response `204 No Content`**

---

## 4. Appointments (`AppointmentsController`)

---

### `POST /api/appointments` — Authorized: Patient

Books a 30-minute slot. Enforces double-booking prevention via UoW transaction check (Layer 2) and DB unique index (Layer 1).

**CQRS**: `BookAppointmentCommand`

**Request**:

```json
{
  "doctorId": "1",
  "date": "2026-03-10",
  "time": "09:30",
  "type": "InPerson"
}
```

**Response `201 Created`**:

```json
{
  "id": "1",
  "doctorId": "1",
  "date": "2026-03-10",
  "time": "09:30",
  "type": "InPerson",
  "status": "Pending",
  "createdAt": "2026-02-28T10:00:00Z"
}
```

**Error Responses**:

| Code | Reason |
|------|--------|
| `400 Bad Request` | Invalid date, time outside working hours, Sunday |
| `409 Conflict` | Slot already taken |

---

### `GET /api/appointments/mine` — Authorized: Patient or Doctor

Returns the current user's appointments. Filtered by role automatically via `userType` claim.

**CQRS**: `GetMyAppointmentsQuery`

**Query Parameters**:

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `status` | string | No | Filter by status |
| `page` | int | No | Default: 1 |
| `pageSize` | int | No | Default: 20, Max: 50 |

**Response `200 OK`**:

```json
{
  "items": [
    {
      "id": "1",
      "patientId": "1",
      "doctorId": "1",
      "date": "2026-03-10",
      "time": "09:30",
      "type": "InPerson",
      "status": "Confirmed"
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20
}
```

---

### `PATCH /api/appointments/{id}/status` — Authorized: Doctor or Patient

Changes appointment status. Role-to-transition rules are enforced at the Application layer.

**CQRS**: `UpdateAppointmentStatusCommand`

**Request**:

```json
{ "status": "Confirmed" }
```

**Role-to-Transition Matrix**:

| Transition | Allowed Role | Notes |
|------------|-------------|-------|
| `Pending → Confirmed` | Doctor only | Patient gets `appointment_confirmed` notification |
| `Confirmed → Completed` | Doctor only | Patient becomes eligible for review |
| `Pending → Cancelled` | Doctor or Patient | Other party notified, Hangfire job deleted |
| `Confirmed → Cancelled` | Doctor or Patient | Other party notified, Hangfire job deleted |
| `Completed → anything` | Nobody | Returns `409 Conflict` — immutable |
| `Cancelled → anything` | Nobody | Returns `409 Conflict` — immutable |
| `Pending → Completed` | Nobody | Invalid transition — returns `400 Bad Request` |

**Response `204 No Content`**

**Error Responses**:

| Code | Reason |
|------|--------|
| `400 Bad Request` | Invalid transition (e.g. Pending → Completed) |
| `403 Forbidden` | Role not allowed for this transition |
| `404 Not Found` | Appointment not found or does not belong to user |
| `409 Conflict` | Transition from immutable status (Completed or Cancelled) |

---

## 5. Chat (`ChatController`)

---

### `POST /api/chats` — Authorized: Patient or Doctor

Creates a ChatRoom between the current user and the other party. Requires a qualifying appointment. If a room already exists, returns the existing one.

**CQRS**: `CreateOrGetChatRoomCommand`

**Request**:

```json
{
  "otherUserId": "1",
  "otherUserType": "Doctor"
}
```

**Response `200 OK`** (existing) or **`201 Created`** (new):

```json
{
  "id": "1",
  "patientId": "1",
  "doctorId": "1",
  "isActive": true,
  "createdAt": "2026-02-28T10:00:00Z"
}
```

**Error Responses**:

| Code | Reason |
|------|--------|
| `403 Forbidden` | No qualifying Confirmed or Completed appointment exists |

---

### `GET /api/chats` — Authorized: Patient or Doctor

Returns all chat rooms for the current user, sorted by most recent activity.

**CQRS**: `GetMyChatRoomsQuery`

**Response `200 OK`**:

```json
[
  {
    "id": "1",
    "otherUser": {
      "id": "1",
      "firstName": "Jane",
      "lastName": "Smith",
      "role": "Doctor"
    },
    "isActive": true,
    "updatedAt": "2026-02-28T10:00:00Z"
  }
]
```

---

### `GET /api/chats/{id}/messages` — Authorized: Patient or Doctor

Returns paginated message history for a chat room using cursor-based pagination.

**CQRS**: `GetChatMessagesQuery`

**Query Parameters**:

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `cursor` | int | No | Last message Id from previous page |
| `limit` | int | No | Default: 20, Max: 50 |

**Response `200 OK`**:

```json
{
  "messages": [
    {
      "id": "1",
      "senderId": "1",
      "senderType": "Patient",
      "content": "Good morning doctor.",
      "status": "Read",
      "sentAt": "2026-02-28T09:40:00Z"
    }
  ],
  "nextCursor": 20,
  "hasMore": true
}
```

**Error Responses**:

| Code | Reason |
|------|--------|
| `403 Forbidden` | User is not a member of this chat room |

---

### `PUT /api/chats/{id}/messages/read` — Authorized: Patient or Doctor

Marks all unread messages in the room as `Read` for the current user.

**CQRS**: `MarkMessagesReadCommand`

**Response `204 No Content`**

---

## 6. Notifications (`NotificationsController`)

---

### `GET /api/notifications` — Authorized: Patient or Doctor

Pulls all notifications for the current user. Called on every app launch to recover missed events.

**CQRS**: `GetNotificationsQuery`

**Query Parameters**:

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `unreadOnly` | bool | No | Default: false |
| `page` | int | No | Default: 1 |
| `pageSize` | int | No | Default: 20 |

**Response `200 OK`**:

```json
{
  "items": [
    {
      "id": "1",
      "eventType": "appointment_confirmed",
      "content": "Your appointment has been confirmed.",
      "referenceId": "1",
      "isRead": false,
      "createdAt": "2026-02-28T10:00:00Z"
    }
  ],
  "totalCount": 3,
  "unreadCount": 2
}
```

---

### `PUT /api/notifications/{id}/read` — Authorized: Patient or Doctor

Marks a single notification as read.

**CQRS**: `MarkNotificationReadCommand`

**Response `204 No Content`**

**Error Responses**:

| Code | Reason |
|------|--------|
| `404 Not Found` | Notification not found or does not belong to user |

---

### `PUT /api/notifications/read-all` — Authorized: Patient or Doctor

Marks all notifications as read for the current user.

**CQRS**: `MarkAllNotificationsReadCommand`

**Response `204 No Content`**

---

## 7. Reviews (`ReviewsController`)

---

### `POST /api/reviews` — Authorized: Patient

Submits a review for a completed appointment. One review per appointment enforced at both DB (unique index) and Application layer.

**CQRS**: `SubmitReviewCommand`

**Request**:

```json
{
  "appointmentId": "1",
  "rating": 5,
  "comment": "Great doctor, highly recommend."
}
```

**Response `201 Created`**:

```json
{
  "id": "1",
  "appointmentId": "1",
  "doctorId": "1",
  "rating": 5,
  "comment": "Great doctor, highly recommend.",
  "createdAt": "2026-02-28T10:00:00Z"
}
```

**Error Responses**:

| Code | Reason |
|------|--------|
| `400 Bad Request` | Rating outside 1–5, appointment not Completed |
| `403 Forbidden` | Appointment does not belong to this Patient |
| `409 Conflict` | Review already submitted for this appointment |

---

## 8. SignalR Hubs

---

### `ChatHub` (`/hubs/chat`)

All methods require a valid JWT passed as a query parameter: `?access_token=eyJhbGci...`

**Client → Server Events**:

| Method | Payload | Description |
|--------|---------|-------------|
| `JoinRoom` | `{ roomId: "1" }` | Subscribe to room group |
| `LeaveRoom` | `{ roomId: "1" }` | Unsubscribe from room group |
| `SendMessage` | `{ roomId: "1", content: "string" }` | Send message — persisted to DB then broadcast |
| `Typing` | `{ roomId: "1" }` | Broadcast typing indicator |
| `MarkRead` | `{ roomId: "1" }` | Mark all messages read — updates DB and broadcasts |

**Server → Client Events**:

| Event | Payload | Trigger |
|-------|---------|---------|
| `OnReceiveMessage` | `MessageDto` | New message sent in room |
| `OnTypingIndicator` | `{ userId, senderType }` | User is typing |
| `OnMessagesRead` | `{ userId, roomId }` | Messages marked as read |
| `OnUserOnline` | `{ userId }` | Contact connected |
| `OnUserOffline` | `{ userId }` | Contact disconnected |

---

### `NotificationHub` (`/hubs/notifications`)

All connections require a valid JWT.

**Server → Client Events**:

| Event | Payload | Trigger |
|-------|---------|---------|
| `OnNotification` | `NotificationDto` | Any system event (appointment change, new message) |

> The `NotificationHub` is receive-only. Clients do not send events to it. It purely delivers server-initiated notifications in real time to online users.

---

## 9. CQRS Full Reference

| Type | Name | Layer | Triggered By |
|------|------|-------|--------------|
| Command | `RegisterUserCommand` | Application | `POST /api/auth/register` |
| Command | `LoginUserCommand` | Application | `POST /api/auth/login` |
| Command | `RefreshTokenCommand` | Application | `POST /api/auth/refresh-token` |
| Command | `LogoutUserCommand` | Application | `POST /api/auth/logout` |
| Query | `GetSpecialitiesQuery` | Application | `GET /api/specialities` |
| Query | `GetNearbyDoctorsQuery` | Application | `GET /api/doctors/nearby` |
| Query | `GetDoctorAvailabilityQuery` | Application | `GET /api/doctors/{id}/availability` |
| Command | `UpdateDoctorLocationCommand` | Application | `PUT /api/doctors/location` |
| Command | `BookAppointmentCommand` | Application | `POST /api/appointments` |
| Query | `GetMyAppointmentsQuery` | Application | `GET /api/appointments/mine` |
| Command | `UpdateAppointmentStatusCommand` | Application | `PATCH /api/appointments/{id}/status` |
| Command | `CreateOrGetChatRoomCommand` | Application | `POST /api/chats` |
| Query | `GetMyChatRoomsQuery` | Application | `GET /api/chats` |
| Query | `GetChatMessagesQuery` | Application | `GET /api/chats/{id}/messages` |
| Command | `MarkMessagesReadCommand` | Application | `PUT /api/chats/{id}/messages/read` |
| Query | `GetNotificationsQuery` | Application | `GET /api/notifications` |
| Command | `MarkNotificationReadCommand` | Application | `PUT /api/notifications/{id}/read` |
| Command | `MarkAllNotificationsReadCommand` | Application | `PUT /api/notifications/read-all` |
| Command | `SubmitReviewCommand` | Application | `POST /api/reviews` |

---

**Version**: 1.1.0 | **Date**: 2026-02-28 | **Last Updated**: 2026-02-28
