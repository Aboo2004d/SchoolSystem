# SchoolSystem

SchoolSystem is a multi-role school-management platform built with ASP.NET Core MVC, ASP.NET Core Identity, Entity Framework Core, SQL Server, and Redis. It manages schools, managers, teachers, students, classes, subjects, assignments, grades, attendance, certificates, exports, and load-test data.

## Documentation

- [التوثيق الكامل باللغة العربية](docs/PROJECT_DOCUMENTATION_AR.md)
- [Complete English documentation](docs/PROJECT_DOCUMENTATION_EN.md)

The documentation covers architecture, database design, GUID identifiers, Identity, roles and ownership, MVC/API separation, API request and response contracts, CSRF, Redis, migrations, seeders, certificates, operations, testing, troubleshooting, and production readiness.

## Technology stack

- .NET 9 / ASP.NET Core MVC
- ASP.NET Core Identity with GUID keys
- Entity Framework Core 9 and SQL Server
- Redis distributed cache
- Razor, Bootstrap, jQuery, DataTables, and Chart.js
- QuestPDF, EPPlus, MailKit, and Notyf

## Local development

Prerequisites: a .NET SDK that supports `net9.0`, SQL Server, and Redis/Memurai.

```powershell
dotnet restore
dotnet build
dotnet watch run
```

Development startup applies pending EF Core migrations automatically. Secrets such as SQL credentials, SMTP credentials, seeder passwords, and production Redis settings must be supplied through User Secrets, environment variables, or a production secret store; they must not be committed.

## Security model

Authorization is based on ASP.NET Core Identity roles (`Admin`, `Manager`, `Teacher`, and `Student`) plus database-backed resource ownership and school scope. Session exists only for compatibility/display state and is not an authorization source. State-changing browser API requests require antiforgery tokens.

## License

See [LICENSE](LICENSE).
