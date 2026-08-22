using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ExpenseCategories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(ExpenseCategory.NameMaxLength);
        builder.Property(c => c.Description).HasMaxLength(ExpenseCategory.DescriptionMaxLength);

        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasOne<ExpenseCategory>()
            .WithMany()
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Expenses");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Status).IsRequired().HasConversion<int>();
        builder.Property(e => e.PaymentMethod).IsRequired().HasConversion<int>();
        builder.Property(e => e.Amount).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(Expense.DescriptionMaxLength);
        builder.Property(e => e.PaymentReference).HasMaxLength(Expense.ReferenceMaxLength);
        builder.Property(e => e.ApprovalComments).HasMaxLength(ExpenseApprovalEntry.CommentsMaxLength);

        // The yearly sequence must be unique within its year (BR-EXP-003).
        builder.HasIndex(e => new { e.Year, e.Number }).IsUnique();
        builder.HasIndex(e => e.ExpenseDate);
        builder.HasIndex(e => new { e.Status, e.ExpenseDate });

        builder.HasOne<ExpenseCategory>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var navigation in new[] { nameof(Expense.ApprovalTrail), nameof(Expense.Attachments) })
        {
            builder.Metadata.FindNavigation(navigation)!.SetPropertyAccessMode(PropertyAccessMode.Field);
        }

        builder.HasMany(e => e.ApprovalTrail)
            .WithOne()
            .HasForeignKey(t => t.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
            .WithOne()
            .HasForeignKey(a => a.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Derived from Number and Year, so it is composed on read rather than stored twice.
        builder.Ignore(e => e.ExpenseNumber);
        builder.Ignore(e => e.IsEditable);
        builder.Ignore(e => e.CountsTowardsReports);
    }
}

internal sealed class ExpenseApprovalEntryConfiguration : IEntityTypeConfiguration<ExpenseApprovalEntry>
{
    public void Configure(EntityTypeBuilder<ExpenseApprovalEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ExpenseApprovalEntries");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.FromStatus).IsRequired().HasConversion<int>();
        builder.Property(t => t.ToStatus).IsRequired().HasConversion<int>();
        builder.Property(t => t.Comments).HasMaxLength(ExpenseApprovalEntry.CommentsMaxLength);

        builder.HasIndex(t => t.ExpenseId);
    }
}

internal sealed class ExpenseAttachmentConfiguration : IEntityTypeConfiguration<ExpenseAttachment>
{
    public void Configure(EntityTypeBuilder<ExpenseAttachment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ExpenseAttachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(ExpenseAttachment.FileNameMaxLength);
        builder.Property(a => a.StoredPath).IsRequired().HasMaxLength(ExpenseAttachment.StoredPathMaxLength);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(ExpenseAttachment.ContentTypeMaxLength);

        builder.HasIndex(a => a.ExpenseId);
    }
}

internal sealed class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RecurringExpenses");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.PaymentMethod).IsRequired().HasConversion<int>();
        builder.Property(r => r.Amount).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(RecurringExpense.DescriptionMaxLength);

        builder.HasOne<ExpenseCategory>()
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
