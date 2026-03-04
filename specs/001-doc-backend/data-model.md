# Data Model: docDOC

**Feature**: 001-doc-backend
**Date**: 2026-02-28
**Version**: 1.1.0 (Reviewed & Fixed)

---

## Changes from v1.0.0

- Added `HangfireJobId` to `Appointment` — required for FR-013 (job deletion on cancel)
- Added `TotalReviews` to `Doctor` — required for FR-011 (efficient rating recalculation)
- Changed `Message.IsRead` (`bool`) → `Message.Status` (`MessageStatus` enum: `Sent/Delivered/Read`) — aligns with spec
- Fixed `Notification` index to use a partial index on `IsRead = false` — more efficient for the primary query
- Restored `Speciality` as a separate table — hardcoded enum was a regression from agreed design
- Removed `Doctor.Speciality` enum field, replaced with `Doctor.SpecialityId` FK

---

## Entities & EF Core Configuration

### 1. `Patient`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| Email | `string` | MaxLength: 100, Unique |
| PasswordHash | `string` | MaxLength: 200 |
| FirstName | `string` | MaxLength: 50 |
| LastName | `string` | MaxLength: 50 |
| DateOfBirth | `DateOnly` | |
| Gender | `enum` | Male, Female, Other |
| CreatedAt | `DateTimeOffset` | UTC |

**EF Configuration**:

```csharp
builder.ToTable("Patients");
builder.HasKey(p => p.Id);
builder.Property(p => p.Email).HasMaxLength(100).IsRequired();
builder.Property(p => p.PasswordHash).HasMaxLength(200).IsRequired();
builder.Property(p => p.FirstName).HasMaxLength(50).IsRequired();
builder.Property(p => p.LastName).HasMaxLength(50).IsRequired();
builder.Property(p => p.Gender).HasConversion<string>();
builder.HasIndex(p => p.Email).IsUnique();
```

---

### 2. `Speciality`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| Name | `string` | MaxLength: 100, Unique |
| IconCode | `string` | MaxLength: 50 |

**EF Configuration**:

```csharp
builder.ToTable("Specialities");
builder.HasKey(s => s.Id);
builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
builder.Property(s => s.IconCode).HasMaxLength(50);
builder.HasIndex(s => s.Name).IsUnique();
```

**Seed Data**:

```csharp
builder.HasData(
    new Speciality { Id = 1, Name = "General",    IconCode = "general"    },
    new Speciality { Id = 2, Name = "Neurologic", IconCode = "neurologic" },
    new Speciality { Id = 3, Name = "Pediatric",  IconCode = "pediatric"  },
    new Speciality { Id = 4, Name = "Radiology",  IconCode = "radiology"  }
);
```

> Speciality is a separate table — not an enum — so new specialities can be added via migration data seeding without code changes.

---

### 3. `Doctor`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| Email | `string` | MaxLength: 100, Unique |
| PasswordHash | `string` | MaxLength: 200 |
| FirstName | `string` | MaxLength: 50 |
| LastName | `string` | MaxLength: 50 |
| SpecialityId | `int` | FK → Specialities |
| Hospital | `string` | MaxLength: 100 |
| AverageRating | `decimal(3,2)` | Default: 0.00 |
| TotalReviews | `int` | Default: 0 |
| IsOnline | `bool` | Default: false |
| Location | `Point` | GEOMETRY(Point, 4326), nullable |
| CreatedAt | `DateTimeOffset` | UTC |

**EF Configuration**:

```csharp
builder.ToTable("Doctors");
builder.HasKey(d => d.Id);
builder.Property(d => d.Email).HasMaxLength(100).IsRequired();
builder.Property(d => d.PasswordHash).HasMaxLength(200).IsRequired();
builder.Property(d => d.FirstName).HasMaxLength(50).IsRequired();
builder.Property(d => d.LastName).HasMaxLength(50).IsRequired();
builder.Property(d => d.Hospital).HasMaxLength(100);
builder.Property(d => d.AverageRating).HasPrecision(3, 2).HasDefaultValue(0.00m);
builder.Property(d => d.TotalReviews).HasDefaultValue(0);
builder.Property(d => d.IsOnline).HasDefaultValue(false);
builder.Property(d => d.Location).HasColumnType("geometry(Point, 4326)");

builder.HasOne(d => d.Speciality)
       .WithMany()
       .HasForeignKey(d => d.SpecialityId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(d => d.Email).IsUnique();
builder.HasIndex(d => d.SpecialityId);
builder.HasIndex(d => d.IsOnline).HasFilter("\"IsOnline\" = true");
builder.HasIndex(d => d.Location).HasMethod("GIST");
```

> `TotalReviews` is stored alongside `AverageRating` to avoid a `COUNT(*)` query on every rating recalculation. New average = `((AverageRating * TotalReviews) + newRating) / (TotalReviews + 1)`.

---

### 4. `Appointment`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| PatientId | `int` | FK → Patients |
| DoctorId | `int` | FK → Doctors |
| Date | `DateOnly` | |
| Time | `TimeOnly` | Enforced at :00 or :30 only |
| Type | `enum` | InPerson, Online |
| Status | `enum` | Pending, Confirmed, Completed, Cancelled |
| HangfireJobId | `string?` | MaxLength: 100, nullable — stores Hangfire job ID for deletion on cancel |
| CreatedAt | `DateTimeOffset` | UTC |

**EF Configuration**:

```csharp
builder.ToTable("Appointments");
builder.HasKey(a => a.Id);
builder.Property(a => a.Type).HasConversion<string>().IsRequired();
builder.Property(a => a.Status).HasConversion<string>().IsRequired();
builder.Property(a => a.HangfireJobId).HasMaxLength(100);

builder.HasOne(a => a.Patient)
       .WithMany()
       .HasForeignKey(a => a.PatientId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasOne(a => a.Doctor)
       .WithMany()
       .HasForeignKey(a => a.DoctorId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(a => new { a.DoctorId, a.Date, a.Time })
       .HasFilter("\"Status\" != 'Cancelled'")
       .IsUnique();
builder.HasIndex(a => a.PatientId);
builder.HasIndex(a => a.DoctorId);
builder.HasIndex(a => new { a.PatientId, a.Status });
builder.HasIndex(a => new { a.DoctorId, a.Date, a.Status });
```

> `HangfireJobId` is populated after the Hangfire job is scheduled (post-booking). It is set to `null` after the job is deleted on cancellation.

---

### 5. `ChatRoom`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| PatientId | `int` | FK → Patients |
| DoctorId | `int` | FK → Doctors |
| IsActive | `bool` | Default: true — false = soft-closed |
| CreatedAt | `DateTimeOffset` | UTC |
| UpdatedAt | `DateTimeOffset` | UTC — updated on every new message |

**EF Configuration**:

```csharp
builder.ToTable("ChatRooms");
builder.HasKey(c => c.Id);
builder.Property(c => c.IsActive).HasDefaultValue(true);

builder.HasOne(c => c.Patient)
       .WithMany()
       .HasForeignKey(c => c.PatientId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasOne(c => c.Doctor)
       .WithMany()
       .HasForeignKey(c => c.DoctorId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(c => new { c.PatientId, c.DoctorId }).IsUnique();
builder.HasIndex(c => c.UpdatedAt);
```

> `IsActive = false` means the ChatRoom is soft-closed per FR-014. History is readable but new messages return `403 Forbidden`. Re-activation happens automatically if a new qualifying appointment is created between the same pair.

---

### 6. `Message`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| ChatRoomId | `int` | FK → ChatRooms |
| SenderId | `int` | Patient or Doctor Id |
| SenderType | `enum` | Patient, Doctor |
| Content | `string` | MaxLength: 2000 |
| Status | `enum` | Sent, Delivered, Read |
| SentAt | `DateTimeOffset` | UTC |

**EF Configuration**:

```csharp
builder.ToTable("Messages");
builder.HasKey(m => m.Id);
builder.Property(m => m.Content).HasMaxLength(2000).IsRequired();
builder.Property(m => m.SenderType).HasConversion<string>().IsRequired();
builder.Property(m => m.Status).HasConversion<string>().HasDefaultValue(MessageStatus.Sent);

builder.HasOne(m => m.ChatRoom)
       .WithMany()
       .HasForeignKey(m => m.ChatRoomId)
       .OnDelete(DeleteBehavior.Cascade);

builder.HasIndex(m => new { m.ChatRoomId, m.SentAt });
builder.HasIndex(m => new { m.ChatRoomId, m.Status })
       .HasFilter("\"Status\" != 'Read'");
```

> `Status` is a three-state enum: `Sent` (saved to DB), `Delivered` (SignalR confirmed delivery), `Read` (recipient explicitly read). A boolean `IsRead` was insufficient — it collapsed `Sent` and `Delivered` into the same state and lost delivery tracking.

---

### 7. `Notification`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| UserId | `int` | Patient or Doctor Id |
| UserType | `enum` | Patient, Doctor |
| EventType | `string` | MaxLength: 50 |
| Content | `string` | MaxLength: 500 |
| ReferenceId | `int?` | Nullable — associated AppointmentId or ChatRoomId |
| IsRead | `bool` | Default: false |
| CreatedAt | `DateTimeOffset` | UTC |

**Event Types**:

| EventType | Trigger | Recipient |
|-----------|---------|-----------|
| `appointment_confirmed` | Doctor confirms | Patient |
| `appointment_cancelled` | Either party cancels | Other party |
| `appointment_reminder` | Hangfire job (1 day before) | Patient |
| `appointment_completed` | Doctor marks complete | Patient |
| `new_message` | New chat message | Recipient |

**EF Configuration**:

```csharp
builder.ToTable("Notifications");
builder.HasKey(n => n.Id);
builder.Property(n => n.EventType).HasMaxLength(50).IsRequired();
builder.Property(n => n.Content).HasMaxLength(500).IsRequired();
builder.Property(n => n.UserType).HasConversion<string>().IsRequired();
builder.Property(n => n.IsRead).HasDefaultValue(false);

builder.HasIndex(n => new { n.UserId, n.UserType })
       .HasFilter("\"IsRead\" = false")
       .HasDatabaseName("IX_Notifications_Unread");

builder.HasIndex(n => new { n.UserId, n.CreatedAt });
```

---

### 8. `Review`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| AppointmentId | `int` | FK → Appointments, Unique (one review per appointment) |
| PatientId | `int` | FK → Patients |
| DoctorId | `int` | FK → Doctors |
| Rating | `int` | 1–5, validated at application layer |
| Comment | `string?` | MaxLength: 1000, nullable |
| CreatedAt | `DateTimeOffset` | UTC |

**EF Configuration**:

```csharp
builder.ToTable("Reviews");
builder.HasKey(r => r.Id);
builder.Property(r => r.Comment).HasMaxLength(1000);

builder.HasOne(r => r.Appointment)
       .WithOne()
       .HasForeignKey<Review>(r => r.AppointmentId)
       .OnDelete(DeleteBehavior.Cascade);

builder.HasOne(r => r.Patient)
       .WithMany()
       .HasForeignKey(r => r.PatientId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasOne(r => r.Doctor)
       .WithMany()
       .HasForeignKey(r => r.DoctorId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(r => r.AppointmentId).IsUnique();
builder.HasIndex(r => new { r.DoctorId, r.CreatedAt });
```

---

### 9. `RefreshToken`

**Fields**:

| Field | Type | Constraints |
|-------|------|-------------|
| Id | `int` | PK |
| UserId | `int` | Patient or Doctor Id |
| UserType | `enum` | Patient, Doctor |
| TokenHash | `string` | MaxLength: 256, Unique — never store raw token |
| ExpiresAt | `DateTimeOffset` | UTC, 7 days from creation |
| IsRevoked | `bool` | Default: false |
| CreatedAt | `DateTimeOffset` | UTC |

**EF Configuration**:

```csharp
builder.ToTable("RefreshTokens");
builder.HasKey(r => r.Id);
builder.Property(r => r.TokenHash).HasMaxLength(256).IsRequired();
builder.Property(r => r.UserType).HasConversion<string>().IsRequired();
builder.Property(r => r.IsRevoked).HasDefaultValue(false);

builder.HasIndex(r => r.TokenHash).IsUnique();
builder.HasIndex(r => new { r.UserId, r.UserType });
builder.HasIndex(r => r.ExpiresAt);
```

---

## Enums

```csharp
public enum Gender          { Male, Female, Other }
public enum AppointmentType { InPerson, Online }
public enum AppointmentStatus { Pending, Confirmed, Completed, Cancelled }
public enum MessageStatus   { Sent, Delivered, Read }
public enum SenderType      { Patient, Doctor }
public enum UserType        { Patient, Doctor }
```

---

## Database Index Summary

| Table | Index | Type | Purpose |
|-------|-------|------|---------|
| Patients | Email | Unique | Login lookup |
| Specialities | Name | Unique | Prevent duplicate names |
| Doctors | Email | Unique | Login lookup |
| Doctors | SpecialityId | B-Tree | Filter by speciality |
| Doctors | IsOnline = true | Partial | Nearby query filter |
| Doctors | Location | Spatial | SQL Server spatial geo-radius |
| Appointments | (DoctorId, Date, Time) WHERE Status != Cancelled | Partial Unique | Double-booking prevention |
| Appointments | PatientId | B-Tree | Patient history |
| Appointments | DoctorId | B-Tree | Doctor schedule |
| Appointments | (PatientId, Status) | Composite | Filtered patient list |
| Appointments | (DoctorId, Date, Status) | Composite | Available slot lookup |
| ChatRooms | (PatientId, DoctorId) | Unique | One room per pair |
| ChatRooms | UpdatedAt | B-Tree | Sort chat list |
| Messages | (ChatRoomId, SentAt) | Composite | Paginated history |
| Messages | (ChatRoomId, Status) WHERE Status != Read | Partial | Unread count |
| Notifications | (UserId, UserType) WHERE IsRead = false | Partial | Unread pull on app launch |
| Notifications | (UserId, CreatedAt) | Composite | Full history |
| Reviews | AppointmentId | Unique | One review per appointment |
| Reviews | (DoctorId, CreatedAt) | Composite | Doctor review list |
| RefreshTokens | TokenHash | Unique | Token validation |
| RefreshTokens | (UserId, UserType) | Composite | Revoke all on logout |
| RefreshTokens | ExpiresAt | B-Tree | Cleanup job |

---

**Version**: 1.1.0 | **Date**: 2026-02-28 | **Last Updated**: 2026-02-28
