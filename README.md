# TicketSystem

A full-stack customer support ticket management system built with:

- **Backend:** .NET 10 Web API · EF Core 10 · SQL Server · JWT authentication · xUnit tests
- **Frontend:** Vue 3 · TypeScript · Vite · PrimeVue 4 · Tailwind CSS v4 · Pinia · Vitest

---

## Features

### Core Functionality
- **JWT Authentication** — register, login, change password, role-based access control
- **Three user roles:**
  - `Submitter` — create and view own tickets, add comments
  - `Agent` — view and manage assigned tickets, self-assign unassigned tickets
  - `Admin` — full access to all tickets, assign agents, manage users
- **Ticket management** — full CRUD with title, description, status, and priority
- **Ticket statuses** — Open → InProgress → Paused → Resolved / Unresolved
- **Ticket priorities** — Low, Medium, High, Critical
- **Assignment workflow** — agents self-assign; admins can assign to any agent
- **Comments** — add, edit (author only), delete (author or admin)
- **History tracking** — all field changes are recorded with old/new values and actor
- **Search & filter** — full-text search on title/description, filter by status
- **Pagination** — configurable page size with total count
- **Dashboard** — live stats (open, in-progress, resolved-today counts)

### Code Quality
- Role-scoped data visibility (submitters can't see other users' tickets)
- XML doc comments on all controller methods → Swagger documentation
- Swagger UI with JWT Bearer auth at `/swagger` (development only)
- Native .NET 10 OpenAPI endpoint at `/openapi/v1.json`
- Clean error handling throughout frontend API layer
- Accessibility: `aria-label` on icon-only buttons, form labels on all inputs
- Responsive mobile-first UI with skeleton loaders and toast notifications

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
SA password: `YourStrong!Passw0rd`

### 3. Start the backend

```bash
cd backend
dotnet restore
dotnet run --project src/TicketSystem.API
```

The API starts at `http://localhost:5000`.
On first run, EF Core migrations are applied and seed data is inserted automatically.

### 4. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

The frontend dev server starts at `http://localhost:5173`.

---

## Seed Credentials

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Password1!` | Admin |
| `agent` | `Password1!` | Agent |
| `agent2` | `Password1!` | Agent |
| `submitter` | `Password1!` | Submitter |

The seed data includes **8 realistic tickets** across all statuses and priorities, with comments, assignment history, and history entries.

---

## API Endpoint Reference

All endpoints are prefixed with `/api`. Authentication required unless noted.

### Auth (`/api/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | None | Register a new user |
| POST | `/auth/login` | None | Login, returns JWT |
| POST | `/auth/change-password` | Required | Change current user's password |

**Login / Register response:**
```json
{ "token": "...", "username": "alice", "role": "Agent", "userId": 1 }
```

### Tickets (`/api/tickets`)

| Method | Endpoint | Roles | Description |
|--------|----------|-------|-------------|
| GET | `/tickets` | All | List tickets (paginated, filterable) |
| GET | `/tickets/{id}` | All | Get ticket details (comments + history) |
| POST | `/tickets` | All | Create a ticket |
| PUT | `/tickets/{id}` | Agent, Admin | Update ticket fields |
| POST | `/tickets/{id}/assign` | Agent, Admin | Assign unassigned ticket |
| GET | `/tickets/stats` | All | Get dashboard statistics |
| POST | `/tickets/{id}/comments` | All | Add a comment |
| PUT | `/tickets/{id}/comments/{cid}` | Author | Edit a comment |
| DELETE | `/tickets/{id}/comments/{cid}` | Author, Admin | Delete a comment |

**Query parameters for `GET /tickets`:**
- `search` — full-text search (title + description)
- `status` — filter by status (Open, InProgress, Paused, Resolved, Unresolved)
- `assignedToMe` — boolean, returns only tickets assigned to current user
- `page` — 1-indexed page number (default: 1)
- `pageSize` — items per page, 1–100 (default: 20)

### Users (`/api/users`)

| Method | Endpoint | Roles | Description |
|--------|----------|-------|-------------|
| GET | `/users/agents` | Admin | List all agents (for assignment dropdown) |

---

## Running Tests

### Backend (xUnit)

```bash
cd backend
dotnet test
```

**Test coverage includes:**
- `TicketService` — full CRUD, comments, history, priority, pagination edge cases
- `AuthController` — register, login, change password (success and failure paths)
- `JwtService` — token generation and validation
- 55 tests total, all passing

### Frontend (Vitest)

```bash
cd frontend
npm run test          # run once
npm run test:watch    # watch mode
npm run test:coverage # coverage report
```

**Test coverage includes:**
- `utils/time.ts` — all `timeAgo` buckets + `formatDate`
- `stores/auth.ts` — login, logout, role computed properties
- `LoginView.vue` — renders form, validation, Sign In button
- `AppNav.vue` — renders nav, displays username and role
- 34 tests total, all passing

---

## Swagger UI

When running in development mode, Swagger UI is available at:

```
http://localhost:5000/swagger
```

To test authenticated endpoints:
1. Call `POST /api/auth/login` with seed credentials
2. Copy the `token` from the response
3. Click **Authorize** in Swagger UI
4. Paste the token (without `Bearer ` prefix) and click **Authorize**

The native .NET 10 OpenAPI JSON document is at:
```
http://localhost:5000/openapi/v1.json
```

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

## Docker Setup

The `docker-compose.yml` at the repo root starts SQL Server for local development:

```bash
docker compose up -d      # start SQL Server
docker compose down       # stop and remove container
docker compose down -v    # stop and remove container + data volume
```

**SQL Server connection details:**
- Host: `localhost:1433`
- Database: `TicketSystem`
- User: `sa`
- Password: `YourStrong!Passw0rd`

---

## Project Structure

```
ticket-test/
├── backend/
│   ├── src/
│   │   └── TicketSystem.API/
│   │       ├── Controllers/        # Auth, Tickets, Users
│   │       ├── Data/               # EF Core DbContext, Migrations, Seeder
│   │       ├── DTOs/               # Request/response models
│   │       ├── Models/             # Entity models + enums
│   │       ├── Services/           # JwtService, TicketService
│   │       └── Program.cs
│   └── tests/
│       └── TicketSystem.Tests/
│           ├── Controllers/        # AuthController unit tests
│           └── Services/           # TicketService + JwtService tests
├── frontend/
│   └── src/
│       ├── __tests__/              # Vitest unit tests
│       ├── api/                    # Axios API layer
│       ├── components/             # AppNav
│       ├── router/                 # Vue Router
│       ├── stores/                 # Pinia auth store
│       ├── utils/                  # time.ts helpers
│       └── views/                  # Page components
├── docker-compose.yml
└── README.md
```

---

## Environment Configuration

### Backend (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TicketSystem;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "CHANGE_ME_IN_PRODUCTION_USE_A_STRONG_SECRET_KEY_256BITS",
    "Issuer": "TicketSystem.API",
    "Audience": "TicketSystem.Frontend",
    "ExpiresInHours": "24"
  }
}
```

> ⚠️ Change the JWT key in production to a cryptographically strong random secret (32+ characters).

---

## License

MIT
