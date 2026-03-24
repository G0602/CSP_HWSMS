namespace HSMS.Application.Services;

public static class SaleCalculator
{
    public static decimal CalculateLineSubtotal(decimal unitPrice, int quantity)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return unitPrice * quantity;
    }

    public static decimal CalculateTotal(IEnumerable<decimal> lineSubtotals)
    {
        return lineSubtotals.Sum();
    }
}
