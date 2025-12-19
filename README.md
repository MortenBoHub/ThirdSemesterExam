
# Døde Duer

## Overview
### What this project does
- This project is a collaboration between Christoffer Abraham & Morten Bo Kristensen to create a website for Jerne Idrætsforening to emulate the game "Døde Duer" for their 3rd Semester Exam.
- Key Features:
  - A game board for players to buy boards & numbers from.
  - An admin page for CRUD operations on boards, players and funds.
  - A profile page for players to view their funds and change certain things for their account.
  - A login page with full authentication.

### Tech stack
- **Frontend**: React 19, TypeScript, Vite, Tailwind CSS, DaisyUI, Jotai (State Management), React Router 7.
- **Backend**: ASP.NET Core (.NET 9), Entity Framework Core.
- **Database**: PostgreSQL (Neon Tech).
- **Tooling**: Vitest (Frontend Testing), Sieve (Sorting/Filtering/Paging), Swagger (NSwag) (API Documentation).

## Architecture (& High-Level Flow)
- **Frontend**: Client-side SPA using React. Communicates with the Backend via RESTful API calls.
- **Backend**: Layered architecture with Controllers, Services, and Data Access layers. Uses EF Core for ORM.
- **Auth Flow**: 
  1. User logs in via POST /api/auth/login.
  2. Backend validates credentials and returns a JWT (signed with HMACSHA512).
  3. Frontend stores JWT in localStorage.
  4. Frontend includes JWT in Authorization: Bearer <token> header for protected API calls.

## Security (& Authorization)
### Authentication
- Uses JWT (JSON Web Tokens) for stateless authentication.
- Tokens contain claims: Id, Email, and Role. (Also isMock)
- Password hashing with HMACSHA512 and unique salt for the admin.
- Password hashing with Bcrypt and unique salt for all other users.
- Mock login should be enabled by default in development?

### Authorization model
#### Roles (& permissions)
| Role  | Can access                                                                     | Notes |
|:------|:-------------------------------------------------------------------------------| :--- |
| Admin | Full CRUD on players, Activate/Deactivate boards, Draw numbers, Process funds. | Highest privilege level. |
| Player | View active boards, Buy boards, Profile management, Fund requests.             | Standard player role. |

#### Protected areas (routes/pages)
- /user: Accessible by authenticated Users and Admins.
- /admin: Accessible only by Admins.
- /login: Public.

#### Protected endpoints ((API))
- [Authorize(Roles = "Admin")]: Player creation, board activation, number drawing, soft-delete/restore.
- [Authorize]: Updating own profile, buying boards, fund requests.
- [AllowAnonymous]: Login, Register, Health check, Public player/board views.

### Secrets handling
- Secrets are managed via appsettings.json and environment variables.
- JwtSecret and Db connection string are and must never be committed to version control in production environments. As such they are gitignored
- Deployment uses fly.toml where secrets should be set using fly secrets set.

## Environment (& Configuration)
### Prerequisites
- **Node.js**: v18.0 or higher
- **npm**: v9.0 or higher
- **.NET SDK**: 9.0
- **PostgreSQL**: 15 or higher (Neon recommended)

### Local setup
1. **Clone repo**: git clone <https://github.com/MortenBoHub/ThirdSemesterExam>
2. **Backend Setup**:
   - Navigate to server/api.
   - Update appsettings.Development.json with your local DB connection string.
   - Run dotnet restore.
   - Run dotnet run.
   - **Note**: By default, the backend uses Docker Testcontainers for PostgreSQL in Development. Ensure Docker is running.
3. **Frontend Setup**:
   - Navigate to client.
   - Run npm install.
   - Run npm run dev.

### Configuration
#### Frontend
- Managed via Vite environment variables and proxy settings if applicable.

#### Backend
- appsettings.json: Global configuration.
- appsettings.Development.json: Local development overrides (DB, JwtSecret).
- AppOptions.cs: Strongly-typed configuration mapping.

#### Database
- Migrations are handled via EF Core.
- Database was created using Neon Tech's PostgreSQL service.
- DB schema is defined in server/dataccess/schema.sql.
- schema.sql also has data that was inserted into the DB during development, namely 20 years worth of Boards and boiler plate for creating an Admin user.

## Linting (& Formatting)
### Frontend
- **ESLint**: npm run lint
- **Prettier**: npm run format

### Backend
- **dotnet format**: Use dotnet format for standard .NET code styling.

## Scripts & Commands
### Frontend
- npm install: Install dependencies.
- npm run dev: Start development server.
- npm run build: Build for production.
- npm run lint: Run ESLint.
- npm test: Run Vitest.

### Backend
- dotnet restore: Restore NuGet packages.
- dotnet build: Build the solution.
- dotnet run: Start the API server.
- dotnet test: Run backend tests.

## Current State & What Works
- **Authentication**: Full login/register flow with JWT.
- **CRUD Operations**: Most CRUD operations like requesting funds, (soft-)deleting players, creating players.
- **Game Board**: Buying boards and drawing winning numbers.
- **State Management**: Reactive UI using Jotai and Tailwind.

## Known Bugs (& Limitations)
- **CRUD Operations**: There have been issues with CRUD operations sometimes not working. Creating Players was working for quite a while before deciding not to work randomly, but is back to working we hope. Reading Players from the database also sometimes decides to stop working for the list in admin's user page, might require a reload. Updating your own profile also seems to encounter errors.
- **Logging**: The program has honestly very limited logging capabilities.
- **Mock Login**: There's been trouble with the mock login on development environments. If you wish to try, it's admin/admin and user/user for the logins and passwords.
- **Soft Delete**: Players are soft-deleted, so if you want to permanently delete someone, it requires manual DB action. Restoring soft-deleted players is also possible but only so long as you dont close down the Bruger dialogue box.

## Test Logins (& Links)
### Environments
- **Local**: [http://localhost:5173](http://localhost:5173) (Vite default)
- **Website**: https://doededuer-jerneif.fly.dev/
- **API Documentation**: https://jerneif-doededuer.fly.dev/swagger/

### Test accounts
| EMail                 | Password | Role   | Notes               |
|:----------------------|:---------|:-------|:--------------------|
| morkri02@easv365.dk   | 12345678 | Admin  | The only admin      |
| chrabr01@easv365.dk   | 12345678 | Player | Has plenty of funds |
| ipsumlorem@easv365.dk | 12345678 | Player | Has no funds        |

## Troubleshooting
- **Database Connection**: Ensure the connection string in appsettings.Development.json is valid and the DB is accessible.
- **CORS Issues**: Check AllowedCorsOrigins in AppOptions if the frontend cannot connect to the backend.

## License
- Internal usage note: Jerne Idrætsforening.
