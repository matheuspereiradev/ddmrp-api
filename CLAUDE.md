# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Service.API is a .NET 10 Web API (generic user-management service, formerly named "Escola") built as a layered ("Clean Architecture"-style) solution with 5 projects. It uses EF Core with PostgreSQL (Npgsql), JWT bearer authentication, and BCrypt for password hashing.

## Commands

```bash
# Restore & build
dotnet build Service.slnx

# Run the API (from repo root or Service.API/)
dotnet run --project Service.API/Service.API.csproj

# EF Core migrations (DbContext and migrations assembly live in Service.Infra.Data)
dotnet ef migrations add <Name> --project Service.Infra.Data --startup-project Service.API
dotnet ef database update --project Service.Infra.Data --startup-project Service.API

# Run tests
dotnet test Service.Tests/Service.Tests.csproj
```

`appsettings.json` deliberately ships `ConnectionStrings:DefaultConnection` and `Jwt:SecretKey` empty. Real values for local dev live in `Service.API/.env` (gitignored; loaded via `DotNetEnv.Env.Load()` at the very top of `Program.cs`, before `WebApplication.CreateBuilder(args)`, so the values land in the process environment and are then picked up by ASP.NET Core's built-in environment-variables configuration provider). `Service.API/.env.example` is the committed template (`ConnectionStrings__DefaultConnection`, `Jwt__SecretKey` — double underscore = nested config key). Copy it to `.env` and fill in real values before running. The API needs a PostgreSQL instance reachable at that connection string before login/token generation will work.

## Architecture

Five projects, referenced top-down (API → Infra.Ioc → {Application, Infra.Data} → Domain). Namespaces, project folders, and `.csproj`/assembly names are all consistently `Service.*` (e.g. the `Service.API` project folder contains `Service.API.csproj`).

- **Service.Domain** — entities (`Entities/BaseEntity.cs`, `Entities/User.cs`, `Entities/Role.cs`), repository interfaces (`Interfaces/IBaseRepository.cs`, `IUserRepository.cs`, `IRoleRepository.cs`), the `IAuthenticate`/`ICurrentUserService` account interfaces, and `Pagination/PagedList.cs`. No dependencies on other projects.
- **Service.Application** — DTOs (`DTOs/User/*`, `DTOs/Role/*`), service interfaces (`Interfaces/IBaseService.cs`, `IUserService.cs`, `IRoleService.cs`, `IAuthenticateService.cs`), and service implementations (`Services/BaseService.cs`, `UserService.cs`, `RoleService.cs`, `AuthenticateService.cs`). Depends only on Domain. Custom exceptions (`Exceptions/AppException.cs` and its subclasses) are thrown here and translated to HTTP status codes by the API's exception middleware.
- **Service.Infra.Data** — `Context/ApplicationDbContext.cs` (Npgsql), EF entity configurations (`EntitiesConfiguration/`), repository implementations (`Repositories/BaseRepository.cs`, `UserRepository.cs`, `RoleRepository.cs`), `Identity/AuthenticateProvider.cs` (JWT generation), `Helpers/PaginationHelper.cs`, and `Migrations/`.
- **Service.Infra.Ioc** — the composition root. `DependencyInjection.cs` wires up the DbContext, JWT bearer authentication, and DI registrations for repositories/services. Also has `ClaimsPrincipalExtension.cs` (`ClaimsPrincipal.GetUserId()`, reads the `"id"` claim) and `CurrentUserService.cs`.
- **Service.API** — ASP.NET Core host: `Program.cs` wires Swagger (with bearer auth), CORS, and `AddInfrastructure()`; `Controllers/UserController.cs`, `Controllers/RoleController.cs`; `Middleware/ExceptionMiddleware.cs` (global try/catch mapping exceptions to status codes); `Filters/ApiResponseWrapperFilter.cs` (wraps every response in `ApiResponseDto<T>`); `Models/ApiResponseDto.cs`, `PaginationParams.cs`, `PaginationHeader.cs`, `UserLogin.cs`; `Extensions/HttpExtensions.cs` (adds a `Pagination` response header).
- **Service.Tests** — xUnit + NSubstitute + EF Core InMemory. `Application/` unit-tests services (`UserService`, `AuthenticateService`) against a faked `IUserRepository`/`IAuthenticate`; `InfraData/` tests `BaseRepository`/`UserRepository` against a real `ApplicationDbContext` on the InMemory provider, with a faked `ICurrentUserService`. No HTTP-layer tests yet (controllers, `ExceptionMiddleware`, `ApiResponseWrapperFilter`) and no CI wired to run this automatically.

### Key patterns

- **Generic base service/repository**: `BaseService<TEntity, TGetDTO, TPostDTO, TPutDTO>` and `BaseRepository<T>` implement CRUD + pagination once; entity-specific classes (`UserService`, `UserRepository`) extend them and only implement `ToGetDTO`/`ToEntity`/`ApplyUpdate` mapping plus any custom queries (e.g. `GetByEmail`). Follow this pattern when adding new entities rather than writing bespoke CRUD.
- **Soft delete**: `BaseEntity` has `deletedAt`/`deletedBy`; `BaseRepository` filters `deletedAt == null` on reads and sets `deletedAt` instead of removing rows on delete. Any new entity/query must respect this filter.
- **Audit fields are automatic, not passed as parameters**: `BaseRepository<T>` injects `ICurrentUserService` (`Service.Domain.Account`, implemented by `CurrentUserService` in `Service.Infra.Ioc` via `IHttpContextAccessor` + `ClaimsPrincipalExtension.GetUserId()`) and uses it to fill `createdBy`/`updatedBy`/`deletedBy` (alongside `createdAt`/`updatedAt`/`deletedAt` via `DateTime.UtcNow`) on every `AddAsync`/`UpdateAsync`/`DeleteAsync`. Neither `IBaseService` nor controllers pass a `userId` around — don't reintroduce that parameter when adding new entities/modules.
- **Email uniqueness is app-level only, by design**: there is no unique DB constraint on `User.Email` — `UserService.AddAsync` checks via `GetByEmail` before inserting. This is a deliberate decision, not an oversight; don't add a unique index/migration for it without checking first.
- **User ↔ Role**: one `Role` has many `User`; each `User` has exactly one `Role` via the required `IdRole` FK (`OnDelete(DeleteBehavior.Restrict)` in `UserConfiguration`), no default value and no seed data — the DB is treated as empty, so there's no backfill concern. `UserService.AddAsync`/`UpdateAsync` both validate `IdRole` via `IRoleRepository.Exists(...)` before touching the repository, throwing `BadRequestException("Role not found.")` — don't let an invalid `IdRole` reach the DB and surface as a raw FK-violation 500.
- **Auth**: JWT claims carry `"id"` and `"email"` (see `AuthenticateProvider.GenerateToken`); `ClaimsPrincipalExtension.GetUserId()` is the standard way to read the current user's id in controllers (see `UserController.UpdateUser`, which updates the authenticated caller rather than taking an id in the route). Passwords are hashed with `BCrypt.Net.BCrypt`.
- **User creation is intentionally not public**: `POST /api/user` requires `[Authorize]` by design — there is no public self-signup endpoint; only an already-authenticated user can create another user. Do not remove that `[Authorize]` to "fix" onboarding.
- **Every HTTP response is an `ApiResponseDto<T>`** (`Success`, `Data`, `Errors`, `Timestamp`). Controller actions never construct it themselves — just `return Ok(data)`/`return NotFound()` etc. with the raw payload; the globally-registered `ApiResponseWrapperFilter` (an `IAsyncResultFilter`, added via `AddControllers(options => options.Filters.Add<ApiResponseWrapperFilter>())` in `Program.cs`) wraps every successful `ObjectResult`/`StatusCodeResult` into the envelope automatically. This was a deliberate choice over wrapping manually per-action specifically so it scales as more controllers/endpoints are added — never revert to manual per-action wrapping.
- **Error handling**: throw exceptions derived from `AppException` (`Service.Application.Exceptions`) — `NotFoundException` (404), `BadRequestException` (400), or the generic `HttpException(message, statusCode)` for an ad-hoc code — the exception itself carries the status code via its `StatusCode` property. `ExceptionMiddleware` is the single catch point: it reads `AppException.StatusCode` (falls back to 401 for `UnauthorizedAccessException`, 500 otherwise) and returns `ApiResponseDto<object?>.Fail(...)`. In production, an unexpected (non-`AppException`) error's message is hidden behind "Internal server error"; `AppException` messages are always shown (they're meant to be user-facing).
- **Pagination**: list endpoints take `PaginationParams` (`PageNumber`/`PageSize`) and return `PagedList<T>`; controllers call `Response.AddPaginationHeader(...)` to expose a `Pagination` response header (also exposed via CORS `Access-Control-Expose-Headers`).
- **CORS**: allowed origins come from `Cors:AllowedOrigins` in `appsettings.json` (a plain string array, not a secret — don't move it to `.env`), read once in `Program.cs` into an `AddCors` policy named `"Default"` applied via `app.UseCors("Default")`. Defaults to an empty array (blocks all cross-origin requests) until a real frontend origin is added.
