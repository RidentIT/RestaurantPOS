using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A goods supplier: who a Goods Received Note credits stock to, who a Purchase Order is sent
/// to, and whose prices and payment history the Supplier Management module tracks.
/// </summary>
public sealed class Supplier : BaseEntity
{
    public const int NameMaxLength = 150;
    public const int ContactNameMaxLength = 150;
    public const int PhoneMaxLength = 30;
    public const int EmailMaxLength = 200;
    public const int AddressMaxLength = 300;

    // EF Core materialisation.
    private Supplier()
    {
    }

    private Supplier(
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        int paymentTermsDays,
        decimal? creditLimit,
        int? leadTimeDays)
    {
        Name = NormaliseName(name);
        ContactName = NormaliseOptional(contactName, ContactNameMaxLength, nameof(contactName));
        Phone = NormaliseOptional(phone, PhoneMaxLength, nameof(phone));
        Email = NormaliseOptional(email, EmailMaxLength, nameof(email))?.ToLowerInvariant();
        Address = NormaliseOptional(address, AddressMaxLength, nameof(address));
        PaymentTermsDays = ValidateNonNegative(paymentTermsDays, nameof(paymentTermsDays));
        CreditLimit = creditLimit is null ? null : ValidateNonNegative(creditLimit.Value, nameof(creditLimit));
        LeadTimeDays = leadTimeDays is null ? null : ValidateNonNegative(leadTimeDays.Value, nameof(leadTimeDays));
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string? ContactName { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    /// <summary>Days after delivery payment is due. 0 means cash on delivery.</summary>
    public int PaymentTermsDays { get; private set; }

    /// <summary>Maximum outstanding balance this supplier extends. Null means no limit is tracked.</summary>
    public decimal? CreditLimit { get; private set; }

    /// <summary>Typical days between placing an order and delivery. Null means not yet known.</summary>
    public int? LeadTimeDays { get; private set; }

    public bool IsActive { get; private set; }

    public static Supplier Create(
        string name,
        string? contactName = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        int paymentTermsDays = 0,
        decimal? creditLimit = null,
        int? leadTimeDays = null) =>
        new(name, contactName, phone, email, address, paymentTermsDays, creditLimit, leadTimeDays);

    public void UpdateDetails(
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        int paymentTermsDays,
        decimal? creditLimit,
        int? leadTimeDays)
    {
        Name = NormaliseName(name);
        ContactName = NormaliseOptional(contactName, ContactNameMaxLength, nameof(contactName));
        Phone = NormaliseOptional(phone, PhoneMaxLength, nameof(phone));
        Email = NormaliseOptional(email, EmailMaxLength, nameof(email))?.ToLowerInvariant();
        Address = NormaliseOptional(address, AddressMaxLength, nameof(address));
        PaymentTermsDays = ValidateNonNegative(paymentTermsDays, nameof(paymentTermsDays));
        CreditLimit = creditLimit is null ? null : ValidateNonNegative(creditLimit.Value, nameof(creditLimit));
        LeadTimeDays = leadTimeDays is null ? null : ValidateNonNegative(leadTimeDays.Value, nameof(leadTimeDays));
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string NormaliseName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();

        return trimmed.Length > NameMaxLength
            ? throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name))
            : trimmed;
    }

    private static string? NormaliseOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length > maxLength
            ? throw new ArgumentException($"'{paramName}' cannot exceed {maxLength} characters.", paramName)
            : trimmed;
    }

    private static int ValidateNonNegative(int value, string paramName) =>
        value >= 0 ? value : throw new ArgumentOutOfRangeException(paramName, value, "Cannot be negative.");

    private static decimal ValidateNonNegative(decimal value, string paramName) =>
        value >= 0 ? value : throw new ArgumentOutOfRangeException(paramName, value, "Cannot be negative.");
}