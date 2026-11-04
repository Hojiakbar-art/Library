namespace Application.Utils;

public static class FineCalculator
{
    public static decimal Calculate(DateTime dueAt, DateTime now, decimal finePerDay)
    {
        if (dueAt >= now || finePerDay <= 0)
        {
            return 0m;
        }

        var overdueDays = (now.Date - dueAt.Date).Days;
        if (overdueDays <= 0)
        {
            return 0m;
        }

        return overdueDays * finePerDay;
    }
}