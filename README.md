# Vira Backend (.NET 9)

- API: ASP.NET Core + Controllers
- CQRS: MediatR + FluentValidation
- DB: EF Core + PostgreSQL (Npgsql)
- Auth: JWT Access + Refresh
- Logs: Serilog + Seq
- OpenAPI: Scalar (/scalar)

## Geliþtirme
- Docker: `docker compose -f docker/docker-compose.yml up -d`
- EF Migrations:
  - `Add-Migration <Name> -Project Vira.Infrastructure -StartupProject Vira.Api -Context Vira.Infrastructure.AppDbContext`
  - `Update-Database -Project Vira.Infrastructure -StartupProject Vira.Api -Context Vira.Infrastructure.AppDbContext`

## Secrets
- `dotnet user-secrets init -p src/Vira.Api/Vira.Api.csproj`
- `dotnet user-secrets set "Jwt:Key" "<32+ char secret>" -p src/Vira.Api/Vira.Api.csproj`
