using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

public sealed record ExpenseCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal? MonthlyBudget,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    bool IsSystem,
    bool IsActive,
    int ExpenseCount);

public sealed record ExpenseResponse(
    Guid Id,
    string ExpenseNumber,
    DateOnly ExpenseDate,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string? Description,
    string Status,
    string PaymentMethod,
    string? PaymentReference,
    DateOnly? PaymentDate,
    bool IsPaid,
    string RecordedByName,
    string? ApprovedByName,
    string? ApprovalComments,
    bool IsRecurring,
    bool IsEditable,
    IReadOnlyCollection<ExpenseAttachmentResponse> Attachments,
    IReadOnlyCollection<ExpenseApprovalEntryResponse> ApprovalTrail);

public sealed record ExpenseAttachmentResponse(
    Guid Id, string FileName, string ContentType, long SizeBytes, string UploadedByName);

public sealed record ExpenseApprovalEntryResponse(
    string FromStatus, string ToStatus, string ActedByName, string? Comments);

public sealed record ExpenseSummaryResponse(
    Guid Id,
    string ExpenseNumber,
    DateOnly ExpenseDate,
    string CategoryName,
    decimal Amount,
    string? Description,
    string Status,
    string PaymentMethod,
    bool IsPaid,
    bool IsRecurring,
    int AttachmentCount);

public sealed record RecurringExpenseResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string? Description,
    string PaymentMethod,
    int DayOfMonth,
    bool IsActive,
    int? LastGeneratedYear,
    int? LastGeneratedMonth);

public sealed record CategoryBreakdownResponse(
    Guid CategoryId,
    string CategoryName,
    decimal Total,
    decimal PercentageOfTotal,
    int ExpenseCount,
    decimal? MonthlyBudget,
    decimal? BudgetUsedPercentage);

public sealed record ProfitSummaryResponse(
    decimal Revenue,
    decimal Expenses,
    decimal Profit,
    decimal? ExpenseRatio,
    decimal? ProfitMargin,
    int OrderCount,
    decimal? AverageOrderValue);

public sealed record PeriodComparisonResponse(
    string PreviousLabel,
    decimal PreviousTotal,
    decimal CurrentTotal,
    decimal Change,
    decimal? ChangePercentage,
    string? LargestIncreaseCategory,
    decimal? LargestIncreaseAmount);

public sealed record DailyExpenseReportResponse(
    DateOnly Date,
    ProfitSummaryResponse Summary,
    IReadOnlyCollection<CategoryBreakdownResponse> Categories,
    PeriodComparisonResponse Comparison,
    IReadOnlyCollection<ExpenseSummaryResponse> Expenses,
    string? HighestCategory,
    string? LowestCategory);

public sealed record DailyFigureResponse(DateOnly Date, decimal Revenue, decimal Expenses, decimal Profit);

public sealed record WeeklyFigureResponse(
    int WeekNumber, DateOnly StartDate, DateOnly EndDate, decimal Revenue, decimal Expenses, decimal Profit);

public sealed record BudgetAlertResponse(
    Guid CategoryId,
    string CategoryName,
    decimal MonthlyBudget,
    decimal SpentThisMonth,
    decimal Remaining,
    decimal UsedPercentage,
    bool IsOverBudget);

public sealed record MonthlyExpenseReportResponse(
    int Year,
    int Month,
    string MonthLabel,
    ProfitSummaryResponse Summary,
    IReadOnlyCollection<CategoryBreakdownResponse> Categories,
    IReadOnlyCollection<DailyFigureResponse> DailyFigures,
    IReadOnlyCollection<WeeklyFigureResponse> WeeklyFigures,
    PeriodComparisonResponse Comparison,
    IReadOnlyCollection<BudgetAlertResponse> BudgetAlerts);

public sealed partial class PosApiClient
{
    public Task<HttpResponseMessage> GetExpenseCategoriesAsync(bool? isActive = null) =>
        Http.GetAsync($"{BaseUrl}/expenses/categories{(isActive.HasValue ? $"?isActive={isActive}" : string.Empty)}");

    public Task<HttpResponseMessage> CreateExpenseCategoryAsync(
        string name, string? description = null, decimal? monthlyBudget = null, Guid? parentCategoryId = null) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/expenses/categories",
            new { name, description, monthlyBudget, parentCategoryId },
            Json);

    public Task<HttpResponseMessage> UpdateExpenseCategoryAsync(
        Guid id, string name, string? description = null, decimal? monthlyBudget = null, Guid? parentCategoryId = null) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/expenses/categories/{id}",
            new { name, description, monthlyBudget, parentCategoryId },
            Json);

    public Task<HttpResponseMessage> SetExpenseCategoryActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/expenses/categories/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> DeleteExpenseCategoryAsync(Guid id) =>
        Http.DeleteAsync($"{BaseUrl}/expenses/categories/{id}");

    public Task<HttpResponseMessage> CreateExpenseAsync(
        DateOnly expenseDate,
        Guid categoryId,
        decimal amount,
        string? description = null,
        string paymentMethod = "Cash",
        string? paymentReference = null,
        DateOnly? paymentDate = null,
        bool isPaid = false,
        bool submitForApproval = false) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/expenses",
            new
            {
                expenseDate,
                categoryId,
                amount,
                description,
                paymentMethod,
                paymentReference,
                paymentDate,
                isPaid,
                submitForApproval,
            },
            Json);

    public Task<HttpResponseMessage> UpdateExpenseAsync(
        Guid id,
        DateOnly expenseDate,
        Guid categoryId,
        decimal amount,
        string? description = null,
        string paymentMethod = "Cash",
        string? paymentReference = null,
        DateOnly? paymentDate = null,
        bool isPaid = false) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/expenses/{id}",
            new { expenseDate, categoryId, amount, description, paymentMethod, paymentReference, paymentDate, isPaid },
            Json);

    public Task<HttpResponseMessage> GetExpensesAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/expenses{query}");

    public Task<HttpResponseMessage> GetExpenseAsync(Guid id) => Http.GetAsync($"{BaseUrl}/expenses/{id}");

    public Task<HttpResponseMessage> DeleteExpenseAsync(Guid id) => Http.DeleteAsync($"{BaseUrl}/expenses/{id}");

    public Task<HttpResponseMessage> SubmitExpenseAsync(Guid id, string? comments = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/expenses/{id}/submit", new { comments }, Json);

    public Task<HttpResponseMessage> ApproveExpensesAsync(string? comments = null, params Guid[] expenseIds) =>
        Http.PostAsJsonAsync($"{BaseUrl}/expenses/approve", new { expenseIds, comments }, Json);

    public Task<HttpResponseMessage> RejectExpensesAsync(string? comments = null, params Guid[] expenseIds) =>
        Http.PostAsJsonAsync($"{BaseUrl}/expenses/reject", new { expenseIds, comments }, Json);

    public Task<HttpResponseMessage> SetExpensePaidAsync(Guid id, bool isPaid, DateOnly? paymentDate = null) =>
        Http.PutAsJsonAsync($"{BaseUrl}/expenses/{id}/paid", new { isPaid, paymentDate }, Json);

    /// <summary>
    /// Awaited rather than returning the task directly: the multipart content has to outlive the
    /// request, and disposing it at the end of a non-async method closes the stream mid-flight.
    /// </summary>
    public async Task<HttpResponseMessage> AddExpenseAttachmentAsync(
        Guid id, string fileName, string contentType, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        return await Http.PostAsync($"{BaseUrl}/expenses/{id}/attachments", form);
    }

    public Task<HttpResponseMessage> GetExpenseAttachmentAsync(Guid attachmentId) =>
        Http.GetAsync($"{BaseUrl}/expenses/attachments/{attachmentId}");

    public Task<HttpResponseMessage> RemoveExpenseAttachmentAsync(Guid expenseId, Guid attachmentId) =>
        Http.DeleteAsync($"{BaseUrl}/expenses/{expenseId}/attachments/{attachmentId}");

    public Task<HttpResponseMessage> GetRecurringExpensesAsync() =>
        Http.GetAsync($"{BaseUrl}/expenses/recurring");

    public Task<HttpResponseMessage> CreateRecurringExpenseAsync(
        Guid categoryId, decimal amount, string? description, string paymentMethod, int dayOfMonth) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/expenses/recurring",
            new { categoryId, amount, description, paymentMethod, dayOfMonth },
            Json);

    public Task<HttpResponseMessage> SetRecurringExpenseActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/expenses/recurring/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> GenerateRecurringExpensesAsync() =>
        Http.PostAsync($"{BaseUrl}/expenses/recurring/generate", null);

    public Task<HttpResponseMessage> GetDailyExpenseReportAsync(DateOnly date) =>
        Http.GetAsync($"{BaseUrl}/expenses/reports/daily?date={date:yyyy-MM-dd}");

    public Task<HttpResponseMessage> GetMonthlyExpenseReportAsync(int year, int month) =>
        Http.GetAsync($"{BaseUrl}/expenses/reports/monthly?year={year}&month={month}");

    public Task<HttpResponseMessage> GetExpenseRangeReportAsync(DateOnly from, DateOnly to) =>
        Http.GetAsync($"{BaseUrl}/expenses/reports/range?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
}
