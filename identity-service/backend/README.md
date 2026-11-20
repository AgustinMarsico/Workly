# Identity Service Backend

.NET 8 backend extracted from the original Workly API identity surface.

```powershell
dotnet build IdentityService.sln
dotnet run --project IdentityService.Api/IdentityService.Api.csproj
```

EF migrations live in `DataAccess/Migrations` and create only:

- `users`
- `invitations`
- `api_keys`

No company, area, space, reservation, or dashboard tables are created here.
