using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

public static class SupplierMappings
{
    public static SupplierDto ToDto(this Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        return new SupplierDto(
            supplier.Id,
            supplier.Name,
            supplier.ContactName,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.PaymentTermsDays,
            supplier.CreditLimit,
            supplier.LeadTimeDays,
            supplier.IsActive,
            supplier.CreatedAtUtc);
    }
}