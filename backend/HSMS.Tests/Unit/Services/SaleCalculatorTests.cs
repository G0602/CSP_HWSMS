using HSMS.Application.Services;

namespace HSMS.Tests;

public class SaleCalculatorTests
{
    [Fact]
    public void CalculateLineSubtotal_Should_Multiply_UnitPrice_And_Quantity()
    {
        decimal lineSubtotal = SaleCalculator.CalculateLineSubtotal(1250.50m, 3);

        Assert.Equal(3751.50m, lineSubtotal);
    }

    [Fact]
    public void CalculateTotal_Should_Return_Sum_Of_LineSubtotals()
    {
        decimal total = SaleCalculator.CalculateTotal(new[] { 100m, 250.75m, 399.25m });

        Assert.Equal(750m, total);
    }

    [Fact]
    public void CalculateLineSubtotal_Should_Throw_When_Quantity_Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SaleCalculator.CalculateLineSubtotal(100m, 0));
    }
}
