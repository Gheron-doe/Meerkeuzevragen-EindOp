# Meerkeuzevragen-EindOp
# Meerkeuzevragen — Setup Guide

Multiple-choice test application built with C# .NET 8, WPF (MVVM), and SQL Server.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| SQL Server | 2019+ | Express edition works fine |
| SQL Server Management Studio (optional) | any | for manual DB inspection |
| Visual Studio | 2022 | with **.NET desktop development** workload |

---

## 1. Create the Database

Open **SQL Server Management Studio** (or `sqlcmd`) and run the setup script:

```sql
-- In SSMS: File → Open → setup_db.sql → Execute (F5)
```

Or via command line:

```bash
sqlcmd -S localhost -E -i setup_db.sql
```

This creates `MeerkeuzevragenDB` with all 8 tables:
`Topic`, `Question`, `Answer`, `Test`, `TestQuestion`, `User`, `TestAttempt`, `AttemptAnswer`

---

## 2. Configure the Connection String

Open `MV_WPF\bin\Debug\net8.0-windows\appsettings.json"` and update the connection string to match your SQL Server instance:

```json
// MV_WPF\bin\Debug\net8.0-windows\appsettings.json
{
  "ConnectionStrings": {
    "SQLServerConnection": "Server=localhost;Database=MeerkeuzevragenDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True" //this here
  },
  "AppSettings": {
    
    "databaseType": "SQL"
  }
}
```

**Common variants:**

```
// Windows Authentication (default)
Server=localhost;Database=MeerkeuzevragenDB;Trusted_Connection=True;TrustServerCertificate=True;

// SQL Server Authentication
Server=localhost;Database=MeerkeuzevragenDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;

// Named instance (e.g. SQL Express)
Server=localhost\SQLEXPRESS;Database=MeerkeuzevragenDB;Trusted_Connection=True;TrustServerCertificate=True;
```

---

## 3. Open the Solution

Open `EindOpdracht.sln` in Visual Studio 2022.

**Solution projects:**

| Project | Role |
|---------|------|
| `MV_BL` | Business logic — domain models, services, interfaces |
| `MV_DL` | Data layer — SQL Server repositories via ADO.NET |
| `MV_Util` | Factories, importers, exporters, scoring strategies |
| `MV_WPF` | WPF MVVM presentation layer |
| `ConsoleAppTest` | Developer test console (scratch / demo) |
| `MV_xUnitTest` | xUnit unit tests |

---

## 4. Build and Run

Set **MV_WPF** as the startup project:

1. Right-click `MV_WPF` in Solution Explorer → **Set as Startup Project**
2. Press `F5` (or `Ctrl+F5` to run without debugger)

To run unit tests:

```bash
dotnet test MV_xUnitTest/
```

Or in Visual Studio: **Test → Run All Tests** (`Ctrl+R, A`)
