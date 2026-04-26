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
    public const string TestAdminPassword = "change-admin-password";

    /// <summary>
    /// Test user credentials (non-admin)
    /// </summary>
    public const string TestManagerUsername = "manager";
    public const string TestManagerPassword = "change-manager-password";

    /// <summary>
    /// API endpoint paths
    /// </summary>
    public static class Endpoints
    {
        public const string AuthLogin = "/api/auth/login";
        public const string AuthRegister = "/api/auth/register";
        public const string Products = "/api/products";
        public const string ProductById = "/api/products/{id}";
        public const string Suppliers = "/api/suppliers";
        public const string Sales = "/api/sales";
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
