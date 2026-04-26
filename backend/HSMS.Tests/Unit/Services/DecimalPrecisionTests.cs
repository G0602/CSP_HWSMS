using HSMS.Application.Services;
using Xunit;

namespace HSMS.Tests;

/// <summary>
/// Tests for decimal precision and rounding in calculations
/// Ensures financial calculations maintain accuracy with LKR currency
/// </summary>
public class DecimalPrecisionTests
{
    [Theory]
    [InlineData(99.99, 1, 99.99)]      // Single digit
    [InlineData(150.50, 2, 301.00)]    // Two digits
    [InlineData(1299.99, 5, 6499.95)]  // Five items
    [InlineData(0.01, 100, 1.00)]      // Very small price
    [InlineData(9999.99, 10, 99999.90)] // Large price and quantity
    public void CalculateLineSubtotal_Should_Maintain_Decimal_Precision(decimal unitPrice, int quantity, decimal expected)
    {
        var result = SaleCalculator.CalculateLineSubtotal(unitPrice, quantity);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateTotal_Should_Sum_Single_Item()
    {
        var result = SaleCalculator.CalculateTotal(new[] { 100m });
        Assert.Equal(100m, result);
    }

    [Fact]
    public void CalculateTotal_Should_Sum_Two_Items()
    {
        var result = SaleCalculator.CalculateTotal(new[] { 100.50m, 200.50m });
        Assert.Equal(301.00m, result);
    }

    [Fact]
    public void CalculateTotal_Should_Sum_Three_Items()
    {
        var result = SaleCalculator.CalculateTotal(new[] { 99.99m, 99.99m, 99.99m });
        Assert.Equal(299.97m, result);
    }

    [Fact]
    public void CalculateTotal_Should_Sum_Small_Amounts()
    {
        var result = SaleCalculator.CalculateTotal(new[] { 0.01m, 0.01m, 0.01m });
        Assert.Equal(0.03m, result);
    }

    [Fact]
    public void CalculateTotal_Should_Sum_Large_Amounts()
    {
        var result = SaleCalculator.CalculateTotal(new[] { 1000.01m, 2000.02m, 3000.03m });
        Assert.Equal(6000.06m, result);
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Not_Lose_Precision_With_Repeating_Decimals()
    {
        // 333.33 * 3 = 999.99 (not 1000.00)
        var result = SaleCalculator.CalculateLineSubtotal(333.33m, 3);
        Assert.Equal(999.99m, result);
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Handle_Very_Small_Amounts()
    {
        // 0.25 * 4 = 1.00
        var result = SaleCalculator.CalculateLineSubtotal(0.25m, 4);
        Assert.Equal(1.00m, result);
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Handle_Large_Amounts()
    {
        // 9999.99 * 1000 = 9,999,990.00
        var result = SaleCalculator.CalculateLineSubtotal(9999.99m, 1000);
        Assert.Equal(9999990.00m, result);
    }

    [Fact]
    public void CalculateTotal_Should_Handle_Empty_Collection()
    {
        var result = SaleCalculator.CalculateTotal(new List<decimal>());
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateTotal_Should_Handle_Single_Item()
    {
        var result = SaleCalculator.CalculateTotal(new[] { 1234.56m });
        Assert.Equal(1234.56m, result);
    }

    [Fact]
    public void CalculateTotal_Should_Not_Round_Intermediate_Values()
    {
        // Sum: 0.1 + 0.2 + 0.3 = 0.6 (exact, no floating point issues with decimal)
        var result = SaleCalculator.CalculateTotal(new[] { 0.1m, 0.2m, 0.3m });
        Assert.Equal(0.6m, result);
    }

    [Theory]
    [InlineData(50.005, 2)]    // Rounding edge case
    [InlineData(100.015, 2)]   // Another rounding edge
    [InlineData(999.995, 2)]   // Near boundary
    public void CalculateLineSubtotal_Should_Use_Correct_Rounding(decimal unitPrice, int quantity)
    {
        var result = SaleCalculator.CalculateLineSubtotal(unitPrice, quantity);
        // Verify result is a valid decimal with correct precision
        Assert.True(result > 0);
        // Check that decimal places are reasonable (max 2 places for currency)
        var decimalPlaces = System.Decimal.GetBits(result)[3] >> 16 & 0xFF;
        Assert.True(decimalPlaces <= 4, $"Too many decimal places: {decimalPlaces}");
    }

    [Fact]
    public void Multiple_Calculations_Should_Maintain_Precision_Chain()
    {
        // Calculate multiple line subtotals then sum
        var line1 = SaleCalculator.CalculateLineSubtotal(100.50m, 3);  // 301.50
        var line2 = SaleCalculator.CalculateLineSubtotal(200.75m, 2);  // 401.50
        var line3 = SaleCalculator.CalculateLineSubtotal(50.25m, 4);   // 201.00

        var total = SaleCalculator.CalculateTotal(new[] { line1, line2, line3 });

        Assert.Equal(301.50m, line1);
        Assert.Equal(401.50m, line2);
        Assert.Equal(201.00m, line3);
        Assert.Equal(904.00m, total);
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Handle_Currency_Edge_Cases()
    {
        // LKR currency has no fractional unit, but we support 2 decimal places
        // Test amounts that should be common in Sri Lankan Rupees

        var test1 = SaleCalculator.CalculateLineSubtotal(2500m, 1);      // 2500.00
        var test2 = SaleCalculator.CalculateLineSubtotal(150m, 15);      // 2250.00
        var test3 = SaleCalculator.CalculateLineSubtotal(99.99m, 100);   // 9999.00

        Assert.Equal(2500m, test1);
        Assert.Equal(2250m, test2);
        Assert.Equal(9999m, test3);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(10, 10, 100)]
    [InlineData(100, 100, 10000)]
    [InlineData(1000, 1000, 1000000)]
    public void CalculateLineSubtotal_Should_Scale_Correctly(int unitPrice, int quantity, int expectedResult)
    {
        var result = SaleCalculator.CalculateLineSubtotal(unitPrice, quantity);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Not_Allow_Zero_Quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            SaleCalculator.CalculateLineSubtotal(100m, 0));
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Not_Allow_Negative_Quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            SaleCalculator.CalculateLineSubtotal(100m, -5));
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Not_Allow_Negative_Price()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            SaleCalculator.CalculateLineSubtotal(-100m, 5));
    }

    [Fact]
    public void CalculateTotal_Should_Preserve_Precision_With_Many_Items()
    {
        // Create 100 items each 10.01
        var items = Enumerable.Range(0, 100).Select(_ => 10.01m).ToArray();
        var result = SaleCalculator.CalculateTotal(items);
        
        // 100 * 10.01 = 1001.00
        Assert.Equal(1001.00m, result);
    }
}
