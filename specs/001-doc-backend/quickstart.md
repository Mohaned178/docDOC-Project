# Quickstart: docDOC Backend

**Feature**: 001-doc-backend
**Date**: 2026-02-28

This guide provides instructions on how to run the DOC Telemedicine backend infrastructure locally.

---

## Prerequisites

1. **.NET  SDK** — `dotnet --version` should be 10.0
2. **SQL Server 2022+** — running locally or remotely
3. **Redis 7+** — running locally or remotely
4. **EF Core CLI**:

   ```bash
   dotnet tool install --global dotnet-ef
   ```

---

## Infrastructure Setup

You need to have **SQL Server** and **Redis** running. The same Redis instance serves both as the SignalR backplane and the GEO/cache store.

> **Note**: Hangfire runs in-process inside the API. It uses the same SQL Server connection and does not require its own service.

---

## Configuration

Add the following to `src/docDOC.Api/appsettings.Development.json`. Update these credentials to match your local SQL Server and Redis instances.

```json
{
  "ConnectionStrings": {
    "SqlServer": "Host=localhost;Port=1433;Database=docdocdb;User Id=sa;Password=docpassword",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "your-very-long-secret-key-at-least-32-characters",
    "Issuer": "docDOC",
    "Audience": "docDOC-clients",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Hangfire": {
    "DashboardPath": "/hangfire"
  }
}
```

> **Warning**: Never commit real secrets to source control. The JWT secret above is for local development only. Use environment variables or a secrets manager in production.

---

## Running the Application

**1. Restore dependencies**:

```bash
dotnet restore
```

**2. Apply migrations and seed data**:

The `docDOC.Infrastructure` project contains `ApplicationDbContext`. The migration will create all tables, indexes, and seed the `Specialities` table with the four initial entries.

```bash
dotnet ef database update \
  --project src/docDOC.Infrastructure \
  --startup-project src/docDOC.Api
```

**3. Start the API**:

```bash
dotnet run --project src/docDOC.Api
```

Swagger UI will be available at: `https://localhost:5001/swagger`
Hangfire dashboard will be available at: `https://localhost:5001/hangfire`

---

## Verification Checklist

Run through these checks in order after first setup. Each step maps to a constitutional requirement or functional requirement.

### ✅ 1. SQL Server Spatial Active

SQL Server has built-in support for spatial data types (Geography). You can verify the connection and version:

```bash
sqlcmd -S localhost -U sa -P docpassword -C -Q "SELECT @@VERSION"
```

Expected output: Microsoft SQL Server 2022...

---

### ✅ 2. Schema & Indexes Created

Connect and verify the tables and critical indexes exist:

```bash
sqlcmd -S localhost -U sa -P docpassword -C -d docdocdb
```

```sql
-- Verify all tables exist
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
GO

-- Verify critical indexes
SELECT name, filter_definition 
FROM sys.indexes 
WHERE object_id IN (OBJECT_ID('Appointments'), OBJECT_ID('Doctors'), OBJECT_ID('Notifications'));
GO
```

---

### ✅ 3. Specialities Seed Data Present

```sql
SELECT * FROM Specialities;
GO
```

Expected output — four rows:

| Id | Name | IconCode |
|----|------|----------|
| 1 | General | general |
| 2 | Neurologic | neurologic |
| 3 | Pediatric | pediatric |
| 4 | Radiology | radiology |

If this table is empty, the migration did not apply the seed data. Re-run `dotnet ef database update`.

---

### ✅ 4. Auth Flow

Use Swagger at `https://localhost:5001/swagger` or any HTTP client.

**Step 1 — Register a Patient**:

```http
POST /api/auth/register
{
  "email": "patient@test.com",
  "password": "StrongPassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Patient",
  "dateOfBirth": "1990-05-15",
  "gender": "Male"
}
```

Expected: `201 Created` with profile object. No tokens returned at this step.

**Step 2 — Login to receive tokens**:

```http
POST /api/auth/login
{
  "email": "patient@test.com",
  "password": "StrongPassword123!",
  "role": "Patient"
}
```

Expected: `200 OK` with `accessToken`, `refreshToken`, and `user` object. Copy the `accessToken`.

**Step 3 — Logout and verify blacklist** (SC-004):

```http
POST /api/auth/logout
Authorization: Bearer <accessToken>
```

Expected: `204 No Content`.

Now call any authenticated endpoint with the same token:

```http
GET /api/notifications
Authorization: Bearer <accessToken>
```

Expected: `401 Unauthorized` — the `jti` is blacklisted in Redis.

---

### ✅ 5. Redis Connectivity

Verify Redis is receiving keys after the logout step above:

```bash
redis-cli KEYS "blacklist:*"
```

Expected: one key matching `blacklist:{jti}` with a TTL equal to the remaining JWT expiry.

```bash
redis-cli TTL "blacklist:<jti-value>"
```

Expected: a positive integer (seconds remaining). If `-1` the TTL was not set — check `LogoutUserCommand`.

---

### ✅ 6. Hangfire Dashboard

Visit `https://localhost:5001/hangfire`.

Expected: Hangfire dashboard loads showing zero enqueued and zero failed jobs. After booking an appointment, a scheduled job should appear here with a trigger time of 1 day before the appointment date.

---

## Common Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| `Connection refused on 1433` | SQL Server not running | Start your SQL Server instance |
| `Migration fails on geometry column` | Missing `UseNetTopologySuite()` | Ensure `UseSqlServer(conn, o => o.UseNetTopologySuite())` in `ApplicationDbContext` |
| `Specialities table is empty` | Seed not applied | Run `dotnet ef database update` again |
| `401 on all requests after restart` | Redis flushed — blacklist cleared | Expected in dev. In production Redis must persist data. |
| `Hangfire dashboard not loading` | Dashboard not mapped | Verify `app.UseHangfireDashboard("/hangfire")` in `Program.cs` |
| `SignalR connection fails` | Redis backplane not configured | Verify `AddStackExchangeRedis` is called in `AddSignalR()` chain |
