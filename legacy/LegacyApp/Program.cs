// Program.cs - ACME Industrial Supply Co. Customer Management System
// Originally written by Dave (2017), migrated from .NET Framework by contractor (2022)
// TODO: Dave left the company, nobody knows how half of this works
// NOTE: DO NOT TOUCH the session configuration, it breaks everything

using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session - added by contractor, don't remove
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // was 60, changed to 30 per ticket #4521, then back to 30... wait
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // GDPR? not sure, leave it
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database - this used to be Oracle, now SQLite "temporarily"
// See ticket #7834 - "migrate back to Oracle Q3 2023"
InitializeDatabase();

app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // MUST be before UseAuthorization or sessions break - learned the hard way
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Database init - copied from Dave's original Oracle setup script, modified for SQLite
// TODO: This should not be in Program.cs but it works so don't move it
void InitializeDatabase()
{
    // HACK: hardcoded path, was Oracle TNS, now SQLite file
    // Original Oracle connection: see appsettings.json OracleConnectionString
    var connStr = "Data Source=acme_legacy.db";

    using var conn = new SqliteConnection(connStr);
    conn.Open();

    // Create tables - translated from Oracle DDL by hand, probably missed some constraints
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS CUSTOMERS (
            CUSTOMER_ID INTEGER PRIMARY KEY AUTOINCREMENT,
            -- was NUMBER(10) in Oracle with SEQUENCE
            FIRST_NAME TEXT NOT NULL,
            LAST_NAME TEXT NOT NULL,
            EMAIL TEXT,
            PHONE TEXT,
            COMPANY_NAME TEXT,
            ADDRESS TEXT,
            CITY TEXT,
            STATE TEXT,
            ZIP_CODE TEXT,
            CREDIT_LIMIT REAL DEFAULT 5000.00,
            -- Oracle had DEFAULT 5000.00 NUMBER(10,2)
            STATUS INTEGER DEFAULT 1,
            -- 1=Active, 0=Inactive, 2=Suspended (added by Dave, never used)
            CREATED_DATE TEXT DEFAULT (datetime('now','localtime')),
            -- was SYSDATE in Oracle
            NOTES TEXT
        );

        CREATE TABLE IF NOT EXISTS PRODUCTS (
            PRODUCT_ID INTEGER PRIMARY KEY AUTOINCREMENT,
            PRODUCT_CODE TEXT NOT NULL,
            PRODUCT_NAME TEXT NOT NULL,
            DESCRIPTION TEXT,
            CATEGORY TEXT,
            UNIT_PRICE REAL NOT NULL,
            STOCK_QTY INTEGER DEFAULT 0,
            REORDER_LEVEL INTEGER DEFAULT 10,
            STATUS INTEGER DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS ORDERS (
            ORDER_ID INTEGER PRIMARY KEY AUTOINCREMENT,
            CUSTOMER_ID INTEGER,
            ORDER_DATE TEXT DEFAULT (datetime('now','localtime')),
            STATUS TEXT DEFAULT 'Pending',
            -- Pending, Approved, Shipped, Delivered, Cancelled
            -- NOTE: 'Procesing' (typo) exists in old data, do NOT fix or reports break
            SUBTOTAL REAL DEFAULT 0,
            TAX_AMOUNT REAL DEFAULT 0,
            TOTAL_AMOUNT REAL DEFAULT 0,
            SHIPPING_ADDRESS TEXT,
            NOTES TEXT,
            CREATED_BY TEXT DEFAULT 'SYSTEM',
            DISCOUNT_PCT REAL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS ORDER_DETAILS (
            DETAIL_ID INTEGER PRIMARY KEY AUTOINCREMENT,
            ORDER_ID INTEGER,
            PRODUCT_ID INTEGER,
            QUANTITY INTEGER NOT NULL,
            UNIT_PRICE REAL NOT NULL,
            LINE_TOTAL REAL NOT NULL,
            DISCOUNT REAL DEFAULT 0
        );
    ";
    cmd.ExecuteNonQuery();

    // Check if data exists - seed if empty
    cmd.CommandText = "SELECT COUNT(*) FROM CUSTOMERS";
    var count = Convert.ToInt32(cmd.ExecuteScalar());
    if (count == 0)
    {
        SeedData(conn);
    }
}

void SeedData(SqliteConnection conn)
{
    var cmd = conn.CreateCommand();

    // Products - copied from the Oracle export Dave did in 2019
    var products = new[]
    {
        ("IND-001", "Industrial Bolt Set M10", "Heavy duty bolts, zinc plated", "Fasteners", 24.99, 500),
        ("IND-002", "Steel Cable 3/8\" x 100ft", "Galvanized aircraft cable", "Cable & Wire", 89.50, 200),
        ("IND-003", "Safety Goggles Pro", "ANSI Z87.1 rated", "Safety", 12.99, 1000),
        ("IND-004", "Hydraulic Hose 1/2\"", "SAE 100R2AT rated to 5000 PSI", "Hydraulics", 45.00, 150),
        ("IND-005", "Bearing 6205-2RS", "Sealed ball bearing 25x52x15mm", "Bearings", 8.75, 2000),
        ("IND-006", "Welding Rod E6013 1/8\"", "5lb pack, all position", "Welding", 15.50, 800),
        ("IND-007", "Air Filter Element", "Replacement for Ingersoll-Rand compressors", "Filtration", 32.00, 300),
        ("IND-008", "Chain Hoist 1-Ton", "Manual chain block with 10ft lift", "Lifting", 189.99, 50),
        ("IND-009", "Pipe Wrench 18\"", "Heavy duty cast iron", "Hand Tools", 34.99, 250),
        ("IND-010", "Lubricant Grease Cartridge", "Lithium complex EP2, 14oz", "Lubrication", 6.99, 5000),
        ("IND-011", "Conveyor Belt 24\" x 50ft", "2-ply 330 grade rubber", "Conveyors", 450.00, 20),
        ("IND-012", "Electrical Conduit 3/4\" EMT", "10ft stick, steel", "Electrical", 8.25, 1500),
        ("IND-013", "Motor 5HP 3-Phase", "TEFC, 1750RPM, 184T frame", "Motors", 675.00, 15),
        ("IND-014", "V-Belt A68", "Classical wrapped, 70\" OC", "Power Transmission", 11.99, 400),
        ("IND-015", "Spray Paint Safety Yellow", "OSHA compliant, 12oz can", "Paint & Coatings", 5.49, 3000),
    };

    foreach (var p in products)
    {
        cmd.CommandText = $"INSERT INTO PRODUCTS (PRODUCT_CODE, PRODUCT_NAME, DESCRIPTION, CATEGORY, UNIT_PRICE, STOCK_QTY) VALUES ('{p.Item1}', '{p.Item2.Replace("'", "''")}', '{p.Item3}', '{p.Item4}', {p.Item5}, {p.Item6})";
        cmd.ExecuteNonQuery();
    }

    // Customers - realistic industrial supply customers
    var customers = new (string first, string last, string email, string phone, string company, string city, string state, string zip, double credit)[]
    {
        ("James", "Morrison", "jmorrison@steelworks.com", "555-0101", "Morrison Steel Works", "Pittsburgh", "PA", "15201", 25000),
        ("Sarah", "Chen", "schen@pacificmfg.com", "555-0102", "Pacific Manufacturing", "Portland", "OR", "97201", 50000),
        ("Robert", "Garcia", "rgarcia@texasweld.com", "555-0103", "Texas Welding Supply", "Houston", "TX", "77001", 35000),
        ("Linda", "Thompson", "lthompson@midwestind.com", "555-0104", "Midwest Industrial Corp", "Chicago", "IL", "60601", 40000),
        ("Michael", "O'Brien", "mobrien@atlanticpipe.com", "555-0105", "Atlantic Pipe & Fitting", "Boston", "MA", "02101", 30000),
        ("Patricia", "Williams", "pwilliams@southernmech.com", "555-0106", "Southern Mechanical", "Atlanta", "GA", "30301", 20000),
        ("David", "Kim", "dkim@precisiontools.com", "555-0107", "Precision Tools Inc", "Detroit", "MI", "48201", 15000),
        ("Jennifer", "Martinez", "jmartinez@coastaleng.com", "555-0108", "Coastal Engineering", "San Diego", "CA", "92101", 45000),
        ("William", "Johnson", "wjohnson@greatplains.com", "555-0109", "Great Plains Equipment", "Omaha", "NE", "68101", 60000),
        ("Maria", "Rodriguez", "mrodriguez@sunbelt.com", "555-0110", "Sunbelt Industrial", "Phoenix", "AZ", "85001", 25000),
        ("Richard", "Davis", "rdavis@northstarfab.com", "555-0111", "North Star Fabrication", "Minneapolis", "MN", "55401", 35000),
        ("Susan", "Wilson", "swilson@libertyind.com", "555-0112", "Liberty Industrial Supply", "Philadelphia", "PA", "19101", 28000),
        ("Thomas", "Anderson", "tanderson@rockymtn.com", "555-0113", "Rocky Mountain Hydraulics", "Denver", "CO", "80201", 22000),
        ("Karen", "Taylor", "ktaylor@bayarea.com", "555-0114", "Bay Area Machine Works", "San Francisco", "CA", "94101", 55000),
        ("Charles", "Brown", "cbrown@dixiesteel.com", "555-0115", "Dixie Steel & Supply", "Nashville", "TN", "37201", 18000),
        ("Nancy", "Moore", "nmoore@cascadepump.com", "555-0116", "Cascade Pump & Valve", "Seattle", "WA", "98101", 42000),
        ("Daniel", "Jackson", "djackson@empiremfg.com", "555-0117", "Empire Manufacturing", "New York", "NY", "10001", 70000),
        ("Betty", "White", "bwhite@heartlandsup.com", "555-0118", "Heartland Supply Co", "Kansas City", "MO", "64101", 20000),
        ("Mark", "Harris", "mharris@gulfcoast.com", "555-0119", "Gulf Coast Fabricators", "New Orleans", "LA", "70112", 32000),
        ("Dorothy", "Clark", "dclark@cornerstonemech.com", "555-0120", "Cornerstone Mechanical", "Cleveland", "OH", "44101", 27000),
        ("Steven", "Lewis", "slewis@pioneerweld.com", "555-0121", "Pioneer Welding Co", "Salt Lake City", "UT", "84101", 19000),
        ("Margaret", "Walker", "mwalker@goldenstate.com", "555-0122", "Golden State Industrial", "Los Angeles", "CA", "90001", 48000),
        ("Paul", "Hall", "phall@chesapeake.com", "555-0123", "Chesapeake Industrial", "Baltimore", "MD", "21201", 33000),
        ("Ruth", "Allen", "rallen@prairiemfg.com", "555-0124", "Prairie Manufacturing", "Wichita", "KS", "67201", 15000),
        ("Andrew", "Young", "ayoung@blueridge.com", "555-0125", "Blue Ridge Supply", "Charlotte", "NC", "28201", 21000),
        ("Helen", "King", "hking@tristate.com", "555-0126", "Tri-State Equipment", "Cincinnati", "OH", "45201", 38000),
        ("Joshua", "Wright", "jwright@pacnw.com", "555-0127", "Pacific Northwest Ind.", "Tacoma", "WA", "98401", 29000),
        ("Virginia", "Lopez", "vlopez@desertind.com", "555-0128", "Desert Industrial Supply", "Tucson", "AZ", "85701", 16000),
        ("Kevin", "Hill", "khill@lakeshore.com", "555-0129", "Lakeshore Manufacturing", "Milwaukee", "WI", "53201", 44000),
        ("Deborah", "Scott", "dscott@peachtree.com", "555-0130", "Peachtree Industrial", "Savannah", "GA", "31401", 23000),
    };

    foreach (var c in customers)
    {
        // NOTE: O'Brien has a quote in the name - this used to crash until Dave added Replace
        cmd.CommandText = $"INSERT INTO CUSTOMERS (FIRST_NAME, LAST_NAME, EMAIL, PHONE, COMPANY_NAME, CITY, STATE, ZIP_CODE, CREDIT_LIMIT) VALUES ('{c.first.Replace("'", "''")}', '{c.last.Replace("'", "''")}', '{c.email}', '{c.phone}', '{c.company.Replace("'", "''")}', '{c.city}', '{c.state}', '{c.zip}', {c.credit})";
        cmd.ExecuteNonQuery();
    }

    // Orders - generate realistic orders
    var rng = new Random(42); // fixed seed so data is consistent (Dave's idea)
    var statuses = new[] { "Pending", "Approved", "Shipped", "Delivered", "Delivered", "Delivered", "Cancelled", "Procesing" };
    // NOTE: "Procesing" typo is INTENTIONAL - exists in prod Oracle data, reports depend on it

    for (int i = 0; i < 80; i++)
    {
        var custId = rng.Next(1, 31);
        var status = statuses[rng.Next(statuses.Length)];
        var daysAgo = rng.Next(1, 365);
        var orderDate = DateTime.Now.AddDays(-daysAgo).ToString("yyyy-MM-dd HH:mm:ss");
        var numItems = rng.Next(1, 5);
        var subtotal = 0.0;

        // discount logic - copied from Oracle stored proc SP_CALC_DISCOUNT
        // customers with credit > 40000 get 5%, > 60000 get 10%
        var discountPct = 0.0;
        // BUG: this queries by custId but doesn't handle the case where customer was deleted
        // It "usually works" because we never delete customers

        cmd.CommandText = $"SELECT CREDIT_LIMIT FROM CUSTOMERS WHERE CUSTOMER_ID = {custId}";
        var creditObj = cmd.ExecuteScalar();
        var credit = creditObj != null ? Convert.ToDouble(creditObj) : 0;
        if (credit > 60000) discountPct = 0.10;
        else if (credit > 40000) discountPct = 0.05;

        cmd.CommandText = $"INSERT INTO ORDERS (CUSTOMER_ID, ORDER_DATE, STATUS, DISCOUNT_PCT, CREATED_BY) VALUES ({custId}, '{orderDate}', '{status}', {discountPct}, 'SEED')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT last_insert_rowid()";
        var orderId = Convert.ToInt32(cmd.ExecuteScalar());

        for (int j = 0; j < numItems; j++)
        {
            var prodId = rng.Next(1, 16);
            var qty = rng.Next(1, 20);

            cmd.CommandText = $"SELECT UNIT_PRICE FROM PRODUCTS WHERE PRODUCT_ID = {prodId}";
            var price = Convert.ToDouble(cmd.ExecuteScalar());
            var lineTotal = qty * price;
            subtotal += lineTotal;

            cmd.CommandText = $"INSERT INTO ORDER_DETAILS (ORDER_ID, PRODUCT_ID, QUANTITY, UNIT_PRICE, LINE_TOTAL) VALUES ({orderId}, {prodId}, {qty}, {price}, {lineTotal})";
            cmd.ExecuteNonQuery();
        }

        // Update order totals - tax calc copied from Oracle trigger TRG_ORDER_TOTALS
        var discount = subtotal * discountPct;
        subtotal = subtotal - discount;
        var tax = subtotal * 0.08; // TODO: tax rate should come from config, hardcoded for now
        var total = subtotal + tax;
        cmd.CommandText = $"UPDATE ORDERS SET SUBTOTAL = {subtotal}, TAX_AMOUNT = {tax}, TOTAL_AMOUNT = {total} WHERE ORDER_ID = {orderId}";
        cmd.ExecuteNonQuery();
    }
}
