using FluentAssertions;

using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class ExpenseTests
{
    private static readonly DateTime Now = new(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 8, 15);

    private static Expense NewExpense(
        decimal amount = 2500m,
        ExpensePaymentMethod method = ExpensePaymentMethod.Cash,
        string? reference = null) =>
        Expense.Create(
            1, Today, Guid.NewGuid(), amount, "Gas cylinder refill", method, reference, null, false, Guid.NewGuid());

    [Fact]
    public void Create_StartsAsADraftAndIsEditable()
    {
        var expense = NewExpense();

        expense.Status.Should().Be(ExpenseStatus.Draft);
        expense.IsEditable.Should().BeTrue();
        expense.CountsTowardsReports.Should().BeFalse("only approved expenses reach reports");
    }

    [Fact]
    public void ExpenseNumber_ReadsAsTheReferenceStaffQuote()
    {
        var expense = Expense.Create(
            7, new DateOnly(2026, 3, 4), Guid.NewGuid(), 100m, null,
            ExpensePaymentMethod.Cash, null, null, false, Guid.NewGuid());

        expense.ExpenseNumber.Should().Be("EXP-007-2026");
    }

    [Fact]
    public void Create_RejectsAnAmountOfZeroOrLess()
    {
        var act = () => NewExpense(amount: 0m);

        act.Should().Throw<ArgumentOutOfRangeException>("an expense must be greater than zero (BR-EXP-002)");
    }

    [Theory]
    [InlineData(ExpensePaymentMethod.Cheque)]
    [InlineData(ExpensePaymentMethod.Card)]
    [InlineData(ExpensePaymentMethod.BankTransfer)]
    public void Create_RequiresAReferenceForEverythingButCash(ExpensePaymentMethod method)
    {
        var act = () => NewExpense(method: method, reference: null);

        act.Should().Throw<ArgumentException>("without one the payment cannot be tied to the bank statement");
    }

    [Fact]
    public void Create_AcceptsCashWithoutAReference()
    {
        var act = () => NewExpense(method: ExpensePaymentMethod.Cash, reference: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Approve_FreezesTheExpenseAndLetsItCount()
    {
        var expense = NewExpense();
        var manager = Guid.NewGuid();

        expense.Approve(manager, Now, "Bulk approved");

        expense.Status.Should().Be(ExpenseStatus.Approved);
        expense.ApprovedByUserId.Should().Be(manager);
        expense.ApprovedAtUtc.Should().Be(Now);
        expense.ApprovalComments.Should().Be("Bulk approved");
        expense.IsEditable.Should().BeFalse();
        expense.CountsTowardsReports.Should().BeTrue();
    }

    [Fact]
    public void Approve_WorksStraightFromDraftWithoutSubmitting()
    {
        var expense = NewExpense();

        var act = () => expense.Approve(Guid.NewGuid(), Now);

        act.Should().NotThrow("a manager recording their own bills approves them directly");
    }

    [Fact]
    public void Submit_MovesADraftToPending()
    {
        var expense = NewExpense();

        expense.Submit(Guid.NewGuid(), Now);

        expense.Status.Should().Be(ExpenseStatus.Pending);
        expense.IsEditable.Should().BeTrue("a pending expense can still be corrected before a decision");
    }

    [Fact]
    public void Submit_RejectsAnExpenseThatIsNotADraft()
    {
        var expense = NewExpense();
        expense.Submit(Guid.NewGuid(), Now);

        var act = () => expense.Submit(Guid.NewGuid(), Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateDetails_IsRefusedOnceApproved()
    {
        var expense = NewExpense();
        expense.Approve(Guid.NewGuid(), Now);

        var act = () => expense.UpdateDetails(
            Today, expense.CategoryId, 9999m, "Changed", ExpensePaymentMethod.Cash, null, null, false);

        act.Should().Throw<InvalidOperationException>(
            "an approved expense has already been counted into a day's profit (EXP-036)");
    }

    [Fact]
    public void Reject_KeepsTheExpenseOnRecord()
    {
        var expense = NewExpense();

        expense.Reject(Guid.NewGuid(), Now, "Not a business cost");

        expense.Status.Should().Be(ExpenseStatus.Rejected);
        expense.ApprovalComments.Should().Be("Not a business cost");
        expense.CountsTowardsReports.Should().BeFalse();
    }

    [Fact]
    public void Approve_IsRefusedOnAnExpenseAlreadyDecided()
    {
        var expense = NewExpense();
        expense.Reject(Guid.NewGuid(), Now);

        var act = () => expense.Approve(Guid.NewGuid(), Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApprovalTrail_RecordsEveryStepInOrder()
    {
        var expense = NewExpense();
        var recorder = Guid.NewGuid();
        var manager = Guid.NewGuid();

        expense.Submit(recorder, Now, "Please review");
        expense.Approve(manager, Now.AddHours(2), "Fine");

        expense.ApprovalTrail.Should().HaveCount(2);

        var submitted = expense.ApprovalTrail.First();
        submitted.FromStatus.Should().Be(ExpenseStatus.Draft);
        submitted.ToStatus.Should().Be(ExpenseStatus.Pending);
        submitted.ActedByUserId.Should().Be(recorder);
        submitted.Comments.Should().Be("Please review");

        var approved = expense.ApprovalTrail.Last();
        approved.FromStatus.Should().Be(ExpenseStatus.Pending);
        approved.ToStatus.Should().Be(ExpenseStatus.Approved);
        approved.ActedByUserId.Should().Be(manager);
    }

    [Fact]
    public void SetPaid_StaysAvailableAfterApproval()
    {
        var expense = NewExpense(method: ExpensePaymentMethod.Cheque, reference: "CHK-001");
        expense.Approve(Guid.NewGuid(), Now);

        var act = () => expense.SetPaid(true, new DateOnly(2026, 8, 20));

        act.Should().NotThrow("a cheque clearing later has not changed the expense, only its settlement");
        expense.IsPaid.Should().BeTrue();
        expense.PaymentDate.Should().Be(new DateOnly(2026, 8, 20));
    }

    [Fact]
    public void AddAttachment_IsRefusedOnceApproved()
    {
        var expense = NewExpense();
        expense.Approve(Guid.NewGuid(), Now);

        var act = () => expense.AddAttachment("receipt.jpg", "2026/08/x.jpg", "image/jpeg", 1024, Guid.NewGuid(), Now);

        act.Should().Throw<InvalidOperationException>();
    }
}

public class ExpenseCategoryTests
{
    [Fact]
    public void Create_StartsActiveAndCustom()
    {
        var category = ExpenseCategory.Create("Cleaning", "Cleaning supplies", 10_000m);

        category.IsActive.Should().BeTrue();
        category.IsSystem.Should().BeFalse();
        category.MonthlyBudget.Should().Be(10_000m);
    }

    [Fact]
    public void Create_RejectsANegativeBudget()
    {
        var act = () => ExpenseCategory.Create("Cleaning", null, -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_AllowsNoBudget()
    {
        var category = ExpenseCategory.Create("Cleaning");

        category.MonthlyBudget.Should().BeNull("an unbudgeted category simply raises no alerts");
    }
}

public class RecurringExpenseTests
{
    private static RecurringExpense NewRecurring(int dayOfMonth = 1) =>
        RecurringExpense.Create(Guid.NewGuid(), 50_000m, "Monthly rent", ExpensePaymentMethod.BankTransfer, dayOfMonth);

    [Fact]
    public void IsDueFor_IsTrueBeforeItHasEverRun()
    {
        NewRecurring().IsDueFor(2026, 8).Should().BeTrue();
    }

    [Fact]
    public void IsDueFor_IsFalseForAMonthAlreadyGenerated()
    {
        var recurring = NewRecurring();
        recurring.MarkGenerated(2026, 8);

        recurring.IsDueFor(2026, 8).Should().BeFalse("opening the screen twice must not create two rents");
        recurring.IsDueFor(2026, 9).Should().BeTrue();
        recurring.IsDueFor(2026, 7).Should().BeFalse("a month already past is not owed again");
    }

    [Fact]
    public void IsDueFor_RollsOverTheYearCorrectly()
    {
        var recurring = NewRecurring();
        recurring.MarkGenerated(2026, 12);

        recurring.IsDueFor(2027, 1).Should().BeTrue();
        recurring.IsDueFor(2026, 12).Should().BeFalse();
    }

    [Fact]
    public void IsDueFor_IsFalseWhenStopped()
    {
        var recurring = NewRecurring();
        recurring.Deactivate();

        recurring.IsDueFor(2026, 8).Should().BeFalse();
    }

    [Fact]
    public void DateFor_ClampsToTheLengthOfAShortMonth()
    {
        var recurring = NewRecurring(dayOfMonth: 31);

        recurring.DateFor(2026, 2).Should().Be(new DateOnly(2026, 2, 28),
            "rent set to the 31st must still land in February rather than being skipped");
        recurring.DateFor(2026, 8).Should().Be(new DateOnly(2026, 8, 31));
    }

    [Fact]
    public void Create_RejectsADayOutsideTheMonth()
    {
        var act = () => NewRecurring(dayOfMonth: 32);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
