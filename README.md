# TicketSystem

A full-stack support ticket management system built with:
- **Backend:** .NET 10 Web API · EF Core · SQL Server · JWT auth
- **Frontend:** Vue 3 · TypeScript · Vite · PrimeVue · Tailwind CSS v4

---

## Prerequisites

| Tool | Minimum Version |
|------|----------------|
| .NET SDK | 10.0 |
| Node.js | 20+ |
| Docker Desktop | Any recent version |
| Git | Any |

---

## Quick Start

### 1. Clone the repo

```bash
git clone <repo-url>
cd ticket-test
```

### 2. Start SQL Server (Docker)

```bash
docker compose up -d
```

SQL Server will be available at `localhost:1433`  
SA password: `TicketSystem_Dev123!`

---

## Backend Setup

### Windows (PowerShell)

```powershell
cd backend
dotnet restore
dotnet ef database update --project src/TicketSystem.API
dotnet run --project src/TicketSystem.API
```

### Linux / macOS

```bash
cd backend
dotnet restore
dotnet ef database update --project src/TicketSystem.API
dotnet run --project src/TicketSystem.API
```

The API will start at `http://localhost:5000` (or as configured in `launchSettings.json`).

> **First run:** EF Core will apply migrations automatically. Check `appsettings.Development.json` to confirm your connection string matches the Docker SA password.

---

## Frontend Setup

### Windows (PowerShell)

```powershell
cd frontend
npm install
npm run dev
```

### Linux / macOS

```bash
cd frontend
npm install
npm run dev
```

The frontend dev server starts at `http://localhost:5173`.  
API requests to `/api/*` are proxied to `http://localhost:5000`.

---

## Build for Production

```bash
# Backend
cd backend
dotnet publish -c Release -o ./publish

# Frontend
cd frontend
npm run build
# Output: frontend/dist/
```

---

## Running Tests

### Backend (xUnit)

```bash
cd backend
dotnet test
```

### Frontend (Vitest)

```bash
cd frontend
npm run test        # (if configured)
```

---

## Project Structure

```
ticket-test/
├── backend/
│   └── src/
│       └── TicketSystem.API/     # .NET 10 Web API
│           ├── Controllers/
│           ├── Models/
│           ├── Services/
│           ├── Data/              # EF Core DbContext + Migrations
│           └── appsettings.json
├── frontend/
│   └── src/
│       ├── api/                   # Axios API layer
│       ├── router/                # Vue Router
│       ├── stores/                # Pinia stores
│       └── views/                 # Vue page components
├── docker-compose.yml             # SQL Server for local dev
└── README.md
```

---

## Environment Variables

### Backend (`appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TicketSystem;User Id=sa;Password=TicketSystem_Dev123!;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "TicketSystem",
    "Audience": "TicketSystem"
  }
}
```

### Frontend (`.env.local`)

```env
VITE_API_BASE_URL=http://localhost:5000
```

---

## Default Credentials

After running migrations, seed data may include a default admin account.  
Check the `DataSeeder` or migration files for initial credentials.

---

## License

MIT
