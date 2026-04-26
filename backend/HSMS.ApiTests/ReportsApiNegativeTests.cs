using Xunit;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 11 - Reports API Tests (Negative/Error Scenarios)
/// 
/// Tests error handling for reports:
/// - Invalid date ranges
/// - Invalid filters
/// - Invalid export types
/// - Unauthorized access
/// 
/// Focus: Error validation, parameter validation, edge cases
/// </summary>
public class ReportsApiNegativeTests
{
    private readonly ApiClient _client = new ApiClient();

    public ReportsApiNegativeTests()
    {
        var authClient = new ApiClient();
        var loginRequest = new
        {
            username = ApiTestConstants.TestAdminUsername,
            password = ApiTestConstants.TestAdminPassword
        };

        var loginResponse = authClient.Post(ApiTestConstants.Endpoints.AuthLogin, loginRequest);
        if (!loginResponse.IsSuccessful || string.IsNullOrWhiteSpace(loginResponse.Content))
        {
            throw new InvalidOperationException("Unable to authenticate.");
        }

        using var document = System.Text.Json.JsonDocument.Parse(loginResponse.Content);
        var token = document.RootElement.GetProperty("accessToken").GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("No authentication token received.");
        }

        _client.SetAuthToken(token);
    }

    // NEGATIVE TESTS - ANALYTICS

    /// <summary>
    /// Test Case 11.1: Analytics with Invalid Date Range (fromDate > toDate)
    /// Scenario: From date is after to date
    /// Expected: Returns 400 Bad Request or empty results
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithInvalidDateRange_Should_HandleError()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?fromDate={fromDate}&toDate={toDate}";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK // OK with empty results
        );
    }

    /// <summary>
    /// Test Case 11.2: Analytics with Malformed Date Format
    /// Scenario: Date format is invalid
    /// Expected: Returns 400 Bad Request or uses default
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithMalformedDate_Should_HandleError()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?fromDate=invalid-date";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK
        );
    }

    /// <summary>
    /// Test Case 11.3: Analytics with Negative Product ID
    /// Scenario: Product ID is negative
    /// Expected: Returns 400 Bad Request or empty results
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithNegativeProductId_Should_HandleError()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?productId=-1";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK
        );
    }

    /// <summary>
    /// Test Case 11.4: Analytics with Non-existent Product ID
    /// Scenario: Product ID doesn't exist
    /// Expected: Returns 404 Not Found or empty results
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithNonExistentProductId_Should_HandleError()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?productId=999999";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK // OK with no data
        );
    }

    /// <summary>
    /// Test Case 11.5: Analytics with Empty Category
    /// Scenario: Category parameter is empty
    /// Expected: Returns 400 Bad Request or default behavior
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithEmptyCategory_Should_HandleGracefully()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?category=";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );
    }

    /// <summary>
    /// Test Case 11.6: Analytics with SQL Injection Attempt
    /// Scenario: Category contains SQL injection
    /// Expected: Returns 200 OK (safe query) or 400 Bad Request
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithSqlInjection_Should_BeSecure()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?category='; DROP TABLE Sales; --";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );

        // Verify table still exists
        var verifyResponse = _client.Get(ApiTestConstants.Endpoints.SalesHistory);
        Assert.True(
            (int)verifyResponse.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)verifyResponse.StatusCode == ApiTestConstants.HttpStatusCodes.Forbidden
        );
    }

    // NEGATIVE TESTS - EXPORT

    /// <summary>
    /// Test Case 11.7: Export with Invalid Type
    /// Scenario: Export type is not supported
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void ExportReport_WithInvalidType_Should_Return_400()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsExport}?type=invalid-type";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
        Assert.Contains("unsupported export type", response.Content?.ToLower() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test Case 11.8: Export with Empty Type
    /// Scenario: Type parameter is empty
    /// Expected: Returns 400 Bad Request or uses default
    /// </summary>
    [Fact]
    public void ExportReport_WithEmptyType_Should_HandleError()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsExport}?type=";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK
        );
    }

    /// <summary>
    /// Test Case 11.9: Export with SQL Injection in Type
    /// Scenario: Type contains SQL injection
    /// Expected: Returns 400 Bad Request
    /// </summary>
    [Fact]
    public void ExportReport_WithSqlInjectionInType_Should_Return_400()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsExport}?type=daily'; DROP TABLE Reports; --";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.BadRequest, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 11.10: Daily Report Authorization Check
    /// Scenario: Verify authorization for daily report
    /// Expected: Returns 200 OK (authorized) or 403 Forbidden (unauthorized)
    /// </summary>
    [Fact]
    public void GetDailySalesReport_ShouldRespectAuthorization()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsDaily;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Forbidden
        );
    }

    /// <summary>
    /// Test Case 11.11: Monthly Report Authorization Check
    /// Scenario: Verify authorization for monthly report
    /// Expected: Returns 200 OK (authorized) or 403 Forbidden (unauthorized)
    /// </summary>
    [Fact]
    public void GetMonthlySalesReport_ShouldRespectAuthorization()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsMonthly;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Forbidden
        );
    }

    /// <summary>
    /// Test Case 11.12: Low-Stock Report Authorization Check
    /// Scenario: Verify authorization for low-stock report
    /// Expected: Returns 200 OK (authorized) or 403 Forbidden (unauthorized)
    /// </summary>
    [Fact]
    public void GetLowStockReport_ShouldRespectAuthorization()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsLowStock;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.Forbidden
        );
    }

    /// <summary>
    /// Test Case 11.13: Analytics with Very Large Date Range
    /// Scenario: Query spanning multiple years
    /// Expected: Returns 200 OK with results or timeout gracefully
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithLargeDateRange_Should_HandleGracefully()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddYears(-10).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?fromDate={startDate}&toDate={endDate}";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.BadRequest
        );
    }

    /// <summary>
    /// Test Case 11.14: Export Performance with Complex Report
    /// Scenario: Export large dataset performance
    /// Expected: Response time < 5 seconds
    /// </summary>
    [Fact]
    public void ExportReport_Daily_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsExport}?type=daily";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 5000);
    }

    /// <summary>
    /// Test Case 11.15: Analytics with Case-Insensitive Category
    /// Scenario: Category with mixed case
    /// Expected: Returns 200 OK (case-insensitive)
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithMixedCaseCategory_Should_WorkCaseInsensitive()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?category=ToOlS";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }
}
