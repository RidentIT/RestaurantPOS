using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Expenses;

/// <summary>
/// Covers expense recording, approval and categories, including the day-of-five-expenses
/// walkthrough from the requirements.
/// </summary>
public class ExpenseManagementTests : IntegrationTestBase
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private async Task<List<ExpenseCategoryResponse>> GetCategoriesAsync()
    {
        var response = await Client.GetExpenseCategoriesAsync();
        response.EnsureSuccessStatusCode();

        return await PosApiClient.ReadAsync<List<ExpenseCategoryResponse>>(response);
    }

    private async Task<Guid> CategoryIdAsync(string name)
    {
        var categories = await GetCategoriesAsync();

        return categories.Single(c => c.Name == name).Id;
    }

    private async Task<ExpenseResponse> RecordAsync(
        string categoryName,
        decimal amount,
        string? description = null,
        string method = "Cash",
        string? reference = null)
    {
        var response = await Client.CreateExpenseAsync(
            Today, await CategoryIdAsync(categoryName), amount, description, method, reference);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await PosApiClient.ReadAsync<ExpenseResponse>(response);
    }

    [Fact]
    public async Task AFreshInstall_ComesWithTheBuiltInCategories()
    {
        await SignInAsAdminAsync();

        var categories = await GetCategoriesAsync();

        categories.Should().Contain(c => c.Name == "Gas/LPG" && c.IsSystem);
        categories.Should().Contain(c => c.Name == "Electricity" && c.MonthlyBudget == 35_000m);
        categories.Should().Contain(c => c.Name == "Staff Meals");
        categories.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task RecordingAnExpense_NumbersItAndLeavesItADraft()
    {
        await SignInAsAdminAsync();

        var expense = await RecordAsync("Gas/LPG", 2500m, "Gas cylinder refill");

        expense.ExpenseNumber.Should().Be($"EXP-001-{Today.Year}");
        expense.Status.Should().Be("Draft");
        expense.Amount.Should().Be(2500m);
        expense.CategoryName.Should().Be("Gas/LPG");
        expense.IsEditable.Should().BeTrue();
        expense.RecordedByName.Should().Be("System Administrator");
    }

    [Fact]
    public async Task ExpenseNumbers_RunInSequenceWithinTheYear()
    {
        await SignInAsAdminAsync();

        var first = await RecordAsync("Gas/LPG", 2500m);
        var second = await RecordAsync("Electricity", 3500m, method: "Cheque", reference: "CHK-001");

        first.ExpenseNumber.Should().Be($"EXP-001-{Today.Year}");
        second.ExpenseNumber.Should().Be($"EXP-002-{Today.Year}");
    }

    [Fact]
    public async Task AnExpenseCannotBeDatedInTheFuture()
    {
        await SignInAsAdminAsync();

        var response = await Client.CreateExpenseAsync(
            Today.AddDays(1), await CategoryIdAsync("Gas/LPG"), 500m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Expense.FutureDate");
    }

    [Fact]
    public async Task BackdatingAnExpense_IsAllowed()
    {
        await SignInAsAdminAsync();

        var response = await Client.CreateExpenseAsync(
            Today.AddDays(-3), await CategoryIdAsync("Gas/LPG"), 500m, "Entered late");

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "yesterday's bills are keyed in this morning (BR-EXP-017)");
    }

    [Fact]
    public async Task ANonCashPayment_RequiresAReference()
    {
        await SignInAsAdminAsync();
        var categoryId = await CategoryIdAsync("Electricity");

        var withoutReference = await Client.CreateExpenseAsync(
            Today, categoryId, 3500m, "Jan bill", "Cheque");
        withoutReference.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var withReference = await Client.CreateExpenseAsync(
            Today, categoryId, 3500m, "Jan bill", "Cheque", "CHK-001");
        withReference.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AnExpenseCanBeCorrectedBeforeApprovalButNotAfter()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Gas/LPG", 2500m);
        var categoryId = await CategoryIdAsync("Gas/LPG");

        var corrected = await Client.UpdateExpenseAsync(expense.Id, Today, categoryId, 2600m, "Corrected");
        corrected.EnsureSuccessStatusCode();
        (await PosApiClient.ReadAsync<ExpenseResponse>(corrected)).Amount.Should().Be(2600m);

        (await Client.ApproveExpensesAsync(null, expense.Id)).EnsureSuccessStatusCode();

        var afterApproval = await Client.UpdateExpenseAsync(expense.Id, Today, categoryId, 9999m);
        afterApproval.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(afterApproval)).Should().Be("Expense.NotEditable");
    }

    [Fact]
    public async Task AManagerCanApproveADraftDirectly()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Gas/LPG", 2500m);

        var approved = await Client.ApproveExpensesAsync("Bulk approved", expense.Id);

        approved.EnsureSuccessStatusCode();
        var results = await PosApiClient.ReadAsync<List<ExpenseResponse>>(approved);
        results.Should().ContainSingle();
        results[0].Status.Should().Be("Approved");
        results[0].ApprovedByName.Should().Be("System Administrator");
        results[0].ApprovalComments.Should().Be("Bulk approved");
    }

    [Fact]
    public async Task SubmittingThenApproving_RecordsTheWholeTrail()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Staff Meals", 800m, "Lunch for 5 staff");

        (await Client.SubmitExpenseAsync(expense.Id, "Please review")).EnsureSuccessStatusCode();
        (await Client.ApproveExpensesAsync("Fine", expense.Id)).EnsureSuccessStatusCode();

        var loaded = await PosApiClient.ReadAsync<ExpenseResponse>(await Client.GetExpenseAsync(expense.Id));

        loaded.Status.Should().Be("Approved");
        loaded.ApprovalTrail.Should().HaveCount(2);
        loaded.ApprovalTrail.First().ToStatus.Should().Be("Pending");
        loaded.ApprovalTrail.First().Comments.Should().Be("Please review");
        loaded.ApprovalTrail.Last().ToStatus.Should().Be("Approved");
    }

    [Fact]
    public async Task RejectingAnExpense_KeepsItOnRecord()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Miscellaneous", 500m);

        (await Client.RejectExpensesAsync("Not a business cost", expense.Id)).EnsureSuccessStatusCode();

        var loaded = await PosApiClient.ReadAsync<ExpenseResponse>(await Client.GetExpenseAsync(expense.Id));
        loaded.Status.Should().Be("Rejected");
        loaded.ApprovalComments.Should().Be("Not a business cost");
    }

    [Fact]
    public async Task ApprovingABatchIsAllOrNothing()
    {
        await SignInAsAdminAsync();
        var first = await RecordAsync("Gas/LPG", 2500m);
        var second = await RecordAsync("Staff Meals", 800m);

        (await Client.ApproveExpensesAsync(null, first.Id)).EnsureSuccessStatusCode();

        // The batch includes one already decided, so none of it should apply.
        var batch = await Client.ApproveExpensesAsync(null, first.Id, second.Id);

        batch.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(batch)).Should().Be("Expense.AlreadyDecided");

        var reloaded = await PosApiClient.ReadAsync<ExpenseResponse>(await Client.GetExpenseAsync(second.Id));
        reloaded.Status.Should().Be("Draft", "a partial bulk approval would leave the manager guessing");
    }

    [Fact]
    public async Task ADecidedExpenseCannotBeDeleted()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Gas/LPG", 2500m);

        var draftDelete = await Client.DeleteExpenseAsync(expense.Id);
        draftDelete.EnsureSuccessStatusCode();

        var second = await RecordAsync("Gas/LPG", 900m);
        (await Client.RejectExpensesAsync(null, second.Id)).EnsureSuccessStatusCode();

        var afterReject = await Client.DeleteExpenseAsync(second.Id);
        afterReject.StatusCode.Should().Be(HttpStatusCode.Conflict, "a rejection is a decision worth keeping");
    }

    [Fact]
    public async Task ExpensesCanBeFilteredAndSearched()
    {
        await SignInAsAdminAsync();
        await RecordAsync("Gas/LPG", 2500m, "Gas cylinder refill");
        await RecordAsync("Electricity", 3500m, "Jan monthly bill", "Cheque", "12345-INV-2024");
        var meals = await RecordAsync("Staff Meals", 800m, "Lunch for 5 staff");
        (await Client.ApproveExpensesAsync(null, meals.Id)).EnsureSuccessStatusCode();

        var byCategory = await PosApiClient.ReadAsync<List<ExpenseSummaryResponse>>(
            await Client.GetExpensesAsync($"?categoryId={await CategoryIdAsync("Gas/LPG")}"));
        byCategory.Should().ContainSingle().Which.CategoryName.Should().Be("Gas/LPG");

        var byMethod = await PosApiClient.ReadAsync<List<ExpenseSummaryResponse>>(
            await Client.GetExpensesAsync("?paymentMethod=Cheque"));
        byMethod.Should().ContainSingle().Which.PaymentMethod.Should().Be("Cheque");

        var byStatus = await PosApiClient.ReadAsync<List<ExpenseSummaryResponse>>(
            await Client.GetExpensesAsync("?status=Approved"));
        byStatus.Should().ContainSingle().Which.CategoryName.Should().Be("Staff Meals");

        var byReference = await PosApiClient.ReadAsync<List<ExpenseSummaryResponse>>(
            await Client.GetExpensesAsync("?search=12345-INV"));
        byReference.Should().ContainSingle().Which.CategoryName.Should().Be("Electricity");

        var byDescription = await PosApiClient.ReadAsync<List<ExpenseSummaryResponse>>(
            await Client.GetExpensesAsync("?search=cylinder"));
        byDescription.Should().ContainSingle();
    }

    [Fact]
    public async Task ACustomCategoryCanBeAddedAndNested()
    {
        await SignInAsAdminAsync();

        var parentId = await CategoryIdAsync("Miscellaneous");

        var created = await Client.CreateExpenseCategoryAsync("Cleaning Supplies", "Mops and detergent", 8000m, parentId);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await PosApiClient.ReadAsync<ExpenseCategoryResponse>(created);
        category.ParentCategoryName.Should().Be("Miscellaneous");
        category.IsSystem.Should().BeFalse();
        category.MonthlyBudget.Should().Be(8000m);
    }

    [Fact]
    public async Task SubcategoriesCannotNestMoreThanOneLevel()
    {
        await SignInAsAdminAsync();
        var parentId = await CategoryIdAsync("Miscellaneous");

        var child = await PosApiClient.ReadAsync<ExpenseCategoryResponse>(
            await Client.CreateExpenseCategoryAsync("Cleaning", null, null, parentId));

        var grandchild = await Client.CreateExpenseCategoryAsync("Mops", null, null, child.Id);

        grandchild.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(grandchild)).Should().Be("ExpenseCategory.NestingTooDeep");
    }

    [Fact]
    public async Task FilteringByAParentCategory_IncludesItsChildren()
    {
        await SignInAsAdminAsync();
        var parentId = await CategoryIdAsync("Miscellaneous");
        var child = await PosApiClient.ReadAsync<ExpenseCategoryResponse>(
            await Client.CreateExpenseCategoryAsync("Cleaning", null, null, parentId));

        await Client.CreateExpenseAsync(Today, child.Id, 500m, "Floor cleaner");
        await Client.CreateExpenseAsync(Today, parentId, 300m, "Other");

        var results = await PosApiClient.ReadAsync<List<ExpenseSummaryResponse>>(
            await Client.GetExpensesAsync($"?categoryId={parentId}"));

        results.Should().HaveCount(2, "a parent totals whatever hangs beneath it");
    }

    [Fact]
    public async Task ABuiltInCategoryCannotBeDeleted()
    {
        await SignInAsAdminAsync();

        var response = await Client.DeleteExpenseCategoryAsync(await CategoryIdAsync("Gas/LPG"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("ExpenseCategory.IsSystem");
    }

    [Fact]
    public async Task ACategoryWithHistoryCannotBeDeleted()
    {
        await SignInAsAdminAsync();
        var category = await PosApiClient.ReadAsync<ExpenseCategoryResponse>(
            await Client.CreateExpenseCategoryAsync("Cleaning"));

        await Client.CreateExpenseAsync(Today, category.Id, 500m);

        var response = await Client.DeleteExpenseCategoryAsync(category.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("ExpenseCategory.InUse");
    }

    [Fact]
    public async Task AReceiptCanBeAttachedAndReadBack()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Staff Meals", 800m);
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4 };

        var uploaded = await Client.AddExpenseAttachmentAsync(expense.Id, "receipt.jpg", "image/jpeg", content);

        uploaded.EnsureSuccessStatusCode();
        var withAttachment = await PosApiClient.ReadAsync<ExpenseResponse>(uploaded);
        withAttachment.Attachments.Should().ContainSingle();

        var attachment = withAttachment.Attachments.Single();
        attachment.FileName.Should().Be("receipt.jpg");
        attachment.SizeBytes.Should().Be(content.Length);

        var downloaded = await Client.GetExpenseAttachmentAsync(attachment.Id);
        downloaded.EnsureSuccessStatusCode();
        (await downloaded.Content.ReadAsByteArrayAsync()).Should().Equal(content);
    }

    [Fact]
    public async Task AnExecutableCannotBeAttachedAsAReceipt()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Staff Meals", 800m);

        var response = await Client.AddExpenseAttachmentAsync(
            expense.Id, "payload.exe", "application/x-msdownload", [1, 2, 3]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Expense.AttachmentTypeNotAllowed");
    }

    [Fact]
    public async Task AnExpenseCanBeMarkedPaidAfterApproval()
    {
        await SignInAsAdminAsync();
        var expense = await RecordAsync("Electricity", 3500m, "Jan bill", "Cheque", "CHK-001");
        (await Client.ApproveExpensesAsync(null, expense.Id)).EnsureSuccessStatusCode();

        var paid = await Client.SetExpensePaidAsync(expense.Id, true, Today);

        paid.EnsureSuccessStatusCode();
        var loaded = await PosApiClient.ReadAsync<ExpenseResponse>(paid);
        loaded.IsPaid.Should().BeTrue("a cheque clearing later has not changed the expense itself");
        loaded.PaymentDate.Should().Be(Today);
    }

    [Fact]
    public async Task StaffWithoutExpensesManagement_CannotReachExpenses()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        (await staff.GetExpensesAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.GetExpenseCategoriesAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
