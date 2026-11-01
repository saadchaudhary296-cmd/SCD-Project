# SCD-Project

Patient EHR application built with ASP.NET Core MVC and SQL Server.

## Run locally

1. Install the .NET 8 SDK and SQL Server (or SQL Server LocalDB).
2. In `Patient.EHR.Frontend`, copy `appsettings.example.json` to `appsettings.json`.
3. Set `ConnectionStrings:DefaultConnection` in `appsettings.json` to your local SQL Server connection string. The local settings file is ignored by Git.
4. From `Patient.EHR.Frontend`, run:

   ```powershell
   dotnet restore
   dotnet run
   ```

Do not commit production connection strings or patient data.
