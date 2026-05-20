# Tickets API General

Tickets API General is a secure authentication and authorization API built with ASP.NET Core, ASP.NET Identity, JWT, Redis, MySQL, and MinIO.

This project provides enterprise-grade authentication features including:

- JWT Authentication
- Refresh Tokens
- Logout with JWT Revocation
- Role-Based Authorization
- Password Recovery
- Password Change
- User Profile Photo Upload
- Redis Token Blacklist
- Swagger Documentation
- MinIO Object Storage
- ASP.NET Identity Integration

---

## Architecture
```
                        ┌──────────────────────┐
                        │ PostgreSQL 16        │
                        │ Redis                │
                        │ MinIO / S3           │
                        └─────────┬────────────┘
                                  │
                    ┌─────────────▼─────────────┐
                    │ API CENTRAL               │
                    │ ASP.NET Core 10 Web API  │
                    │ JWT + EF Core  │
                    └───────┬─────────┬─────────┘
                            │         │
        ┌───────────────────┘         └───────────────────┐
        │                                                 │
┌───────▼────────┐                            ┌──────────▼─────────┐
│ tickets.com    │                            │ admin.tickets.com  │
│ Laravel 11     │                            │ ASP.NET MVC        │
│ Blade/Inertia  │                            │ Razor + Chart.js   │
└───────┬────────┘                            └──────────┬─────────┘
        │                                                │
        └────────────────┬───────────────────────────────┘
                         │
               HTTP REST + JWT
                         │
        ┌────────────────▼────────────────┐
        │ tickets.reception.com           │
        │ Django 5 + HTMX                │
        │ Recepción rápida               │
        │ (Módulo simple únicamente)     │
        └────────────────┬───────────────┘
                         │
                         ▼
               ┌────────────────┐
               │ success.app    │
               │ PWA React      │
               │ QR Scanner     │
               └────────────────┘
```

![Architecture](architecture.png)

---

## Project Strucutre
```txt
ApiGeneral/
│
├── AuthApi/
│   ├── Controllers/
│   │   ├── AuthController.cs         ← Login, logout, register, photo upload, refresh
│   │   └── DomainControllers.cs      ← Venues, Events, Showtimes, Seats,
│   │                                     Orders, Scanner, Admin
│   ├── Data/
│   │   └── AppDbContext.cs           ← Identity + domain tables
│   ├── DTOs/
│   │   └── Dtos.cs                   ← Request/response DTOs
│   ├── Entities/
│   │   ├── ApplicationUser.cs        ← Extends IdentityUser
│   │   └── DomainEntities.cs         ← Venue, Event, Showtime, Seat,
│   │                                     Order, OrderItem, Payment, Ticket, TicketValidation
│   ├── Services/
│   │   ├── Interfaces/IServices.cs   ← Service contracts
│   │   ├── AuthControllerService.cs  ← Authentication logic
│   │   ├── JwtService.cs             ← JWT generation
│   │   ├── VenueAndEventService.cs   ← Venues and events
│   │   ├── ShowtimeAndSeatService.cs ← Showtimes and seat reservations
│   │   ├── OrderService.cs           ← Orders and payments
│   │   └── ScannerAndAdminService.cs ← QR validation and dashboard
│   └── Seed/
│       └── SeedData.cs               ← Users, venues, events, showtimes
│
├── Program.cs
├── appsettings.json
└── ApiGeneral.csproj
```

# Technologies

- .NET 10
- ASP.NET Core Web API
- ASP.NET Identity
- Entity Framework Core
- MySQL
- Redis
- JWT Authentication
- MinIO
- Swagger / OpenAPI
- Docker

---

# Features

## Authentication

- User Login
- User Logout
- JWT Access Tokens
- Refresh Tokens
- Token Revocation with Redis Blacklist

## Authorization

- Role-Based Access Control (RBAC)
- JWT Bearer Authentication
- Protected Endpoints

## User Management

- Change Password
- Forgot Password
- Reset Password
- Upload Profile Photo

## Storage

- MinIO Object Storage for profile photos
- MySQL relational database
- Redis cache for revoked JWT tokens

---

# Start Project

## Add `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=tickets_api_general;user=root;password=YOUR_PASSWORD",
    "Redis": "localhost:6379"
  },

  "Jwt": {
    "Key": "THIS_IS_A_VERY_LONG_SUPER_SECRET_KEY_FOR_JWT_AUTH_TICKETS_API_GENERAL_2026",
    "Issuer": "TicketsAPI",
    "Audience": "TicketsUsers",
    "ExpireMinutes": 15,
    "RefreshExpireDays": 7
  },

  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "Bucket": "user-photos"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },

  "AllowedHosts": "*"
}
```

or ``In Terminal``
```bash
cp appsettings.example.json appsettings.json
```

# Execute

---

## Entity Framework Migrations

Create migrations:

```bash
dotnet ef migrations add InitialCreate
```

Apply migrations:

```bash
dotnet ef database update
```

# Run the API

```bash
dotnet run
```

## If you have the same DB (like the project has) only execute

```bash
dotnet build
```

```bash
dotnet run
```

---

# Default Roles

| Role | Email | Password |
|---|---|---|
| Admin | admin@tickets.com | Admin1234! |
| Customer | customer@tickets.com | Customer1234! |
| Scanner | scanner@tickets.com | Scanner1234! |
| Receptionist | receptionist@tickets.com | Recept1234! |

---

# Requirements

Install the following tools before running the project:

- .NET SDK 10
- Docker Desktop
- MySQL Server
- Entity Framework CLI

---

# Install Entity Framework CLI

```bash
dotnet tool install --global dotnet-ef
```

---

# NuGet Packages

Install required packages:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Relational
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
dotnet add package StackExchange.Redis
dotnet add package Minio
dotnet add package BCrypt.Net-Next
```

---

# MySQL Configuration

Create a MySQL database:

```sql
CREATE DATABASE tickets_api_general;
```

---

# Redis Setup

Run Redis using Docker:

```bash
docker run -d \
  --name tickets-api-general-redis \
  -p 6379:6379 \
  -v redis_data:/data \
  redis:latest \
  redis-server --appendonly yes
```

Verify Redis:

```bash
docker ps
```

---

# MinIO Setup

Run MinIO using Docker:

```bash
docker run -d \
  --name tickets-api-general-minio \
  -p 9000:9000 \
  -p 9001:9001 \
  -v minio_data:/data \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  quay.io/minio/minio server /data --console-address ":9001"
```

---

# MinIO Console

Open:

```txt
http://localhost:9001
```

Credentials:

```txt
Username: minioadmin
Password: minioadmin
```

---

# Create Public Bucket

Create a bucket named:

```txt
user-photos
```

Make the bucket public:

```bash
docker exec -it tickets-api-general-minio mc alias set local http://localhost:9000 minioadmin minioadmin

docker exec -it tickets-api-general-minio mc anonymous set public local/user-photos
```

---

# Swagger Documentation

Open Swagger:

```txt
http://localhost:5201/swagger
```

---

# Authentication Flow

## Login

Endpoint:

```http
POST /api/auth/login
```

Request:

```json
{
  "email": "admin@tickets.com",
  "password": "Admin1234!"
}
```

Response:

```json
{
  "accessToken": "jwt_token",
  "refreshToken": "refresh_token"
}
```

---

# Logout

Endpoint:

```http
POST /api/auth/logout
```

Protected endpoint.

Behavior:

- JWT token is added to Redis blacklist
- Revoked tokens cannot access protected endpoints anymore

---

# Upload User Photo

Endpoint:

```http
POST /api/auth/upload-photo
```

Protected endpoint.

Content-Type:

```txt
multipart/form-data
```

Field:

```txt
file
```

Behavior:

- Uploads image to MinIO
- Stores public image URL in MySQL `PhotoUrl` field

---

# JWT Blacklist System

This project implements JWT revocation using Redis.

When a user logs out:

1. JWT token is stored in Redis blacklist
2. Every protected endpoint validates Redis blacklist
3. Revoked tokens return `401 Unauthorized`

This provides immediate logout invalidation.

---

# Security Features

- JWT Authentication
- Redis Token Revocation
- Role-Based Authorization
- Password Hashing
- ASP.NET Identity
- Refresh Tokens
- Secure Password Policies
- Protected Swagger Endpoints
- Public/Private MinIO Policies

---

# Recommended Production Improvements

- HTTPS
- Docker Compose
- Reverse Proxy (NGINX)
- Environment Variables
- Azure Key Vault / AWS Secrets Manager
- Email Service Provider
- Rate Limiting
- Refresh Token Rotation
- Audit Logging
- API Versioning
- Centralized Logging

---

## API Endpoints

### Auth (`/api/auth`)
| Method | Endpoint                          | Access             | Description                          |
| ------ | --------------------------------- | ------------------ | ------------------------------------ |
| POST   | `/api/auth/login`                 | Public             | Login → JWT + refresh token          |
| POST   | `/api/auth/logout`                | Authenticated      | Invalidates token in Redis blacklist |
| POST   | `/api/auth/refresh`               | Public             | Generate new access token            |
| POST   | `/api/auth/register-customer`     | Public             | Register customer                    |
| POST   | `/api/auth/register-admin`        | Admin              | Create admin                         |
| POST   | `/api/auth/register-scanner`      | Admin/Scanner      | Create scanner                       |
| POST   | `/api/auth/register-receptionist` | Admin/Receptionist | Create receptionist                  |
| POST   | `/api/auth/upload-photo`          | Authenticated      | Upload profile photo → MinIO         |
| POST   | `/api/auth/change-password`       | Authenticated      | Change password                      |
| POST   | `/api/auth/forgot-password`       | Public             | Request password reset               |
| POST   | `/api/auth/reset-password`        | Public             | Confirm password reset               |

Venues (`/api/venues`)
| Method | Endpoint           | Access | Description             |
| ------ | ------------------ | ------ | ----------------------- |
| GET    | `/api/venues`      | Public | List venues (paginated) |
| GET    | `/api/venues/{id}` | Public | Venue details           |
| POST   | `/api/venues`      | Admin  | Create venue            |
| DELETE | `/api/venues/{id}` | Admin  | Disable venue           |

Events (`/api/events`)
| Method | Endpoint           | Access | Description                                       |
| ------ | ------------------ | ------ | ------------------------------------------------- |
| GET    | `/api/events`      | Public | List events (`?isActive=true&page=1&pageSize=20`) |
| GET    | `/api/events/{id}` | Public | Event details                                     |
| POST   | `/api/events`      | Admin  | Create event                                      |
| PUT    | `/api/events/{id}` | Admin  | Update event                                      |
| DELETE | `/api/events/{id}` | Admin  | Disable event                                     |

Showtimes (`/api/showtimes`)
| Method | Endpoint                    | Access | Description                      |
| ------ | --------------------------- | ------ | -------------------------------- |
| GET    | `/api/showtimes`            | Public | List showtimes (`?eventId=1`)    |
| GET    | `/api/showtimes/{id}`       | Public | Showtime details                 |
| GET    | `/api/showtimes/{id}/seats` | Public | Seat map                         |
| POST   | `/api/showtimes`            | Admin  | Create showtime with seat layout |

Seats (`/api/seats`)
| Method | Endpoint             | Access        | Description                          |
| ------ | -------------------- | ------------- | ------------------------------------ |
| POST   | `/api/seats/reserve` | Authenticated | Reserve seats (Redis lock for 5 min) |
| POST   | `/api/seats/release` | Authenticated | Release reservation                  |

Orders (`/api/orders`)
| Method | Endpoint           | Access        | Description                      |
| ------ | ------------------ | ------------- | -------------------------------- |
| POST   | `/api/orders`      | Authenticated | Create order from reserved seats |
| POST   | `/api/orders/pay`  | Authenticated | Pay order → generates QR tickets |
| GET    | `/api/orders`      | Authenticated | User orders                      |
| GET    | `/api/orders/{id}` | Authenticated | Order details                    |

Scanner (`/api/scanner`)
| Method | Endpoint                | Access                     | Description                  |
| ------ | ----------------------- | -------------------------- | ---------------------------- |
| POST   | `/api/scanner/validate` | Admin/Scanner/Receptionist | Validate QR code at entrance |

Admin (`/api/admin`)
| Method | Endpoint               | Access | Description                                              |
| ------ | ---------------------- | ------ | -------------------------------------------------------- |
| GET    | `/api/admin/dashboard` | Admin  | Stats: sales, occupancy, revenue (Redis cache for 5 min) |

---

## Complete Purchase Flow

```
1. POST /api/auth/login              → JWT token
2. GET  /api/showtimes/{id}/seats    → View available seats
3. POST /api/seats/reserve           → Reserve seats (5 min Redis lock)
4. POST /api/orders                  → Create order
5. POST /api/orders/pay              → Pay → QR tickets generated
6. POST /api/scanner/validate        → Validate QR at entrance
```

# Role Assignment

Each endpoint automatically assigns the corresponding ASP.NET Identity role.

Roles are stored in:

- AspNetRoles
- AspNetUserRoles
- AspNetUsers
---

## Redis System

| Purpose         | Key                  | TTL                            |
| --------------- | -------------------- | ------------------------------ |
| JWT Blacklist   | `blacklist:{token}`  | Until token expiration         |
| Seat Lock       | `seat_lock:{seatId}` | 10 seconds (during processing) |
| Dashboard Cache | `admin:dashboard`    | 5 minutes                      |

---

# Docker Cleanup

Stop Redis:

```bash
docker stop tickets-api-general-redis
```

Remove Redis:

```bash
docker rm tickets-api-general-redis
```

Stop MinIO:

```bash
docker stop tickets-api-general-minio
```

Remove MinIO:

```bash
docker rm tickets-api-general-minio
```


# Docker Deployment

Build and run all services:

```bash
docker compose up --build
```

Run containers in background:

```bash
docker compose up -d
```

Stop containers:

```bash
docker compose down
```

---

# Services

| Service | URL |
|---|---|
| API | http://localhost:5201 |
| Swagger | http://localhost:5201/swagger |
| MinIO Console | http://localhost:9001 |
| MinIO API | http://localhost:9000 |
| MySQL | localhost:3306 |
| Redis | localhost:6379 |

---

## Seed Data Included

```txt
- 2 Venues: Cinépolis Premium and Teatro Metropolitano (Medellín)
- 2 Events: Inception (movie) and Rock en Vivo 2025 (concert)
- 2 Showtimes with pre-generated seats:
    - Inception: 60 seats (rows A-F, 10 seats per row, E/F = Premium)
    - Rock en Vivo: 100 seats (rows 1-5, 20 seats per row, row 1 = VIP)
```

## Example Request Body — Create Showtime with Seats

```txt
POST /api/showtimes
{
  "eventId": 1,
  "startTime": "2025-09-15T20:00:00Z",
  "basePrice": 25000,
  "seatLayout": [
    { "row": "A", "seatCount": 10, "type": 0 },
    { "row": "B", "seatCount": 10, "type": 0 },
    { "row": "C", "seatCount": 8,  "type": 1 }
  ]
}
```
## Example Request Body — Reserve Seats

```
POST /api/seats/reserve
{
  "showtimeId": 1,
  "seatIds": [1, 2, 3]
}
```

## Example Request Body — Validate QR

```
POST /api/scanner/validate
{
  "qrCode": "VFM6MToxNzM...",
  "deviceInfo": "Scanner-Device-001"
}
```
# License

This project is intended for educational and development purposes.