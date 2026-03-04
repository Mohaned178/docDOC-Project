# Technical Research & Architecture Decisions: docDOC Backend

**Feature**: 001-doc-backend
**Date**: 2026-02-28

---

## 1. Clean Architecture + CQRS + Unit of Work

**Decision**: The solution is divided into four projects: `docDOC.Domain`, `docDOC.Application`, `docDOC.Infrastructure`, and `docDOC.Api`. CQRS is implemented using MediatR. All commands are wrapped in a `UnitOfWorkBehavior` MediatR pipeline behavior that calls `SaveChangesAsync` automatically after every successful command handler. Queries bypass the UoW behavior entirely — they are read-only and must never trigger a save.

**Rationale**: Required by Constitution Principle I. Separates read and write models, enforces transactional integrity without polluting every handler with boilerplate, and keeps Infrastructure dependencies out of Application and Domain layers.

**Alternatives considered**: Traditional N-Tier with Service classes. Rejected — the Constitution explicitly mandates CQRS and Unit of Work. Service classes would couple business logic to infrastructure concerns.

---

## 2. Separated User Tables, No Shared Schema

**Decision**: A single `/api/auth/register` endpoint accepts a `role` field. Based on this field, the payload is mapped to either a `Patient` or `Doctor` entity and inserted into completely separate SQL Server tables. No shared `Users` base table, no ASP.NET Core Identity, no TPH inheritance. Login also requires the `role` field to perform a direct single-table lookup without cross-table searching.

**Rationale**: Constitution Principle IV mandates separated tables to prevent nullable columns and shared schema pollution while keeping a unified frontend registration experience.

**Alternatives considered**:

- **TPH (Table-Per-Hierarchy) with EF Core**: Rejected — creates a wide, sparse table with many nullable columns. Exactly what the Constitution prohibits.
- **ASP.NET Core Identity**: Rejected — Identity's `AspNetUsers` table is a single-user-table assumption that directly contradicts the separated schema mandate. Identity's biggest wins (email confirmation, 2FA, lockout) are all out of scope for MVP. What remains — password hashing and JWT — are two NuGet packages away without Identity's constraints. See Decision 14 for full reasoning.

---

## 3. JWT Claims: `role` vs `userType` — Two Separate Claims

**Decision**: JWT tokens contain both a `role` claim and a `userType` claim. Both carry the same value (`"Patient"` or `"Doctor"`) in MVP but serve completely different layers and must never be merged into one claim.

- `role` is consumed by ASP.NET Core's `[Authorize(Roles)]` middleware for endpoint-level access control. The framework reads this automatically — no application code touches it.
- `userType` is consumed by Application layer handlers via `ICurrentUserService` to route queries to the correct database table (`patients` or `doctors`). Application code reads this explicitly.

**Rationale**: Constitution Principle IV mandates both claims. Merging them into one would couple the framework's authorization mechanism to the application's internal routing logic. If the authorization model ever grows more complex (admin roles, staff roles), `role` can evolve independently without breaking internal table routing.

**Alternatives considered**: Single `role` claim used for both purposes. Rejected — the framework reads `role` for `[Authorize]` and application code would have to read the same claim for table routing, coupling two completely different concerns to one field.

---

## 4. JWT Blacklist on Logout — Redis String, Not Set

**Decision**: On logout, the JWT's `jti` claim is written as an individual Redis **String** key — `blacklist:{jti}` — with a TTL exactly matching the token's remaining expiry time. A custom `JwtRedisBlacklistMiddleware` checks `EXISTS blacklist:{jti}` on every authenticated request and returns `401` if the key exists.

**Rationale**: Constitution Principle IV requires explicit Redis-based token blacklisting. Redis `EXISTS` check on a String key is under 10ms, meeting SC-004. Using a Redis Set with `SADD` / `SISMEMBER` was considered and rejected for a critical reason: Redis Sets have no per-member TTL. Individual members cannot expire. This means a Set-based blacklist would grow indefinitely — every logged-out token would remain in the Set forever, consuming memory with no cleanup path. The String-per-`jti` approach with TTL is self-cleaning — keys expire automatically when the token would have expired anyway.

**Alternatives considered**:

- **Redis Set with `SISMEMBER`**: Rejected — no per-member TTL, unbounded growth.
- **DB whitelist (active sessions table)**: Rejected — requires a DB query on every request, violating the SC-004 latency requirement.

---

## 5. Refresh Token Storage — Hash, Never Raw

**Decision**: Refresh tokens are generated as cryptographically random strings. Only the SHA-256 hash of the raw token is stored in the `RefreshTokens` SQL Server table (`TokenHash` column). The raw token is returned to the client once and never stored server-side. Lookup on refresh is done by hashing the incoming raw token and querying against `TokenHash`. Tokens expire after 7 days (`ExpiresAt`) and are revoked immediately on use or logout (`IsRevoked = true`).

**Rationale**: If the `RefreshTokens` table is ever compromised via SQL injection or a database breach, raw tokens cannot be used by an attacker. The hash is useless without the raw value. This is the same reasoning behind never storing raw passwords.

**Alternatives considered**: Storing raw tokens in DB. Rejected — a database breach would immediately expose all active sessions. Hashing costs one SHA-256 operation per refresh request, which is negligible.

---

## 6. Geo-Spatial Doctor Discovery — Two-Tier Strategy

**Decision**: Implement a two-tier fallback strategy using exact Redis key `doctors:geo`.

- **Fast path (Redis GEO)**: Doctor locations are synced to Redis on every `UpdateDoctorLocationCommand` using `GEOADD doctors:geo {lon} {lat} {doctorId}`. When a doctor goes offline, their entry is removed with `ZREM doctors:geo {doctorId}`. Nearby searches use `GEOSEARCH doctors:geo ... BYRADIUS {radius} km ASC COUNT 20`.
- **Fallback / source of truth (SQL Server Spatial)**: The `Doctors` table includes a `geography` column. EF Core uses `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite`. Queries use spatial functions like `.IsWithinDistance()`. Results are cached in `nearby:cache:{lat}:{lon}:{radius}` with a 2-minute TTL.

The fast path is tried first on every request. SQL Server Spatial is only invoked if Redis returns empty results or is unavailable.

**Rationale**: Constitution Principle II mandates Redis GEO as the primary discovery mechanism with SQL Server Spatial as the reliable fallback. Redis GEO operations are O(N+log(M)) and operate entirely in memory, meeting SC-001 (< 100ms).

**Alternatives considered**: Pure SQL Server Spatial only. Rejected — spatial queries involve disk I/O and are slower under concurrent load. Pure Redis only. Rejected — Redis is an ephemeral cache; SQL Server is the durable source of truth. A Redis flush must not lose doctor location data permanently.

---

## 7. SignalR with Redis Backplane

**Decision**: Implement `ChatHub` and `NotificationHub` using ASP.NET Core SignalR. Configure `builder.Services.AddSignalR().AddStackExchangeRedis(redisConnString, opts => opts.Configuration.ChannelPrefix = "docDOC")` to support horizontal scaling across multiple API instances. All messages and notifications are persisted to SQL Server via UoW **before** `Clients.User(id).SendAsync()` is called. SignalR delivery is best-effort on top of a guaranteed persistence layer.

**Rationale**: Constitution Principle III mandates SignalR with Redis backplane. Principle VII mandates persistence regardless of delivery success. Persisting before sending ensures SC-002 (100% message durability) even if the SignalR connection drops between the persist and the send.

**Alternatives considered**: Raw WebSockets. Rejected — SignalR provides automatic reconnection handling, group management, and the Redis backplane for multi-server fan-out natively. Reimplementing those features on raw WebSockets adds significant complexity with no benefit.

---

## 8. Chat Authorization — Gate at Room Creation, Not Message Send

**Decision**: The qualifying appointment check (`Confirmed` or `Completed` appointment between Patient and Doctor) is enforced inside `CreateOrGetChatRoomCommand` in the Application layer. If no qualifying appointment exists, a `ForbiddenException` is thrown, which maps to `403 Forbidden`. `SendMessageCommand` performs a secondary check — it verifies `ChatRoom.IsActive = true` — but does NOT re-run the appointment query. These are two separate guards for two separate scenarios.

| Guard | Location | Checks | Covers |
|-------|----------|--------|--------|
| Appointment gate | `CreateOrGetChatRoomCommand` | Qualifying appointment exists | FR-008, SC-005 |
| Soft-close guard | `SendMessageCommand` | `ChatRoom.IsActive = true` | FR-014 |

**Rationale**: Constitution Principle VI mandates Application layer enforcement. Placing the gate at room creation means it fires once — not on every message. Re-running the appointment query on every `SendMessageCommand` would add unnecessary DB load and is architecturally incorrect. The room's existence (and its `IsActive` flag) is the persistent record of authorization state.

**Alternatives considered**: Controller-layer `[Authorize]` policy. Rejected — the Constitution explicitly demands Application layer enforcement, not infrastructure or API layer.

---

## 9. ChatRoom Soft-Close on Cancellation

**Decision**: When an appointment is cancelled, the system re-evaluates whether the ChatRoom between the two parties remains valid. If no other `Confirmed` or `Completed` appointment exists between the same Patient and Doctor, the ChatRoom is **soft-closed** — `IsActive` is set to `false`. Message history is fully preserved and readable. New messages are rejected with `403 Forbidden`. The ChatRoom is **not deleted**. If a new qualifying appointment is created later, `IsActive` is restored to `true` automatically.

**Rationale**: Constitution Principle VI mandates soft-close behavior. Deleting the ChatRoom would destroy conversation history which has ongoing medical value. Soft-close preserves history while correctly enforcing the access control rule.

**Alternatives considered**: Hard delete ChatRoom on cancellation. Rejected — destroys medical conversation history. No action on cancellation. Rejected — violates Principle VI which requires re-evaluation on every cancellation.

---

## 10. Hangfire Background Jobs — Scheduled at Booking, Not Confirmation

**Decision**: The Hangfire appointment reminder job is scheduled inside `BookAppointmentCommand` — at the moment the appointment is created with `Pending` status. The Hangfire job ID is stored on `Appointment.HangfireJobId` (two-step write: create appointment → schedule job → persist job ID). When an appointment is cancelled by either party, the job is deleted via `BackgroundJob.Delete(appointment.HangfireJobId)`.

**Critical clarification**: The reminder is scheduled at **booking time**, not confirmation time. This is intentional — the Constitution says "scheduled at booking time" explicitly (FR-010). If the Doctor never confirms, the job is deleted on cancellation. If the Doctor confirms, the job fires as scheduled.

**Rationale**: Constitution Principle V mandates Hangfire for background jobs. Scheduling at booking rather than confirmation is simpler — there is no need to detect a transition and reschedule. The job either fires or gets deleted.

**Alternatives considered**:

- `IHostedService` / `BackgroundService`: Rejected — in-memory, does not survive server restarts. Hangfire's SQLServer persistence ensures jobs survive restarts.
- Schedule at confirmation: Rejected — adds unnecessary complexity and a second scheduling trigger. Booking is the correct moment per the Constitution.

---

## 11. Double-Booking Prevention — Two Mandatory Layers

**Decision**: Double-booking is prevented by two layers that are both mandatory. Neither is optional.

**Layer 1 — Database (catches true race conditions)**:

```sql
CREATE UNIQUE INDEX ON "Appointments" ("DoctorId", "Date", "Time")
WHERE "Status" != 'Cancelled';
```

This partial unique index makes it physically impossible for two non-cancelled appointments to exist for the same doctor at the same date and time. Even if two requests hit the database simultaneously, only one INSERT succeeds. The second throws `SqlException` with `SqlState = "23505"`, which is caught and mapped to `ConflictException → 409`.

**Layer 2 — Application (closes the application-level race window)**:
Inside `BookAppointmentCommand`, an explicit transaction is opened via `IUnitOfWork.BeginTransactionAsync()`. `IsSlotTakenAsync(doctorId, date, time)` is called inside the transaction before the INSERT. If taken, `ConflictException` is thrown immediately without touching the DB.

**Rationale**: FR-016 and SC-003 mandate both layers. Layer 2 alone is insufficient — two requests can both read "slot is free" before either writes. Layer 1 alone is insufficient — it catches the race at the DB level but the error surface is a raw DB exception rather than a clean application response. Together they provide deterministic 0% double-booking with a clean `409 Conflict` response in all scenarios.

---

## 12. MessageStatus as Three-State Enum

**Decision**: `Message.Status` is a `MessageStatus` enum with three states: `Sent`, `Delivered`, `Read`. It is not a boolean `IsRead` field.

| State | Meaning |
|-------|---------|
| `Sent` | Persisted to SQL Server |
| `Delivered` | SignalR confirmed the recipient's client received it |
| `Read` | Recipient explicitly triggered the read event |

**Rationale**: A boolean `IsRead` collapses `Sent` and `Delivered` into the same state and permanently loses delivery tracking. The mobile UI needs to show "sent", "delivered", and "read" indicators — identical to standard chat applications. `bool IsRead = true` cannot represent the difference between "delivered but not read" and "read."

**Alternatives considered**: Boolean `IsRead`. Rejected — insufficient for three-state delivery tracking.

---

## 13. Speciality as a Table, Not an Enum

**Decision**: Specialities are stored in a separate `Specialities` table with `Id`, `Name`, and `IconCode` columns. The `Doctor` entity has a `SpecialityId` FK. The table is seeded with four initial entries via EF Core `HasData` using **stable, hardcoded integer IDs** — not auto-increment sequences (`SERIAL` / `IDENTITY`) which regenerates on every migration comparison.

**Rationale**: A C# enum requires a code change and a migration for every new speciality. A table requires only a data migration (seed). Telemedicine platforms grow their speciality catalogue frequently. The table approach keeps the schema evolvable without code changes.

**Critical implementation note**: Seed data IDs must be hardcoded literals:

```csharp
new Speciality { Id = 1, Name = "General", IconCode = "general" }
```

Using auto-increment sequences (`SERIAL` / `IDENTITY`) in `HasData` causes EF Core to detect a "change" on every `dotnet ef migrations add` and generate spurious empty migrations.

**Alternatives considered**: C# enum stored as integer or string. Rejected — requires code change and migration for new entries, and cannot store metadata like `IconCode` without additional workarounds.

---

## 14. No ASP.NET Core Identity

**Decision**: Authentication is implemented with a custom stack: BCrypt.Net for password hashing, a custom `JwtService` for token generation, a `RefreshTokens` SQL Server table, and a Redis blacklist for logout. ASP.NET Core Identity is explicitly not used.

**Rationale**: Identity's core assumption is a single `AspNetUsers` table. Our Constitution mandates separate `Patients` and `Doctors` tables. Adapting Identity to two separate tables requires overriding `UserStore`, `UserManager`, `SignInManager`, and the token provider — more code than building auth from scratch, with a framework actively working against the design. Additionally, Identity's primary value-adds (email confirmation, 2FA, account lockout, external providers) are all out of scope for MVP. What remains — password hashing and JWT — are two NuGet packages.

**What our stack provides vs Identity**:

| Feature | Identity | Our Stack |
|---------|----------|-----------|
| Password hashing | `PasswordHasher<T>` (PBKDF2) | BCrypt.Net (bcrypt) |
| JWT generation | Not included — you write it | Custom `JwtService` |
| Refresh tokens | Not included — you write it | `RefreshTokens` table |
| Role enforcement | `AspNetRoles` table | JWT `role` claim |
| Token blacklist | Not included | Redis `blacklist:{jti}` |
| Two separate user tables | ❌ Fights the framework | ✅ Native |
| Email confirmation | Built-in | Out of scope |
| 2FA / lockout | Built-in | Out of scope |

**Alternatives considered**: ASP.NET Core Identity with custom `UserStore`. Rejected — the volume of overrides required exceeds the cost of a custom implementation, and the framework's assumptions create ongoing maintenance friction.

---

## Decision Index

| # | Decision | Constitution Principle | FR / SC |
|---|----------|----------------------|---------|
| 1 | Clean Architecture + CQRS + UoW | I | All |
| 2 | Separated user tables, no shared schema | IV | FR-001, FR-002 |
| 3 | `role` vs `userType` — two claims, two layers | IV | FR-003 |
| 4 | Redis String blacklist per `jti` with TTL | IV | FR-004, SC-004 |
| 5 | Refresh token hash storage | IV | FR-012 |
| 6 | Two-tier geo discovery (Redis GEO + SQL Server Spatial) | II | FR-005, FR-006, SC-001 |
| 7 | SignalR with Redis backplane, persist before send | III, VII | FR-009, SC-002 |
| 8 | Chat gate at room creation, soft-close at message send | VI | FR-008, SC-005 |
| 9 | ChatRoom soft-close on cancellation | VI | FR-014 |
| 10 | Hangfire scheduled at booking, job ID stored | V | FR-010, FR-013 |
| 11 | Two-layer double-booking prevention | VIII | FR-016, SC-003 |
| 12 | `MessageStatus` three-state enum | III | FR-009 |
| 13 | Speciality as table with stable seed IDs | II | — |
| 14 | No ASP.NET Core Identity | IV | FR-001, FR-002 |
