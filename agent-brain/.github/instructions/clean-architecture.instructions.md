---
applyTo: "src/**/*.cs"
---

# Clean Architecture — .NET 8

- Dependency direction: Domain ← Application ← Infrastructure ← Api.
- Domain: entities, enums, domain exceptions only. No EF Core, no ASP.NET, no `System.Text.Json`.
- Application: interfaces, DTOs, services, validators. No `DbContext`, no EF Core usings.
- Infrastructure: `DbContext`, `IEntityTypeConfiguration<T>`, repositories, JWT, external services.
- Api: controllers only map HTTP ↔ service. No business logic, no direct DbContext.
- One class = one responsibility. Prefer constructor injection. No `new` of a service inside another service.
- All database / HTTP / file I/O is `async` with `CancellationToken` passed through.
- No `DateTime.Now` — use `DateTime.UtcNow`. No hardcoded connection strings or secrets.
- Errors: throw domain exceptions from Application, map to HTTP status in Api middleware/filter — never return stack traces to the client.
