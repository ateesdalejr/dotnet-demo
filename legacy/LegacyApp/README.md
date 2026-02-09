# ACME Industrial Supply - Customer Management System v2.3.1

> **WARNING:** This application is intentionally terrible. It was built to demonstrate real-world legacy code anti-patterns for training and demo purposes.

## Running the App

```bash
cd LegacyApp
dotnet run
```

Then open http://localhost:5000 (or whatever port it prints).

The SQLite database (`acme_legacy.db`) is created automatically on first run with seed data: 30 customers, 15 products, and ~80 orders.

## Architecture (or lack thereof)

```
LegacyApp/
├── Program.cs              ← DB init + seeding crammed in here
├── appsettings.json        ← Oracle creds, API keys, passwords committed
├── Controllers/
│   └── HomeController.cs   ← THE God Controller. Everything is here.
├── Models/
│   └── Models.cs           ← All models in one file, mixed naming conventions
└── Views/
    ├── Shared/_Layout.cshtml ← Bootstrap 3.3.7 via CDN
    ├── Home/Index.cshtml     ← Customer list + order entry + 200 lines of jQuery
    └── Home/Report.cshtml    ← Sales report + copy-pasted JS from Index
```

## What Makes This Code Terrible

### Security
- **SQL Injection everywhere** - string interpolation in all queries, zero parameterization
- **Secrets in config** - Oracle passwords, API keys, SMTP creds committed to repo
- **No CSRF protection** - anti-forgery tokens removed because "they broke IE11"
- **GET request for delete** - `DeleteCustomer` is a GET endpoint with no auth
- **No authentication** - "coming in Phase 3" (there was no Phase 3)

### Architecture
- **God Controller** - one controller handles customers, orders, products, reports, JSON APIs
- **No service layer** - business logic mixed directly into controller actions
- **No repository pattern** - raw SQL inline in every method
- **No dependency injection** - static connection string, `new SqliteConnection()` everywhere
- **DB init in Program.cs** - schema creation and 200+ lines of seed data in the startup file

### Code Quality
- **Duplicated code** - `viewOrder()` JS function copy-pasted between Index and Report views
- **Duplicated logic** - discount calculation exists in 3 places (seed, SaveOrder, and conceptually in the Oracle stored proc comment)
- **Tax rate hardcoded** - `0.08` appears in multiple places, also defined as `TAX_RATE` constant that's never used
- **Mixed naming** - `UPPER_CASE` Oracle columns mixed with `camelCase` C# properties
- **ViewBag abuse** - everything passed through `ViewBag`, no view models
- **Magic strings** - status codes ("Pending", "Procesing"), message format ("OK:", "ERR:")
- **Comments that lie** - outdated references to Oracle, Dave, tickets that don't exist
- **No using statements** - connections opened and manually closed, never properly disposed
- **No error handling** - if anything fails, the whole page crashes

### The Subtle Bug
Deleting a customer doesn't delete their orders. The customer list page uses `LEFT JOIN` so orphaned orders still show in reports. But the order detail view works fine... until someone tries to look up the deleted customer's name, which returns "Unknown". This has happened twice in "production." Mike in ops fixed it by re-inserting the customer manually in the database.

### Data Quirks
- The status value `"Procesing"` (missing an 's') exists in seed data and is **intentional** - it mimics a real typo that exists in production Oracle data. The reports and views both handle it. Fixing the typo would break the status breakdown report.

### What Would Be Scary to Refactor
1. **The order entry flow** - jQuery builds form rows, collects data into comma-separated hidden fields, POSTs to a controller that parses them back apart. Changing any piece breaks the others.
2. **The discount calculation** - exists in 3 places with slightly different variable names. Change one, forget the others.
3. **Status string matching** - scattered across controllers, views, and JavaScript. No enum, no constants.

## History
- **2017** - Originally Oracle Forms, written by Dave
- **2018** - "Migrated" to ASP.NET MVC by Dave (still Oracle DB)
- **2019** - Bootstrap 3 added, IE11 compatibility hacks
- **2020** - Chart.js integration attempted, abandoned after 2 days
- **2022** - Contractor migrated to ASP.NET Core, replaced Oracle with "temporary" SQLite
- **2023** - Dave left the company. Mike in ops maintains it now.
- **2024** - You're looking at it. Good luck.
