# 🏥 docDOC - Telemedicine Backend

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoft-sql-server)](https://www.microsoft.com/en-us/sql-server)
[![Redis](https://img.shields.io/badge/Redis-7.0-DC382D?logo=redis)](https://redis.io/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

**docDOC** is a high-performance, scalable telemedicine backend designed to connect patients with doctors in real-time. Built on a solid **Clean Architecture** foundation, it leverages modern technologies to ensure reliability, speed, and a premium user experience.

---

## 🚀 Key Features

- **🔒 Unified Authentication**: Single endpoint registration for Patients and Doctors with JWT-based auth and silent token refresh.
- **📍 Nearby Doctor Discovery**: Blazing fast search using **Redis GEO** with **SQL Server Spatial** as a reliable fallback.
- **📅 Smart Appointments**: 30-minute slot booking with double-booking prevention and automatic reminders via **Hangfire**.
- **💬 Real-Time Chat**: Direct communication between patients and doctors powered by **SignalR**, with full message persistence.
- **🔔 Persistent Notifications**: Real-time and historical notifications ensuring users never miss an update.
- **⭐ Trust System**: Post-consultation reviews with dynamic rating recalculation.

---

## 🛠️ Tech Stack

- **Core**: .NET 10.0 (ASP.NET Core API)
- **Persistence**: SQL Server 2022+ (Entity Framework Core)
- **Caching & GEO**: Redis 7+
- **Background Jobs**: Hangfire
- **Real-time Messaging**: SignalR
- **Validation**: FluentValidation
- **Documentation**: Swagger / OpenAPI

---

## 📁 Project Structure

The project follows the **Clean Architecture** pattern to ensure separation of concerns and maintainability:

```text
docDOC/
├── src/
│   ├── docDOC.Api/           # Entry point, Controllers, and API Configuration
│   ├── docDOC.Application/   # Use cases, Handlers, DTOs, and Business Logic
│   ├── docDOC.Domain/        # Entities, Value Objects, and Domain Exceptions
│   └── docDOC.Infrastructure/# Persistence, External Services, and Repositories
├── specs/                   # Detailed documentation and specifications
└── docDOC.sln               # Solution File
```

---

## 🏁 Quick Start

### 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2022+](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Redis 7+](https://redis.io/download)

### ⚙️ Setup

1. **Clone the repository**:

    ```bash
    git clone https://github.com/your-repo/docDOC.git
    cd docDOC
    ```

2. **Configure Environment**:
    Update `src/docDOC.Api/appsettings.Development.json` with your connection strings:

    ```json
    {
      "ConnectionStrings": {
        "SqlServer": "your-sql-connection-string",
        "Redis": "localhost:6379"
      }
    }
    ```

3. **Update Database**:

    ```bash
    dotnet ef database update --project src/docDOC.Infrastructure --startup-project src/docDOC.Api
    ```

4. **Run the API**:

    ```bash
    dotnet run --project src/docDOC.Api
    ```

Visit `https://localhost:5001/swagger` to explore the API.

---

## 🛡️ Best Practices & Quality

- **Atomic Transactions**: Unit of Work pattern for reliable data updates.
- **Performance**: Redis GEO fast-path for location-based queries.
- **Reliability**: Double-booking prevention enforced at both Database and Application layers.
- **Security**: JWT Blacklisting via Redis on logout for immediate token invalidation.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---


