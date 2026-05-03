namespace HSMS.ApiTests.Helpers;

/// <summary>
/// Constants used across all API tests
/// </summary>
public static class ApiTestConstants
{
    /// <summary>
    /// Base URL of the backend API
    /// </summary>
    public const string BaseUrl = "http://localhost:5162";

    /// <summary>
    /// Test admin credentials for authentication
    /// </summary>
    public const string TestAdminUsername = "admin";
    public const string TestAdminPassword = "Admin@123";

    /// <summary>
    /// Test user credentials (manager role)
    /// </summary>
    public const string TestManagerUsername = "manager";
    public const string TestManagerPassword = "Manager@123";

    /// <summary>
    /// Test user credentials (cashier role)
    /// </summary>
    public const string TestCashierUsername = "cashier";
    public const string TestCashierPassword = "Cashier@123";

    /// <summary>
    /// API endpoint paths
    /// </summary>
    public static class Endpoints
    {
        // Auth
        public const string AuthLogin = "/api/auth/login";
        public const string AuthRegister = "/api/auth/register";

        // Products
        public const string Products = "/api/products";
        public const string ProductById = "/api/products/{id}";
        public const string ProductInventory = "/api/products/inventory";
        public const string ProductLowStock = "/api/products/low-stock";
        public const string ProductSearch = "/api/products/search";
        public const string ProductStock = "/api/products/{id}/stock";

        // Suppliers
        public const string Suppliers = "/api/suppliers";
        public const string SupplierById = "/api/suppliers/{id}";

        // Sales
        public const string Sales = "/api/sales";
        public const string SalesHistory = "/api/sales/history";
        public const string SalesById = "/api/sales/{id}";
        public const string SalesInvoice = "/api/sales/{id}/invoice";

        // Reports
        public const string ReportsDaily = "/api/reports/daily";
        public const string ReportsMonthly = "/api/reports/monthly";
        public const string ReportsAnalytics = "/api/reports/analytics";
        public const string ReportsLowStock = "/api/reports/low-stock";
        public const string ReportsSummary = "/api/reports/summary";
        public const string ReportsExport = "/api/reports/export";

        // Users
        public const string Users = "/api/users";
        public const string UserById = "/api/users/{id}";
        public const string UserRole = "/api/users/{id}/role";
        public const string UserPassword = "/api/users/{id}/password";
    }

    /// <summary>
    /// HTTP status codes for assertions
    /// </summary>
    public static class HttpStatusCodes
    {
        public const int OK = 200;
        public const int Created = 201;
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int Conflict = 409;
        public const int InternalServerError = 500;
    }
}
