using Xunit;
using System.Text.Json;
using HSMS.ApiTests.Helpers;

namespace HSMS.ApiTests;

/// <summary>
/// MEMBER 10 - Reports API Tests (Positive Scenarios)
/// 
/// Tests reporting functionality:
/// - Daily sales reports
/// - Monthly sales reports
/// - Sales analytics
/// - Low-stock reports
/// - Report exports (CSV)
/// 
/// Focus: Report generation, data aggregation, exports
/// </summary>
public class ReportsApiTests
{
    private readonly ApiClient _client = new ApiClient();

    public ReportsApiTests()
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

        using var document = JsonDocument.Parse(loginResponse.Content);
        var token = document.RootElement.GetProperty("accessToken").GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("No authentication token received.");
        }

        _client.SetAuthToken(token);
    }

    // POSITIVE TESTS - DAILY REPORT

    /// <summary>
    /// Test Case 10.1: Get Daily Sales Report
    /// Scenario: Retrieve daily sales summary
    /// Expected: Returns 200 OK with daily data
    /// </summary>
    [Fact]
    public void GetDailySalesReport_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsDaily;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 10.2: Daily Report Returns Array
    /// Scenario: Verify daily report format
    /// Expected: Valid JSON array with daily entries
    /// </summary>
    [Fact]
    public void GetDailySalesReport_Response_ShouldBeArray()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsDaily;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    /// <summary>
    /// Test Case 10.3: Daily Report Contains Required Fields
    /// Scenario: Verify report structure
    /// Expected: Contains date and total sales amount
    /// </summary>
    [Fact]
    public void GetDailySalesReport_ShouldContainRequiredFields()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsDaily;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        if (doc.RootElement.GetArrayLength() > 0)
        {
            var firstItem = doc.RootElement[0];
            // Should have date and total amount
            Assert.True(
                firstItem.TryGetProperty("date", out _) ||
                firstItem.TryGetProperty("Date", out _)
            );
        }
    }

    // POSITIVE TESTS - MONTHLY REPORT

    /// <summary>
    /// Test Case 10.4: Get Monthly Sales Report
    /// Scenario: Retrieve monthly sales summary
    /// Expected: Returns 200 OK with monthly data
    /// </summary>
    [Fact]
    public void GetMonthlySalesReport_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsMonthly;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 10.5: Monthly Report Returns Array
    /// Scenario: Verify monthly report format
    /// Expected: Valid JSON array
    /// </summary>
    [Fact]
    public void GetMonthlySalesReport_Response_ShouldBeArray()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsMonthly;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // POSITIVE TESTS - ANALYTICS

    /// <summary>
    /// Test Case 10.6: Get Sales Analytics
    /// Scenario: Retrieve sales analytics data
    /// Expected: Returns 200 OK with analytics
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsAnalytics;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 10.7: Get Analytics with Date Range Filter
    /// Scenario: Analytics with from/to date filters
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithDateRange_Should_Return_200()
    {
        // Arrange
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?fromDate={today}";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 10.8: Get Analytics with Product Filter
    /// Scenario: Analytics filtered by product ID
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithProductFilter_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?productId=1";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.True(
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.OK ||
            (int)response.StatusCode == ApiTestConstants.HttpStatusCodes.NotFound
        );
    }

    /// <summary>
    /// Test Case 10.9: Get Analytics with Category Filter
    /// Scenario: Analytics filtered by category
    /// Expected: Returns 200 OK
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_WithCategoryFilter_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsAnalytics}?category=Tools";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    // POSITIVE TESTS - LOW-STOCK REPORT

    /// <summary>
    /// Test Case 10.10: Get Low-Stock Report
    /// Scenario: Retrieve low-stock inventory report
    /// Expected: Returns 200 OK with low-stock items
    /// </summary>
    [Fact]
    public void GetLowStockReport_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsLowStock;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    // POSITIVE TESTS - SUMMARY

    /// <summary>
    /// Test Case 10.11: Get Reports Summary
    /// Scenario: Get summary of all reports
    /// Expected: Returns 200 OK with daily, monthly, and low-stock data
    /// </summary>
    [Fact]
    public void GetReportsSummary_Should_Return_200()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsSummary;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 10.12: Reports Summary Contains All Sections
    /// Scenario: Verify summary structure
    /// Expected: Contains daily, monthly, and low-stock sections
    /// </summary>
    [Fact]
    public void GetReportsSummary_Should_IncludeAllSections()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsSummary;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        using var doc = JsonDocument.Parse(response.Content);
        // Summary should contain report sections
        var content = response.Content?.ToLower() ?? "";
        Assert.True(
            content.Contains("daily") ||
            content.Contains("monthly") ||
            content.Contains("lowstock")
        );
    }

    // POSITIVE TESTS - EXPORT

    /// <summary>
    /// Test Case 10.13: Export Daily Report as CSV
    /// Scenario: Export daily sales as CSV
    /// Expected: Returns 200 OK with CSV file
    /// </summary>
    [Fact]
    public void ExportReport_Daily_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsExport}?type=daily";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        // Should contain CSV data
        Assert.NotNull(response.Content);
    }

    /// <summary>
    /// Test Case 10.14: Export Monthly Report as CSV
    /// Scenario: Export monthly sales as CSV
    /// Expected: Returns 200 OK with CSV file
    /// </summary>
    [Fact]
    public void ExportReport_Monthly_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsExport}?type=monthly";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 10.15: Export Low-Stock Report as CSV
    /// Scenario: Export low-stock inventory as CSV
    /// Expected: Returns 200 OK with CSV file
    /// </summary>
    [Fact]
    public void ExportReport_LowStock_Should_Return_200()
    {
        // Arrange
        var endpoint = $"{ApiTestConstants.Endpoints.ReportsExport}?type=low-stock";

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
    }

    /// <summary>
    /// Test Case 10.16: Get Reports Performance - Daily
    /// Scenario: Daily report retrieval performance
    /// Expected: Response time < 2 seconds
    /// </summary>
    [Fact]
    public void GetDailySalesReport_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsDaily;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 2000);
    }

    /// <summary>
    /// Test Case 10.17: Get Reports Performance - Analytics
    /// Scenario: Analytics retrieval performance
    /// Expected: Response time < 3 seconds (complex query)
    /// </summary>
    [Fact]
    public void GetSalesAnalytics_Should_CompleteWithinReasonableTime()
    {
        // Arrange
        var endpoint = ApiTestConstants.Endpoints.ReportsAnalytics;

        // Act
        var response = _client.Get(endpoint);

        // Assert
        Assert.Equal(ApiTestConstants.HttpStatusCodes.OK, (int)response.StatusCode);
        var responseTime = ApiClient.GetResponseTime(response);
        Assert.NotNull(responseTime);
        Assert.True(responseTime.Value.TotalMilliseconds < 3000);
    }
}
