# StockSmart

StockSmart is a web-based stock and sales system for a small retail shop. It replaces paper and spreadsheet stock records with one system for products, suppliers, sales, low-stock alerts and sales reports.

Built for the NQF5 capstone project.

## Features

| | Feature |
|---|---|
| FR1 | Login with three roles (Owner, Manager, Staff), plus user management for the Manager |
| FR2 | Products: view and search for everyone; add, edit, deactivate and reactivate for the Manager |
| FR3 | Suppliers: contact details and the products each one supplies |
| FR4/FR5 | New sale: several lines per sale, live total, stock deducted in one database transaction |
| FR6 | Low stock: products at or below their reorder level, with the supplier's contact details |
| FR7 | Daily and weekly sales reports, with best sellers (Owner and Manager) |

## Technology

- ASP.NET Core 8 Razor Pages (C#)
- SQL Server LocalDB, accessed with ADO.NET (`Microsoft.Data.SqlClient`) and parameterised SQL
- Bootstrap 5
- Git and GitHub for version control

## What you need

1. **.NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
2. **SQL Server Express LocalDB**: install it from the SQL Server Express installer (choose *Download Media*, then *LocalDB*), or with Visual Studio's *Data storage and processing* workload.
3. **Git** (optional): you can use *Code, then Download ZIP* on GitHub instead.

To check that both are installed, run these in PowerShell:

```powershell
dotnet --version
sqllocaldb info
```

The first should show a version starting with `8.`, and the second should list `MSSQLLocalDB`.

## How to run it

```powershell
git clone https://github.com/Jarrod607/StockSmart.git
cd StockSmart
dotnet run --launch-profile http
```

Then open **http://localhost:5184** in a browser.

**On the first run, the app creates the `StockSmart` database, its tables and sample data automatically.** You don't need to run any SQL scripts. The connection string is in `appsettings.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=StockSmart;Trusted_Connection=True;TrustServerCertificate=True;
```

To stop the app, press **Ctrl + C** in the terminal.

## Login accounts

| Username | Password | Role | Can do |
|---|---|---|---|
| `owner` | `Owner#2026` | Owner | Everything except user management, including reports |
| `manager` | `Manager#2026` | Manager | Everything, including managing users and editing products and suppliers |
| `staff` | `Staff#2026` | Staff | View products, suppliers and low stock; record sales |

These are sample accounts for testing. Passwords are stored as hashes, never as plain text.

## Starting again with fresh data

1. Stop the app.
2. In SQL Server Management Studio, connect to `(localdb)\MSSQLLocalDB`, right-click the **StockSmart** database, choose **Delete**, and tick **Close existing connections**.
3. Run the app again. It recreates the database and sample data.

The database files are stored in `%LOCALAPPDATA%\StockSmart`.

## Project structure

| Folder | Contents |
|---|---|
| `Pages/` | Razor Pages: one `.cshtml` (view) and `.cshtml.cs` (logic) per page |
| `Models/` | Classes for users, products, suppliers and sales |
| `Repositories/` | All database access, using parameterised SQL |
| `Services/` | Business rules, such as recording a sale and checking stock |
| `Data/` | Creates the database and loads the sample data on first run |
| `Evidence of Build/` | Test screenshots, test data scripts and the build diary |
| `Security/` | Password hashing |
| `Exceptions/` | Custom exceptions, such as database errors and insufficient stock |