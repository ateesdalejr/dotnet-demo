// Models.cs - All models in one file because "it's easier to find things" - Dave
// TODO: Should probably split this up someday
// NOTE: Some fields match Oracle column names (UPPER_CASE), some don't. Don't ask why.

namespace LegacyApp.Models
{
    // Customer model - matches CUSTOMERS table (mostly)
    public class Customer
    {
        public int CUSTOMER_ID { get; set; } // Oracle style, don't rename
        public string FIRST_NAME { get; set; }
        public string LAST_NAME { get; set; }
        public string Email { get; set; } // why is this one lowercase? nobody knows
        public string Phone { get; set; }
        public string COMPANY_NAME { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZIP_CODE { get; set; }
        public double CREDIT_LIMIT { get; set; }
        public int Status { get; set; }
        public string CREATED_DATE { get; set; }
        public string Notes { get; set; }

        // Computed - not from DB, populated manually in controller
        public int OrderCount { get; set; }
        public double TotalSpent { get; set; }

        // Added for the grid display, used in ViewBag too sometimes
        public string FullName => FIRST_NAME + " " + LAST_NAME;
        public string StatusText => Status == 1 ? "Active" : Status == 0 ? "Inactive" : "Suspended";
    }

    // Order model
    public class Order
    {
        public int ORDER_ID { get; set; }
        public int CUSTOMER_ID { get; set; }
        public string ORDER_DATE { get; set; }
        public string Status { get; set; } // note: lowercase 's' unlike Customer.Status which is int
        public double SUBTOTAL { get; set; }
        public double TAX_AMOUNT { get; set; }
        public double TOTAL_AMOUNT { get; set; }
        public string SHIPPING_ADDRESS { get; set; }
        public string Notes { get; set; }
        public string CREATED_BY { get; set; }
        public double DISCOUNT_PCT { get; set; }

        // Joined fields - sometimes populated, sometimes null, depends on the query
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public List<OrderDetail> Details { get; set; } // only populated on detail view
    }

    public class OrderDetail
    {
        public int DETAIL_ID { get; set; }
        public int ORDER_ID { get; set; }
        public int PRODUCT_ID { get; set; }
        public int Quantity { get; set; }
        public double UNIT_PRICE { get; set; }
        public double LINE_TOTAL { get; set; }
        public double Discount { get; set; }

        // Joined
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
    }

    public class Product
    {
        public int PRODUCT_ID { get; set; }
        public string PRODUCT_CODE { get; set; }
        public string PRODUCT_NAME { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public double UNIT_PRICE { get; set; }
        public int STOCK_QTY { get; set; }
        public int Status { get; set; }
    }
}
