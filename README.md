# EindOpdracht — Meerkeuzevragen
## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Visual Studio 2022 (v17.8+) with the **".NET desktop development"** workload
- SQL Server 2019+ (Express edition works fine)
- SQL Server Management Studio (SSMS) or any SQL client — optional but useful

---

## 1. Database Setup

Open SSMS (or `sqlcmd`) and run the setup script at the repo root:

```
setup_db.sql
```

This creates the `MeerkeuzevragenDB` database and all tables from scratch. Running it again drops and recreates all tables, so **don't run it on a database that already has data you want to keep**.

---

## 2. Connection String

Open `MV_WPF/appsettings.json` and update `Data Source` to match your SQL Server instance name:

```json
{
  "ConnectionStrings": {
    "SQLServerConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=MeerkeuzevragenDB;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True"
  },
  "AppSettings": {
    "databaseType": "SQL"
  }
}
```

Replace `YOUR_SERVER_NAME` with your instance name, e.g. `localhost`, `.\SQLEXPRESS`, or `DESKTOP-ABC123\SQLEXPRESS`.

The app uses **Windows Authentication** (`Integrated Security=True`). Make sure the Windows account you run the app under has `db_owner` or at minimum `db_datareader` + `db_datawriter` rights on `MeerkeuzevragenDB`.

---

## 3. Build & Run

```bash
# Restore packages and build
dotnet build EindOpdracht.sln

# Run the WPF app
dotnet run --project MV_WPF
```

Or open `EindOpdracht.sln` in Visual Studio, set **MV_WPF** as the startup project, and press F5.

