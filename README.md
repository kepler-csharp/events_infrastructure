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

# Default Roles

| Role | Email | Password |
|---|---|---|
| Admin | admin@tickets.com | Admin1234! |
| Customer | customer@tickets.com | Customer1234! |
| Scanner | scanner@tickets.com | Scanner1234! |
| Receptionist | receptionist@tickets.com | Recept1234! |

---

# Project Structure

```txt
ApiGeneral/
│
├── AuthApi/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Entities/
│   ├── Services/
│   ├── Seed/
│
├── Program.cs
├── appsettings.json
├── ApiGeneral.csproj
```

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

# appsettings.json

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

---

# Entity Framework Migrations

Create migrations:

```bash
dotnet ef migrations add InitialCreate
```

Apply migrations:

```bash
dotnet ef database update
```

---

# Run the API

```bash
dotnet run
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

# Role Registration Endpoints

## Register Admin

```http
POST /api/auth/register-admin
```

Authorization:

```txt
Admin only
```

---

## Register Customer

```http
POST /api/auth/register-customer
```

Authorization:

```txt
Public
```

---

## Register Scanner

```http
POST /api/auth/register-scanner
```

Authorization:

```txt
Admin & Scanner only
```

---

## Register Receptionist

```http
POST /api/auth/register-receptionist
```

Authorization:

```txt
Admin & Receptionist only
```

---

# Registration Request Body

```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

---

# Role Assignment

Each endpoint automatically assigns the corresponding ASP.NET Identity role.

Roles are stored in:

- AspNetRoles
- AspNetUserRoles
- AspNetUsers
---

# License

This project is intended for educational and development purposes.