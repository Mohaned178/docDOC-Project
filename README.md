# 🏥 docDOC - Telemedicine Backend

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![FastEndpoints](https://img.shields.io/badge/FastEndpoints-8.0-FF69B4?logo=dotnet)](https://fast-endpoints.com/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoft-sql-server)](https://www.microsoft.com/en-us/sql-server)
[![Redis](https://img.shields.io/badge/Redis-7.0-DC382D?logo=redis)](https://redis.io/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

**docDOC** is a high-performance, scalable telemedicine backend designed to connect patients with doctors in real-time. Built on a solid **Clean Architecture** foundation, it leverages **FastEndpoints** for a modern, vertical-slice inspired API development experience that is both developer-friendly and blazing fast.

---

## 🚀 Key Features

- **⚡ FastEndpoints Powered**: Modern REPR (Request-Endpoint-Response) pattern for clean, maintainable, and highly performant API endpoints.
- **🔒 Secure Authentication**: Unified registration for Patients and Doctors with JWT-based auth, Refresh Tokens, and **Redis-backed token blacklisting** for secure logouts.
- **📍 Geospatial Search**: Advanced "Nearby Doctor" discovery using **NetTopologySuite** and SQL Server Spatial indexing.
- **📅 Smart Appointments**: Advanced slot booking with strict business rules (30-min increments, working hours validation) and automatic reminders via **Hangfire**.
- **💬 Real-Time Chat**: Direct, persistent communication between patients and doctors powered by **SignalR**.
- **🔔 Notification System**: Real-time push notifications and historical tracking for all critical user events.
- **⭐ Trust & Reviews**: Interactive feedback system for patients to rate and review medical consultations.

---

## 🛠️ Tech Stack

- **Framework**: .NET 10.0 (ASP.NET Core)
- **API Engine**: [FastEndpoints](https://fast-endpoints.com/) 8.0+
- **Mediator Pattern**: MediatR (for decoupling Application logic)
- **Database**: SQL Server 2022 (Entity Framework Core)
- **Spatial Data**: NetTopologySuite
- **Caching & Identity Security**: Redis
- **Background Tasks**: Hangfire
- **Real-time**: SignalR Hubs
- **Security**: JWT Bearer + BCrypt.Net

---

## 🏁 Installation & Setup

### 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Docker)
- [Redis](https://redis.io/download) (for caching and security)

### ⚙️ Step-by-Step Instructions

1.  **Clone the Repository**
    ```bash
    git clone https://github.com/Mohaned178/docDOC-Project.git
    cd docDOC
    ```

2.  **Configure Environment**
    Update `src/docDOC.Api/appsettings.json` with your connection strings:
    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Database=docDOC;Trusted_Connection=True;TrustServerCertificate=True;",
        "Redis": "localhost:6379"
      },
      "JwtSettings": {
        "Secret": "YourSuperSecretKeyNotLessThen32Chars!",
        "Issuer": "docDOC",
        "Audience": "docDOC"
      }
    }
    ```

3.  **Run Migrations**
    ```bash
    dotnet ef database update --project src/docDOC.Infrastructure --startup-project src/docDOC.Api
    ```

4.  **Start the Project**
    ```bash
    dotnet run --project src/docDOC.Api
    ```
    The API will be available at: `https://localhost:7173`

---

## 📂 Project Structure

The solution follows **Clean Architecture** principles to ensure the core business logic remains independent of external frameworks:

*   **`docDOC.Api`**: The entry point. Contains **FastEndpoints**, SignalR Hubs, and API-specific middleware.
*   **`docDOC.Application`**: The "Heart" of the system. Houses MediatR Commands/Queries, Validators, and DTOs.
*   **`docDOC.Domain`**: Pure domain layer. Contains Entities, Enums, and Repository Interfaces.
*   **`docDOC.Infrastructure`**: Implementation layer. Handles EF Core Contexts, Redis Services, and Hangfire Jobs.

---

## 🌐 API Endpoints

### 🔐 Authentication (`/api/auth`)
*   `POST /api/auth/register` - Create a new account (Patient or Doctor).
*   `POST /api/auth/login` - Obtain JWT access and refresh tokens.
*   `POST /api/auth/refresh-token` - Renew an expired access token using a refresh token.
*   `POST /api/auth/logout` - Securely sign out and blacklist the current token.

### 👨‍⚕️ Doctors (`/api/doctors`)
*   `GET /api/doctors/nearby` - Find doctors within a specific radius based on GPS coordinates.
*   `GET /api/doctors/{id}/availability?date=yyyy-MM-dd` - View available time slots for a doctor.
*   `PUT /api/doctors/location` - (Doctor only) Update current clinic/hospital coordinates.

### 📅 Appointments (`/api/appointments`)
*   `POST /api/appointments` - Book a new consultation (Patient only).
*   `GET /api/appointments/mine` - Retrieve your history of appointments.
*   `PATCH /api/appointments/{id}/status` - Confirm, complete, or cancel an appointment.

### 💬 Real-Time Chat (`/api/chat`)
*   `POST /api/chat` - Initialize or retrieve a chat room for an active appointment.
*   `GET /api/chat` - List all active conversations.
*   `GET /api/chat/{id}/messages` - Load message history (supports cursor-based pagination).
*   `PUT /api/chat/{id}/messages/read` - Mark all messages in a room as seen.

### 🔔 Notifications (`/api/notifications`)
*   `GET /api/notifications` - Get all alerts/reminders.
*   `PUT /api/notifications/{id}/read` - Mark a single notification as read.
*   `PUT /api/notifications/read-all` - Clear all pending notifications.

### 📑 Specialities (`/api/specialities`)
*   `GET /api/specialities` - Fetch the list of medical specialities (General, Cardiology, etc.).

### ⭐ Reviews (`/api/reviews`)
*   `POST /api/reviews` - (Patient only) Rate and review a doctor after a completed consultation.

---

## 🧪 Testing with Postman

To test the API easily, you can import the **Swagger JSON** directly into Postman:

1.  Run the project.
2.  Go to `https://localhost:7173/swagger/v1/swagger.json`.
3.  In Postman, click **Import** and paste the URL.
4.  **Important**: Turn off **"SSL certificate verification"** in Postman Settings -> General to allow `localhost` HTTPS calls.

### Postman Collection
Alternatively, you can create a collection by using the Swagger definition or your own manual endpoint configurations as detailed in the API section.

---

## 🤝 Contribution

1.  Fork the project.
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the Branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

---

## ✉️ Contact

Project Lead - [Mohaned178](https://github.com/Mohaned178)
Project Link: [https://github.com/Mohaned178/docDOC-Project](https://github.com/Mohaned178/docDOC-Project)
