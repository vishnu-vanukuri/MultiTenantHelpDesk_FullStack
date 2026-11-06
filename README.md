# Multi-Tenant Help Desk & Issue Tracking System

A runnable full-stack starter aligned with the DeltaX Associate Product Engineer JD:
- C# / ASP.NET Core 8 Web API
- Entity Framework Core + SQL Server
- Vue 3 + Vite
- JWT authentication and role-based authorization
- Tenant isolation (every ticket query is scoped to the authenticated tenant)
- REST APIs, validation, structured logging, Docker Compose

## Prerequisites
- .NET 8 SDK
- Node.js 20+
- Docker Desktop (recommended) or SQL Server 2022

## 1. Start SQL Server
From the project root:
```bash
docker compose up -d sqlserver
```
Wait until SQL Server is healthy. The connection string is configured in `backend/appsettings.json`.
Default local SQL credentials are for development only. Change them before deployment.

## 2. Start the API
```bash
cd backend
dotnet restore
dotnet run
```
The API listens at `http://localhost:5080` and `https://localhost:7080` (depending on launch profile).
Swagger: `http://localhost:5080/swagger`.

The database is created automatically on startup for this learning/demo project. For production, replace
`EnsureCreated` with EF Core migrations and a controlled deployment migration step.

## 3. Start Vue
In another terminal:
```bash
cd frontend
npm install
npm run dev
```
Open `http://localhost:5173`.

## 4. Try it
1. Register a company and its first admin account.
2. Sign in.
3. Create tickets and update their status/priority.
4. Register a second company in a private/incognito browser. Its tickets are isolated from the first company.

## API endpoints
| Method | Endpoint | Auth | Purpose |
|---|---|---:|---|
| POST | `/api/auth/register` | No | Create tenant + first admin |
| POST | `/api/auth/login` | No | Login and receive JWT |
| GET | `/api/tickets` | Yes | List current tenant tickets |
| GET | `/api/tickets/{id}` | Yes | Get one current-tenant ticket |
| POST | `/api/tickets` | Yes | Create ticket |
| PUT | `/api/tickets/{id}` | Yes | Update ticket |
| DELETE | `/api/tickets/{id}` | Admin | Delete ticket |
| GET | `/api/dashboard/summary` | Yes | Tenant dashboard counts |

All authenticated ticket operations derive `TenantId` from the signed JWT, never from a client-supplied tenant ID.

## Example registration payload
```json
{
  "companyName": "Acme Support",
  "fullName": "Admin User",
  "email": "admin@acme.test",
  "password": "Passw0rd!"
}
```

## Example ticket payload
```json
{
  "title": "Unable to access the portal",
  "description": "The sign-in page returns an error.",
  "priority": "High",
  "status": "Open",
  "assignedTo": "Support Agent"
}
```

## Engineering practices
- Keep controllers thin; move business rules into services as the app grows.
- Add xUnit unit/integration tests before production use.
- Use BDD scenarios for acceptance criteria (examples in `docs/BDD_SCENARIOS.md`).
- Use GitHub Actions CI workflow in `.github/workflows/ci.yml`.
- Add Serilog/ELK shipping in deployment; this starter writes structured console logs and documents the ELK integration path.
- For production multi-tenancy, consider separate databases or SQL Server Row-Level Security in addition to application-level tenant filters.
- Never use the sample JWT key or SQL password in production. Store secrets in environment variables / a secret manager.

