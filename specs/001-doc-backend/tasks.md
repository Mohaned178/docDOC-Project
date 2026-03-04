# Tasks: docDOC Telemedicine Backend

**Source of Truth:** plan.md v1.1.0
**Feature Specification:** spec.md v1.2.0

---

## Phase 1: Foundation, Entities & Auth (P1)

**Definition of Done:**

- Patient and Doctor can register, login, refresh token, and logout.
- JWT blacklist rejects logged-out tokens under 10ms (SC-004).
- Separate tables confirmed via DB inspection.
- Specialities table seeded with four entries.

---

### 1.0 — NuGet Packages

- [x] T001 Install Application packages in `src/docDOC.Application/docDOC.Application.csproj`:

  ```
  dotnet add src/docDOC.Application package MediatR
  dotnet add src/docDOC.Application package FluentValidation.DependencyInjectionExtensions
  dotnet add src/docDOC.Application package BCrypt.Net-Next
  ```

- [x] T002 Install Infrastructure packages in `src/docDOC.Infrastructure/docDOC.Infrastructure.csproj`:

  ```
  dotnet add src/docDOC.Infrastructure package Microsoft.EntityFrameworkCore.Design
  dotnet add src/docDOC.Infrastructure package Microsoft.EntityFrameworkCore.Tools
  dotnet add src/docDOC.Infrastructure package Microsoft.EntityFrameworkCore
  dotnet add src/docDOC.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
  dotnet add src/docDOC.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite
  dotnet add src/docDOC.Infrastructure package StackExchange.Redis
  dotnet add src/docDOC.Infrastructure package System.IdentityModel.Tokens.Jwt
  dotnet add src/docDOC.Infrastructure package Microsoft.AspNetCore.Authentication.JwtBearer
  ```

- [x] T003 Install API packages in `src/docDOC.Api/docDOC.Api.csproj`:

  ```
  dotnet add src/docDOC.Api package Swashbuckle.AspNetCore
  dotnet add src/docDOC.Api package Serilog.AspNetCore
  dotnet add src/docDOC.Api package AutoMapper.Extensions.Microsoft.DependencyInjection
  ```

---

### 1.1 — Solution & Project Setup

- [x] T004 Create solution `docDOC` via `dotnet new sln -n docDOC` in `/`
- [x] T005 [P] Create Domain project in `src/docDOC.Domain/docDOC.Domain.csproj`
- [x] T006 [P] Create Application project in `src/docDOC.Application/docDOC.Application.csproj`
- [x] T007 [P] Create Infrastructure project in `src/docDOC.Infrastructure/docDOC.Infrastructure.csproj`
- [x] T008 [P] Create API project in `src/docDOC.Api/docDOC.Api.csproj`
- [x] T009 Configure project references (API → Infrastructure → Application → Domain) in `docDOC.sln`
- [x] T010 Configure SQL Server, Redis, and JWT settings in `src/docDOC.Api/appsettings.json`

---

### 1.2 — Domain Entities

- [x] T011 [P] Create `BaseEntity` with `Id` (int) and `CreatedAt` (DateTimeOffset) in `src/docDOC.Domain/Entities/BaseEntity.cs`
- [x] T012 [P] Create `Gender` enum in `src/docDOC.Domain/Enums/Gender.cs`
- [x] T013 [P] Create `AppointmentStatus` enum (Pending, Confirmed, Completed, Cancelled) in `src/docDOC.Domain/Enums/AppointmentStatus.cs`
- [x] T014 [P] Create `AppointmentType` enum (InPerson, Online) in `src/docDOC.Domain/Enums/AppointmentType.cs`
- [x] T015 [P] Create `MessageStatus` enum (Sent, Delivered, Read) in `src/docDOC.Domain/Enums/MessageStatus.cs`
- [x] T016 [P] Create `SenderType` enum (Patient, Doctor) in `src/docDOC.Domain/Enums/SenderType.cs`
- [x] T017 [P] Create `UserType` enum (Patient, Doctor) in `src/docDOC.Domain/Enums/UserType.cs`
- [x] T018 [P] Create `DomainException` base exception in `src/docDOC.Domain/Exceptions/DomainException.cs`
- [x] T019 [P] Create `NotFoundException` extending `DomainException` in `src/docDOC.Domain/Exceptions/NotFoundException.cs`
- [x] T020 [P] Create `ForbiddenException` extending `DomainException` in `src/docDOC.Domain/Exceptions/ForbiddenException.cs`
- [x] T021 [P] Create `ConflictException` extending `DomainException` in `src/docDOC.Domain/Exceptions/ConflictException.cs`
- [x] T022 [P] Create `Patient` entity extending `BaseEntity` in `src/docDOC.Domain/Entities/Patient.cs` (Satisfies: FR-002)
- [x] T023 [P] Create `Doctor` entity extending `BaseEntity` with `SpecialityId`, `AverageRating`, `TotalReviews`, `IsOnline`, `Location` in `src/docDOC.Domain/Entities/Doctor.cs` (Satisfies: FR-002, FR-005)
- [x] T024 [P] Create `Speciality` entity extending `BaseEntity` in `src/docDOC.Domain/Entities/Speciality.cs`
- [x] T025 [P] Create `RefreshToken` entity with `TokenHash` (never raw token) in `src/docDOC.Domain/Entities/RefreshToken.cs` (Satisfies: FR-012)
- [x] T026 [P] Create `Appointment` entity including `HangfireJobId` (string?, nullable) in `src/docDOC.Domain/Entities/Appointment.cs` (Satisfies: FR-007, FR-010)
- [x] T027 [P] Create `ChatRoom` entity with `IsActive` (bool, soft-close flag) and `UpdatedAt` in `src/docDOC.Domain/Entities/ChatRoom.cs` (Satisfies: FR-008, FR-014)
- [x] T028 [P] Create `Message` entity using `MessageStatus` enum — NOT a bool field in `src/docDOC.Domain/Entities/Message.cs` (Satisfies: FR-009)
- [x] T029 [P] Create `Notification` entity in `src/docDOC.Domain/Entities/Notification.cs` (Satisfies: FR-009)
- [x] T030 [P] Create `Review` entity in `src/docDOC.Domain/Entities/Review.cs` (Satisfies: FR-011)

---

### 1.3 — Repository Interfaces (Domain Layer)

- [x] T031 [P] Create `IRepository<T>` with `GetByIdAsync`, `AddAsync`, `Update`, `Delete` in `src/docDOC.Domain/Interfaces/IRepository.cs`
- [x] T032 [P] Create `IPatientRepository` extending `IRepository<Patient>` in `src/docDOC.Domain/Interfaces/IPatientRepository.cs`
- [x] T033 [P] Create `IDoctorRepository` with `GetNearbyAsync(lat, lon, radiusKm, specialityId?)` in `src/docDOC.Domain/Interfaces/IDoctorRepository.cs` (Satisfies: FR-006)
- [x] T034 [P] Create `IAppointmentRepository` with `IsSlotTakenAsync(doctorId, date, time)` and `GetByPairAsync(patientId, doctorId)` in `src/docDOC.Domain/Interfaces/IAppointmentRepository.cs` (Satisfies: FR-016)
- [x] T035 [P] Create `IChatRoomRepository` with `GetByPairAsync(patientId, doctorId)` and `HasQualifyingAppointmentAsync(patientId, doctorId)` in `src/docDOC.Domain/Interfaces/IChatRoomRepository.cs` (Satisfies: FR-008)
- [x] T036 [P] Create `IMessageRepository` with `GetPagedAsync(roomId, cursor, limit)` in `src/docDOC.Domain/Interfaces/IMessageRepository.cs`
- [x] T037 [P] Create `INotificationRepository` extending `IRepository<Notification>` in `src/docDOC.Domain/Interfaces/INotificationRepository.cs`
- [x] T038 [P] Create `IReviewRepository` extending `IRepository<Review>` in `src/docDOC.Domain/Interfaces/IReviewRepository.cs`
- [x] T039 [P] Create `IRefreshTokenRepository` with `GetByHashAsync(hash)` and `RevokeAllForUserAsync(userId)` in `src/docDOC.Domain/Interfaces/IRefreshTokenRepository.cs` (Satisfies: FR-012)
- [x] T040 [P] Create `IUnitOfWork` exposing all repositories, `SaveChangesAsync`, and `BeginTransactionAsync` in `src/docDOC.Domain/Interfaces/IUnitOfWork.cs` (Satisfies: FR-016)

---

### 1.4 — Infrastructure: DbContext & Configurations

- [x] T041 Create `ApplicationDbContext` with all DbSets in `src/docDOC.Infrastructure/Persistence/ApplicationDbContext.cs`
- [x] T042 Enable SQL Server Spatial via `UseSqlServer(conn, o => o.UseNetTopologySuite())` in `src/docDOC.Infrastructure/Persistence/ApplicationDbContext.cs` (Satisfies: FR-006)
- [x] T043 [P] Create `PatientConfiguration` with email unique index in `src/docDOC.Infrastructure/Persistence/Configurations/PatientConfiguration.cs` (Satisfies: FR-002)
- [x] T044 [P] Create `SpecialityConfiguration` with `HasData` seed using stable hardcoded integer IDs for General, Neurologic, Pediatric, Radiology in `src/docDOC.Infrastructure/Persistence/Configurations/SpecialityConfiguration.cs`
- [x] T045 [P] Create `DoctorConfiguration` with GIST index on `Location` and partial index on `IsOnline = true` in `src/docDOC.Infrastructure/Persistence/Configurations/DoctorConfiguration.cs` (Satisfies: FR-005, FR-006)
- [x] T046 [P] Create `AppointmentConfiguration` with partial unique index `(DoctorId, Date, Time) WHERE Status != 'Cancelled'` in `src/docDOC.Infrastructure/Persistence/Configurations/AppointmentConfiguration.cs` (Satisfies: FR-016)
- [x] T047 [P] Create `ChatRoomConfiguration` with unique index on `(PatientId, DoctorId)` in `src/docDOC.Infrastructure/Persistence/Configurations/ChatRoomConfiguration.cs`
- [x] T048 [P] Create `MessageConfiguration` with composite index on `(ChatRoomId, SentAt)` in `src/docDOC.Infrastructure/Persistence/Configurations/MessageConfiguration.cs`
- [x] T049 [P] Create `NotificationConfiguration` with partial index `(UserId, UserType) WHERE IsRead = false` in `src/docDOC.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs`
- [x] T050 [P] Create `ReviewConfiguration` with unique index on `AppointmentId` in `src/docDOC.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs`
- [x] T051 [P] Create `RefreshTokenConfiguration` with unique index on `TokenHash` in `src/docDOC.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- [x] T052 Generate `InitialCreate` migration via `dotnet ef migrations add InitialCreate --project src/docDOC.Infrastructure --startup-project src/docDOC.Api`
- [x] T053 Apply migration and verify Specialities seed data via `dotnet ef database update --project src/docDOC.Infrastructure --startup-project src/docDOC.Api`

---

### 1.5 — Infrastructure: Unit of Work & Repositories

- [x] T054 [P] Implement `BaseRepository<T>` implementing `IRepository<T>` in `src/docDOC.Infrastructure/Persistence/Repositories/BaseRepository.cs`
- [x] T055 Implement `UnitOfWork` implementing `IUnitOfWork`, wrapping `ApplicationDbContext` in `src/docDOC.Infrastructure/Persistence/UnitOfWork.cs` (Satisfies: FR-016)
- [x] T056 [P] Implement `PatientRepository` extending `BaseRepository<Patient>` in `src/docDOC.Infrastructure/Persistence/Repositories/PatientRepository.cs`
- [x] T057 [P] Implement `SpecialityRepository` extending `BaseRepository<Speciality>` in `src/docDOC.Infrastructure/Persistence/Repositories/SpecialityRepository.cs`
- [x] T058 [P] Implement `RefreshTokenRepository` with `GetByHashAsync` and `RevokeAllForUserAsync` in `src/docDOC.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs` (Satisfies: FR-012)
- [x] T059 [P] Implement `DoctorRepository.GetNearbyAsync` using NetTopologySuite spatial query in `src/docDOC.Infrastructure/Persistence/Repositories/DoctorRepository.cs` (Satisfies: FR-006) — Redis fast path added in Phase 2
- [x] T060 [P] Implement `AppointmentRepository.IsSlotTakenAsync` querying non-Cancelled appointments at given doctorId/date/time in `src/docDOC.Infrastructure/Persistence/Repositories/AppointmentRepository.cs` (Satisfies: FR-016)
- [x] T061 [P] Implement `ChatRoomRepository.HasQualifyingAppointmentAsync` querying for Confirmed or Completed appointments between pair in `src/docDOC.Infrastructure/Persistence/Repositories/ChatRoomRepository.cs` (Satisfies: FR-008)
- [x] T062 [P] Implement `MessageRepository.GetPagedAsync` with cursor-based pagination in `src/docDOC.Infrastructure/Persistence/Repositories/MessageRepository.cs`
- [x] T063 [P] Implement `NotificationRepository` extending `BaseRepository<Notification>` in `src/docDOC.Infrastructure/Persistence/Repositories/NotificationRepository.cs`
- [x] T064 [P] Implement `ReviewRepository` extending `BaseRepository<Review>` in `src/docDOC.Infrastructure/Persistence/Repositories/ReviewRepository.cs`

---

### 1.6 — Infrastructure: Services

- [x] T065 [P] Implement `IRedisService` interface in `src/docDOC.Application/Interfaces/IRedisService.cs`
- [x] T066 [P] Implement `IJwtService` interface in `src/docDOC.Application/Interfaces/IJwtService.cs`
- [x] T067 [P] Implement `ICurrentUserService` interface in `src/docDOC.Application/Interfaces/ICurrentUserService.cs`
- [x] T068 [P] Implement `RedisService` wrapping `IConnectionMultiplexer` in `src/docDOC.Infrastructure/Services/RedisService.cs` (Satisfies: FR-004)
- [x] T069 [P] Implement `JwtService` generating access tokens with claims `sub`, `email`, `role`, `userType`, `jti` in `src/docDOC.Infrastructure/Services/JwtService.cs` (Satisfies: FR-003)
- [x] T070 [P] Implement `CurrentUserService` reading `sub`, `role`, `userType`, `jti` from `IHttpContextAccessor` in `src/docDOC.Infrastructure/Services/CurrentUserService.cs` (Satisfies: FR-003)

---

### 1.7 — Application: MediatR Pipeline Behaviors

- [x] T071 [P] Implement `ValidationBehavior<TRequest, TResponse>` running FluentValidation before every handler in `src/docDOC.Application/Behaviors/ValidationBehavior.cs`
- [x] T072 Implement `UnitOfWorkBehavior<TRequest, TResponse>` wrapping Command handlers in `SaveChangesAsync` — Queries must bypass this behavior entirely in `src/docDOC.Application/Behaviors/UnitOfWorkBehavior.cs` (Satisfies: FR-016) — **Foundational: all subsequent commands depend on this**
- [x] T073 Register behaviors in DI in correct pipeline order (Validation first, then UoW) in `src/docDOC.Application/DependencyInjection.cs`

---

### 1.8 — Application: Auth Commands

- [x] T074 [P] Implement `RegisterUserCommand` + Handler — reads `role`, routes insert to `Patients` or `Doctors` table via UoW, returns `201` with profile, does NOT return tokens in `src/docDOC.Application/Features/Auth/Commands/RegisterUserCommand.cs` (Satisfies: FR-001, FR-002)
- [x] T075 [P] Implement `LoginUserCommand` + Handler — validates credentials against correct table using `role` field, generates JWT via `IJwtService`, stores SHA-256 hashed refresh token via `IRefreshTokenRepository` in `src/docDOC.Application/Features/Auth/Commands/LoginUserCommand.cs` (Satisfies: FR-001, FR-003)
- [x] T076 [P] Implement `RefreshTokenCommand` + Handler — hashes incoming token, looks up by hash, checks `IsRevoked` and `ExpiresAt`, revokes old token, issues new access + refresh token pair in `src/docDOC.Application/Features/Auth/Commands/RefreshTokenCommand.cs` (Satisfies: FR-012)
- [x] T077 [P] Implement `LogoutUserCommand` + Handler — writes `blacklist:{jti}` to Redis as a String key with TTL matching JWT remaining expiry, calls `RevokeAllForUserAsync` in `src/docDOC.Application/Features/Auth/Commands/LogoutUserCommand.cs` (Satisfies: FR-004)
- [x] T078 [P] Add FluentValidation validators for `RegisterUserCommand` (required fields, password strength, valid role) in `src/docDOC.Application/Features/Auth/Validators/RegisterUserCommandValidator.cs`
- [x] T079 [P] Add FluentValidation validators for `LoginUserCommand` (required email, password, role) in `src/docDOC.Application/Features/Auth/Validators/LoginUserCommandValidator.cs`

---

### 1.9 — API: Auth Layer

- [x] T080 Implement `JwtRedisBlacklistMiddleware` checking `EXISTS blacklist:{jti}` on every request, rejecting with `401` if present — must be registered BEFORE controller execution in `src/docDOC.Api/Middleware/JwtRedisBlacklistMiddleware.cs` (Satisfies: FR-004, SC-004)
- [x] T081 Implement `ExceptionHandlingMiddleware` mapping `NotFoundException → 404`, `ForbiddenException → 403`, `ConflictException → 409`, `DomainException → 400` in `src/docDOC.Api/Middleware/ExceptionHandlingMiddleware.cs`
- [x] T082 Scaffold `AuthController` with four endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh-token`, `POST /api/auth/logout` in `src/docDOC.Api/Controllers/AuthController.cs`
- [x] T083 Register all services in `src/docDOC.Api/Program.cs`: middleware, MediatR, FluentValidation, AutoMapper, EF Core with SQL Server Spatial, JWT bearer, Redis
- [x] T084 Configure `[Authorize(Roles = "Patient")]` and `[Authorize(Roles = "Doctor")]` on test endpoints to verify `role` claim enforcement in `src/docDOC.Api/Controllers/AuthController.cs` (Satisfies: FR-003)

---

## Phase 2: Doctor Discovery & Appointments (P1)

**Definition of Done:**

- Doctor can update location and appear in nearby results (SC-001).
- Patient can view available slots and book successfully.
- Concurrent booking of same slot results in `409 Conflict` with 0% double-booking confirmed (SC-003).
- Hangfire reminder job is scheduled and `HangfireJobId` persisted on the appointment record.
- Hangfire job deleted on cancellation and ChatRoom soft-closed where applicable.

---

### 2.0 — NuGet Packages

- [x] T085 Install Hangfire packages in `src/docDOC.Infrastructure/docDOC.Infrastructure.csproj`:

  ```
  dotnet add src/docDOC.Infrastructure package Hangfire
  dotnet add src/docDOC.Infrastructure package Hangfire.SqlServer
  dotnet add src/docDOC.Infrastructure package Hangfire.AspNetCore
  ```

---

### 2.1 — Hangfire Setup (MUST precede BookAppointmentCommand)

- [x] T086 Register Hangfire with SQL Server storage in `src/docDOC.Api/Program.cs`: `services.AddHangfire(config => config.UseSqlServerStorage(connString))`
- [x] T087 Add `services.AddHangfireServer()` in `src/docDOC.Api/Program.cs`
- [x] T088 Map Hangfire dashboard in `src/docDOC.Api/Program.cs`: `app.UseHangfireDashboard("/hangfire")`
- [x] T089 [P] Create `AppointmentReminderJob` with `Execute(int appointmentId)` method — sends notification via `INotificationDispatcher` in `src/docDOC.Infrastructure/Services/HangfireJobs/AppointmentReminderJob.cs` (Satisfies: FR-010)
- [x] T090 [P] Create `RefreshTokenCleanupJob` deleting expired and revoked tokens older than 7 days, registered as recurring daily job in `src/docDOC.Infrastructure/Services/HangfireJobs/RefreshTokenCleanupJob.cs`

---

### 2.2 — Doctor Location & Discovery

- [x] T091 Implement `UpdateDoctorLocationCommand` + Handler — updates `Doctor.Location` (SQL Server Point) and `Doctor.IsOnline` in SQL Server via UoW, then calls `GEOADD doctors:geo {lon} {lat} {doctorId}` in Redis. If `isOnline = false`, calls `ZREM doctors:geo {doctorId}` to remove from GEO set in `src/docDOC.Application/Features/Doctors/Commands/UpdateDoctorLocationCommand.cs` (Satisfies: FR-005)
- [x] T092 Implement `GetNearbyDoctorsQuery` + Handler with two-tier strategy in `src/docDOC.Application/Features/Doctors/Queries/GetNearbyDoctorsQuery.cs` (Satisfies: FR-006, SC-001):
  - Fast path: `GEOSEARCH doctors:geo FROMLONLAT {lon} {lat} BYRADIUS {radius} km ASC COUNT 20`
  - If Redis returns results: fetch details checking `doctor:cache:{doctorId}` (10min TTL) first, then DB
  - Fallback: SQL Server `ST_DWithin` + `ST_Distance` if Redis returns empty or fails
  - Cache SQL Server results in `nearby:cache:{lat}:{lon}:{radius}` with 2min TTL
  - Apply `specialityId` filter at DB level (SQL Server path) or post-fetch (Redis path)
- [x] T093 Implement `GetSpecialitiesQuery` + Handler — return from `specialities:cache` Redis key (1hr TTL), fallback to DB, cache result in `src/docDOC.Application/Features/Specialities/Queries/GetSpecialitiesQuery.cs`

---

### 2.3 — Doctor Availability

- [x] T094 Implement `GetDoctorAvailabilityQuery` + Handler in `src/docDOC.Application/Features/Doctors/Queries/GetDoctorAvailabilityQuery.cs` (Satisfies: FR-007):
  - Reject if `date` is Sunday — return `400 Bad Request`
  - Generate all 30-min slots from 08:00 to 16:30 inclusive (last slot ends at 17:00)
  - Call `IAppointmentRepository.IsSlotTakenAsync` for each slot
  - Return only free slots
  - Do NOT cache — availability must always be real-time

---

### 2.4 — Appointment Booking

- [x] T095 Implement `BookAppointmentCommand` + Handler — booking with two-layer double-booking prevention in `src/docDOC.Application/Features/Appointments/Commands/BookAppointmentCommand.cs` (Satisfies: FR-007, FR-016):
  - Validate date is Mon–Sat, time is on :00 or :30, time is within 08:00–16:30
  - Open explicit UoW transaction via `IUnitOfWork.BeginTransactionAsync()`
  - Call `IsSlotTakenAsync(doctorId, date, time)` inside the transaction — Layer 2
  - If taken: throw `ConflictException` → `409 Conflict`
  - Create and insert `Appointment` with `Status = Pending`
  - Call `SaveChangesAsync()` — Layer 1 partial unique index fires here as safety net
  - Catch `SqlException` with `SqlState == "23505"` → throw `ConflictException`
  - Commit transaction
- [x] T096 Implement `HangfireJobId` two-step write-back inside `BookAppointmentCommand` handler (Satisfies: FR-010, FR-013):
  - Step 1: Schedule job → `var jobId = BackgroundJob.Schedule<AppointmentReminderJob>(j => j.Execute(appointment.Id), triggerTime)`
  - Step 2: Set `appointment.HangfireJobId = jobId` → call `SaveChangesAsync()` to persist the job ID

---

### 2.5 — Appointment Status Management

- [x] T097 Implement `UpdateAppointmentStatusCommand` + Handler — enforce full role-to-transition matrix in `src/docDOC.Application/Features/Appointments/Commands/UpdateAppointmentStatusCommand.cs` (Satisfies: FR-013, FR-015):
  - `Pending → Confirmed`: Doctor only — notify Patient
  - `Confirmed → Completed`: Doctor only — notify Patient, Patient becomes eligible for review
  - `Pending → Cancelled`: Doctor or Patient — delete `appointment.HangfireJobId` via `BackgroundJob.Delete()`, notify other party
  - `Confirmed → Cancelled`: Doctor or Patient — same side effects as above
  - `Completed → anything`: Nobody — throw `ConflictException` → `409 Conflict`
  - `Cancelled → anything`: Nobody — throw `ConflictException` → `409 Conflict`
  - Any other transition: throw `DomainException` → `400 Bad Request`
- [x] T098 Implement ChatRoom soft-close logic inside `UpdateAppointmentStatusCommand` on cancellation in `src/docDOC.Application/Features/Appointments/Commands/UpdateAppointmentStatusCommand.cs` (Satisfies: FR-014):
  - Call `IChatRoomRepository.GetByPairAsync(patientId, doctorId)`
  - If room exists: call `HasQualifyingAppointmentAsync(patientId, doctorId)`
  - If no qualifying appointment remains: set `ChatRoom.IsActive = false`

---

### 2.6 — Controllers & Scaffolding

- [x] T099 [P] Scaffold `SpecialitiesController` with `GET /api/specialities` (public, no auth) in `src/docDOC.Api/Controllers/SpecialitiesController.cs`
- [x] T100 [P] Scaffold `DoctorsController` with `GET /api/doctors/nearby`, `GET /api/doctors/{id}/availability`, `PUT /api/doctors/location` in `src/docDOC.Api/Controllers/DoctorsController.cs`
- [x] T101 [P] Scaffold `AppointmentsController` with `POST /api/appointments`, `GET /api/appointments/mine`, `PATCH /api/appointments/{id}/status` in `src/docDOC.Api/Controllers/AppointmentsController.cs`

---

## Phase 3: Real-Time Chat & Notifications (P2)

**Definition of Done:**

- Chat room creation rejected unconditionally without a qualifying `Confirmed` or `Completed` appointment (SC-005).
- 100% of messages are persisted to SQL Server before SignalR delivery (SC-002).
- Typing indicators and read receipts functional.
- Offline users receive all missed notifications by calling `GET /api/notifications` on app launch — not on SignalR reconnect.
- All notification write endpoints functional.

---

### 3.0 — NuGet Packages

- [x] T102 Install SignalR Redis backplane package in `src/docDOC.Infrastructure/docDOC.Infrastructure.csproj`:

  ```
  dotnet add src/docDOC.Infrastructure package Microsoft.AspNetCore.SignalR.StackExchangeRedis
  ```

---

### 3.1 — SignalR Setup

- [x] T103 Register SignalR with Redis backplane in `src/docDOC.Api/Program.cs`:

  ```csharp
  builder.Services.AddSignalR()
      .AddStackExchangeRedis(redisConnString, opts => {
          opts.Configuration.ChannelPrefix = "docDOC";
      });
  ```

- [x] T104 Map hub endpoints in `src/docDOC.Api/Program.cs`: `app.MapHub<ChatHub>("/hubs/chat")` and `app.MapHub<NotificationHub>("/hubs/notifications")`

---

### 3.2 — Notification Dispatcher

- [x] T105 [P] Create `INotificationDispatcher` interface with `SendAsync(userId, userType, eventType, content, referenceId)` in `src/docDOC.Application/Interfaces/INotificationDispatcher.cs`
- [x] T106 Implement `NotificationDispatcher` in `src/docDOC.Infrastructure/Services/NotificationDispatcher.cs` (Satisfies: FR-009):
  - Step 1: Persist `Notification` to SQL Server via UoW — always, regardless of SignalR state
  - Step 2: Check `EXISTS online:{userId}` in Redis
  - Step 3: If online, push `OnNotification` event via `NotificationHub` to user's SignalR group

---

### 3.3 — Notification Hub

- [x] T107 Implement `NotificationHub` in `src/docDOC.Infrastructure/Hubs/NotificationHub.cs`:
  - `OnConnectedAsync`: set `online:{userId}` as Redis String with 30s TTL
  - `OnDisconnectedAsync`: delete `online:{userId}` key
  - Heartbeat: client pings every 20s, server refreshes TTL on ping

---

### 3.4 — Chat Commands & Queries

- [x] T108 Implement `CreateOrGetChatRoomCommand` + Handler in `src/docDOC.Application/Features/Chat/Commands/CreateOrGetChatRoomCommand.cs` (Satisfies: FR-008, SC-005):
  - Call `IChatRoomRepository.HasQualifyingAppointmentAsync(patientId, doctorId)`
  - If no qualifying appointment: throw `ForbiddenException` → `403 Forbidden`
  - If existing room with `IsActive = false` and qualifying appointment now found: set `IsActive = true`, return room
  - If room exists and `IsActive = true`: return existing room
  - If no room exists: create new `ChatRoom` with `IsActive = true`
- [x] T109 Implement `SendMessageCommand` + Handler in `src/docDOC.Application/Features/Chat/Commands/SendMessageCommand.cs` (Satisfies: FR-009, FR-014):
  - Verify `ChatRoom.IsActive = true` — if false: throw `ForbiddenException` → `403 Forbidden`
  - Verify sender (`sub` claim) is `PatientId` or `DoctorId` on the room
  - Persist `Message` to SQL Server with `Status = Sent` — before any SignalR call
  - Update `ChatRoom.UpdatedAt`
  - Increment `unread:{roomId}:{recipientId}` Redis counter
  - Dispatch `new_message` via `INotificationDispatcher`
  - Return persisted `MessageDto` — SignalR broadcast handled by `ChatHub`
- [x] T110 Implement `MarkMessagesReadCommand` + Handler in `src/docDOC.Application/Features/Chat/Commands/MarkMessagesReadCommand.cs` (Satisfies: FR-009):
  - Update all messages in room where sender is not current user to `Status = Read`
  - Delete `unread:{roomId}:{userId}` Redis key
  - Broadcast `OnMessagesRead` event via `ChatHub` to sender
- [x] T111 [P] Implement `GetMyChatRoomsQuery` + Handler returning rooms sorted by `UpdatedAt DESC` in `src/docDOC.Application/Features/Chat/Queries/GetMyChatRoomsQuery.cs`
- [x] T112 [P] Implement `GetChatMessagesQuery` + Handler using `IMessageRepository.GetPagedAsync` for cursor-based pagination in `src/docDOC.Application/Features/Chat/Queries/GetChatMessagesQuery.cs`

---

### 3.5 — Chat Hub

- [x] T113 Implement `ChatHub` in `src/docDOC.Infrastructure/Hubs/ChatHub.cs` — all methods require JWT auth:
  - `JoinRoom(roomId)` → `Groups.AddToGroupAsync(Context.ConnectionId, roomId)`
  - `LeaveRoom(roomId)` → `Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId)`
  - `SendMessage(roomId, content)` → dispatch `SendMessageCommand` via MediatR, then broadcast `OnReceiveMessage` to group
  - `Typing(roomId)` → set `typing:{roomId}:{userId}` Redis String with 3s TTL, broadcast `OnTypingIndicator` to group
  - `MarkRead(roomId)` → dispatch `MarkMessagesReadCommand` via MediatR

---

### 3.6 — Notification Commands & Queries

- [x] T114 [P] Implement `GetNotificationsQuery` + Handler supporting `unreadOnly` filter and pagination in `src/docDOC.Application/Features/Notifications/Queries/GetNotificationsQuery.cs`
- [x] T115 [P] Implement `MarkNotificationReadCommand` + Handler — validates notification `UserId` matches current user's `sub` claim in `src/docDOC.Application/Features/Notifications/Commands/MarkNotificationReadCommand.cs`
- [x] T116 [P] Implement `MarkAllNotificationsReadCommand` + Handler — bulk sets `IsRead = true` for all notifications belonging to current user in `src/docDOC.Application/Features/Notifications/Commands/MarkAllNotificationsReadCommand.cs`

---

### 3.7 — Wire Notification Dispatcher into Phase 2 Commands

- [x] T117 Update `UpdateAppointmentStatusCommand` (Phase 2) to use `INotificationDispatcher` for all status transition notifications in `src/docDOC.Application/Features/Appointments/Commands/UpdateAppointmentStatusCommand.cs`

---

### 3.8 — Controllers

- [x] T118 [P] Scaffold `ChatController` with `POST /api/chats`, `GET /api/chats`, `GET /api/chats/{id}/messages`, `PUT /api/chats/{id}/messages/read` in `src/docDOC.Api/Controllers/ChatController.cs`
- [x] T119 [P] Scaffold `NotificationsController` with `GET /api/notifications`, `PUT /api/notifications/{id}/read`, `PUT /api/notifications/read-all` in `src/docDOC.Api/Controllers/NotificationsController.cs`

---

## Phase 4: Reviews & Testing (P3)

**Definition of Done:**

- [x] Review subsystem operational and bound to `Completed` appointments only.
- [x] All integration tests pass against local SQL Server and Redis instances.
- [x] Race condition test passes confirming 0% double-booking (SC-003).
- [x] All 5 success criteria have at least one passing test.
- [x] Swagger UI fully documents all endpoints with JWT auth scheme.

---

### 4.0 — NuGet Packages

- [x] T120 Install test packages in `tests/docDOC.Tests/docDOC.Tests.csproj`:

  ```
  dotnet add tests/docDOC.Tests package xUnit
  dotnet add tests/docDOC.Tests package Moq
  dotnet add tests/docDOC.Tests package FluentAssertions
  dotnet add tests/docDOC.Tests package Microsoft.AspNetCore.Mvc.Testing
  ```

---

### 4.1 — Reviews

- [x] T121 Implement `SubmitReviewCommand` + Handler in `src/docDOC.Application/Features/Reviews/Commands/SubmitReviewCommand.cs` (Satisfies: FR-011):
  - Verify appointment exists and `Status = Completed` — throw `DomainException` → `400` if not
  - Verify `appointment.PatientId` matches current user's `sub` claim — throw `ForbiddenException` → `403` if not
  - Verify no review exists for this appointment — throw `ConflictException` → `409` if duplicate
  - Persist `Review`
  - Recalculate: `newAvg = ((AverageRating * TotalReviews) + newRating) / (TotalReviews + 1)`
  - Increment `Doctor.TotalReviews`
  - Update `Doctor.AverageRating`
  - Invalidate `doctor:cache:{doctorId}` in Redis
- [x] T122 Add `SubmitReviewCommandValidator` — rating must be 1–5, comment max 1000 chars in `src/docDOC.Application/Features/Reviews/Validators/SubmitReviewCommandValidator.cs`
- [x] T123 [P] Scaffold `ReviewsController` with `POST /api/reviews` in `src/docDOC.Api/Controllers/ReviewsController.cs`

---

### 4.2 — Integration Tests

**Auth Tests** (FR-001, FR-002, FR-003, FR-004, FR-012):

- [x] T124 [P] `Register_Patient_CreatesRecordInPatientsTable_NotDoctorsTable` in `tests/docDOC.Tests/AuthTests.cs` → FR-002
- [x] T125 [P] `Register_Doctor_CreatesRecordInDoctorsTable_NotPatientsTable` in `tests/docDOC.Tests/AuthTests.cs` → FR-002
- [x] T126 [P] `Login_WithValidCredentials_ReturnsJwtWithCorrectClaims` — verify `role`, `userType`, `jti` all present in `tests/docDOC.Tests/AuthTests.cs` → FR-003
- [x] T127 [P] `Login_WithoutRoleField_Returns400` in `tests/docDOC.Tests/AuthTests.cs` → FR-001
- [x] T128 [P] `Logout_BlacklistsToken_SubsequentRequestReturns401` — verify Redis key exists after logout in `tests/docDOC.Tests/AuthTests.cs` → FR-004, SC-004
- [x] T129 [P] `RefreshToken_WithValidToken_ReturnsNewTokenPairAndRevokesOld` in `tests/docDOC.Tests/AuthTests.cs` → FR-012
- [x] T130 [P] `RefreshToken_WithRevokedToken_Returns401` in `tests/docDOC.Tests/AuthTests.cs` → FR-012
- [x] T131 [P] `RefreshToken_WithExpiredToken_Returns401` in `tests/docDOC.Tests/AuthTests.cs` → FR-012

**Discovery Tests** (FR-005, FR-006, SC-001):

- [x] T132 [P] `UpdateLocation_PersistsToBothSqlServerAndRedisGeo` — verify `Doctor.Location` in DB and `ZSCORE doctors:geo` in Redis in `tests/docDOC.Tests/DiscoveryTests.cs` → FR-005
- [x] T133 [P] `GetNearby_UsesRedisGeoFastPath_ReturnsResultsUnder100ms` — measure response time in `tests/docDOC.Tests/DiscoveryTests.cs` → FR-006, SC-001
- [x] T134 [P] `GetNearby_WhenRedisEmpty_FallsBackToSqlServer` — flush Redis GEO key before request, verify results still returned in `tests/docDOC.Tests/DiscoveryTests.cs` → FR-006
- [x] T135 [P] `GetNearby_WithSpecialityFilter_ReturnsOnlyMatchingDoctors` — seed two doctors with different specialities, filter by one in `tests/docDOC.Tests/DiscoveryTests.cs` → FR-006

**Appointment Tests** (FR-007, FR-010, FR-013, FR-014, FR-015, FR-016, SC-003):

- [x] T136 [P] `BookAppointment_ValidSlot_Returns201AndSchedulesHangfireJob` in `tests/docDOC.Tests/AppointmentTests.cs` → FR-010
- [x] T137 [P] `BookAppointment_HangfireJobId_PersistedOnAppointment` — verify `HangfireJobId` is non-null after booking in `tests/docDOC.Tests/AppointmentTests.cs` → FR-010, FR-013
- [x] T138 `BookAppointment_ConcurrentRequests_OnlyOneSucceeds_OtherGets409` — use `Parallel.ForEach` or `Task.WhenAll` to fire two simultaneous booking requests for the same slot against SQL Server in `tests/docDOC.Tests/AppointmentTests.cs` → FR-016, SC-003
- [x] T139 [P] `BookAppointment_SundayDate_Returns400` in `tests/docDOC.Tests/AppointmentTests.cs` → FR-007
- [x] T140 [P] `BookAppointment_TimeOutsideWorkingHours_Returns400` in `tests/docDOC.Tests/AppointmentTests.cs` → FR-007
- [x] T141 [P] `CancelAppointment_DeletesHangfireJob` — verify `HangfireJobId` is null and job absent from Hangfire storage in `tests/docDOC.Tests/AppointmentTests.cs` → FR-013
- [x] T142 [P] `CancelAppointment_NoOtherQualifyingAppointment_SoftClosesChatRoom` — verify `ChatRoom.IsActive = false` in `tests/docDOC.Tests/AppointmentTests.cs` → FR-014
- [x] T143 [P] `CancelAppointment_OtherQualifyingAppointmentExists_ChatRoomRemainsOpen` — verify `ChatRoom.IsActive = true` in `tests/docDOC.Tests/AppointmentTests.cs` → FR-014
- [x] T144 [P] `CompleteAppointment_ThenCancelAttempt_Returns409` in `tests/docDOC.Tests/AppointmentTests.cs` → FR-015

**Chat Tests** (FR-008, FR-009, SC-002, SC-005):

- [x] T145 [P] `CreateChatRoom_WithoutConfirmedAppointment_Returns403` in `tests/docDOC.Tests/ChatTests.cs` → FR-008, SC-005
- [x] T146 [P] `CreateChatRoom_WithConfirmedAppointment_Returns201` in `tests/docDOC.Tests/ChatTests.cs` → FR-008
- [x] T147 [P] `SendMessage_PersistedToSqlServer_BeforeSignalRDelivery` — verify `Message` row exists in DB before SignalR broadcast fires in `tests/docDOC.Tests/ChatTests.cs` → FR-009, SC-002
- [x] T148 [P] `SendMessage_ToSoftClosedRoom_Returns403` in `tests/docDOC.Tests/ChatTests.cs` → FR-014
- [x] T149 [P] `MarkMessagesRead_UpdatesStatusAndClearsRedisCounter` — verify `MessageStatus = Read` in DB and `unread:{roomId}:{userId}` deleted from Redis in `tests/docDOC.Tests/ChatTests.cs` → FR-009

**Review Tests** (FR-011):

- [x] T150 [P] `SubmitReview_ForCompletedAppointment_RecalculatesDoctorRating` — verify `AverageRating` and `TotalReviews` updated on Doctor in `tests/docDOC.Tests/ReviewTests.cs` → FR-011
- [x] T151 [P] `SubmitReview_ForPendingAppointment_Returns400` in `tests/docDOC.Tests/ReviewTests.cs` → FR-011
- [x] T152 [P] `SubmitReview_Duplicate_Returns409` in `tests/docDOC.Tests/ReviewTests.cs` → FR-011

---

### 4.3 — API Documentation

- [x] T153 Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to `src/docDOC.Api/docDOC.Api.csproj`
- [x] T154 Register XML documentation file path in `SwaggerGen` and configure JWT bearer auth scheme in `src/docDOC.Api/Program.cs`:

  ```csharp
  c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "docDOC.Api.xml"));
  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
  ```

- [x] T155 [P] Add `[ProducesResponseType]` attributes and XML `<summary>` comments to `AuthController` in `src/docDOC.Api/Controllers/AuthController.cs`
- [x] T156 [P] Add `[ProducesResponseType]` attributes and XML `<summary>` comments to `DoctorsController` in `src/docDOC.Api/Controllers/DoctorsController.cs`
- [x] T157 [P] Add `[ProducesResponseType]` attributes and XML `<summary>` comments to `AppointmentsController` in `src/docDOC.Api/Controllers/AppointmentsController.cs`
- [x] T158 [P] Add `[ProducesResponseType]` attributes and XML `<summary>` comments to `ChatController` in `src/docDOC.Api/Controllers/ChatController.cs`
- [x] T159 [P] Add `[ProducesResponseType]` attributes and XML `<summary>` comments to `NotificationsController` in `src/docDOC.Api/Controllers/NotificationsController.cs`
- [x] T160 [P] Add `[ProducesResponseType]` attributes and XML `<summary>` comments to `ReviewsController` in `src/docDOC.Api/Controllers/ReviewsController.cs`
- [x] T161 [P] Add `[ProducesResponseType]` attributes and XML `<summary>` comments to `SpecialitiesController` in `src/docDOC.Api/Controllers/SpecialitiesController.cs`

---

## Task Summary

| Phase | Tasks | Parallelizable |
|-------|-------|---------------|
| Phase 1 | T001–T084 | 58 of 84 |
| Phase 2 | T085–T101 | 8 of 17 |
| Phase 3 | T102–T119 | 10 of 18 |
| Phase 4 | T120–T161 | 34 of 42 |
| **Total** | **161** | **110 parallelizable** |
