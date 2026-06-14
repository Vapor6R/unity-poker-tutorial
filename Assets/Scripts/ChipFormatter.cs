public static class ChipFormatter
{
    public static string Format(long amount)
    {
        if (amount >= 1_000_000_000_000)
            return (amount / 1_000_000_000_000f).ToString("0.##") + "T";
        if (amount >= 1_000_000_000)
            return (amount / 1_000_000_000f).ToString("0.##") + "B";
        if (amount >= 1_000_000)
            return (amount / 1_000_000f).ToString("0.##") + "M";
        if (amount >= 1_000)
            return (amount / 1_000f).ToString("0.##") + "K";
        return amount.ToString("N0");
    }
}