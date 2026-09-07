namespace RestaurantPOS.Application.Expenses.Dtos;

/// <summary>One category's share of a period's spending (EXP-016, EXP-019).</summary>
public sealed record CategoryBreakdownDto(
    Guid CategoryId,
    string CategoryName,
    decimal Total,
    /// <summary>Share of the period's expenses, 0-100.</summary>
    decimal PercentageOfTotal,
    int ExpenseCount,
    decimal? MonthlyBudget,
    /// <summary>How much of the monthly budget this has used, 0-100+. Null when unbudgeted.</summary>
    decimal? BudgetUsedPercentage);

/// <summary>One day's figures, used for both the daily report and the monthly trend line.</summary>
public sealed record DailyFigureDto(DateOnly Date, decimal Revenue, decimal Expenses, decimal Profit);

/// <summary>
/// Revenue against expenses for a period (EXP-026, EXP-027).
/// </summary>
/// <param name="Revenue">Taken from settled bills at the till.</param>
/// <param name="Expenses">Approved expenses only (BR-EXP-010).</param>
/// <param name="ExpenseRatio">Expenses as a share of revenue, 0-100. Null when nothing was sold.</param>
/// <param name="ProfitMargin">Profit as a share of revenue, 0-100. Null when nothing was sold.</param>
/// <param name="OrderCount">Bills settled in the period.</param>
/// <param name="AverageOrderValue">Revenue divided by <paramref name="OrderCount"/>. Null when nothing was sold.</param>
public sealed record ProfitSummaryDto(
    decimal Revenue,
    decimal Expenses,
    decimal Profit,
    decimal? ExpenseRatio,
    decimal? ProfitMargin,
    int OrderCount,
    decimal? AverageOrderValue);

/// <summary>How a period compares with the one before it (EXP-025).</summary>
public sealed record PeriodComparisonDto(
    string PreviousLabel,
    decimal PreviousTotal,
    decimal CurrentTotal,
    decimal Change,
    /// <summary>Percentage change. Null when the previous period had no spending to compare against.</summary>
    decimal? ChangePercentage,
    /// <summary>The category that moved the most, which is where an owner will want to look first.</summary>
    string? LargestIncreaseCategory,
    decimal? LargestIncreaseAmount);

/// <summary>A day's expense report (EXP-028).</summary>
public sealed record DailyExpenseReportDto(
    DateOnly Date,
    ProfitSummaryDto Summary,
    IReadOnlyCollection<CategoryBreakdownDto> Categories,
    PeriodComparisonDto Comparison,
    IReadOnlyCollection<ExpenseSummaryDto> Expenses,
    string? HighestCategory,
    string? LowestCategory);

/// <summary>A month's expense report (EXP-029), including its daily trend and weekly split.</summary>
public sealed record MonthlyExpenseReportDto(
    int Year,
    int Month,
    string MonthLabel,
    ProfitSummaryDto Summary,
    IReadOnlyCollection<CategoryBreakdownDto> Categories,
    IReadOnlyCollection<DailyFigureDto> DailyFigures,
    IReadOnlyCollection<WeeklyFigureDto> WeeklyFigures,
    PeriodComparisonDto Comparison,
    IReadOnlyCollection<BudgetAlertDto> BudgetAlerts);

public sealed record WeeklyFigureDto(
    int WeekNumber, DateOnly StartDate, DateOnly EndDate, decimal Revenue, decimal Expenses, decimal Profit);

/// <summary>
/// A category at or over its monthly budget (EXP-038, BR-EXP-016).
/// </summary>
/// <param name="IsOverBudget">True once spending has passed the limit rather than merely neared it.</param>
public sealed record BudgetAlertDto(
    Guid CategoryId,
    string CategoryName,
    decimal MonthlyBudget,
    decimal SpentThisMonth,
    decimal Remaining,
    decimal UsedPercentage,
    bool IsOverBudget);

/// <summary>A free-range summary used by the analysis dashboard and the year-to-date view.</summary>
public sealed record ExpenseRangeReportDto(
    DateOnly From,
    DateOnly To,
    ProfitSummaryDto Summary,
    IReadOnlyCollection<CategoryBreakdownDto> Categories,
    IReadOnlyCollection<DailyFigureDto> DailyFigures);
