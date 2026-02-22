using Everlore.Domain.Inventory;
using FluentValidation;

namespace Everlore.Application.Validators;

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(50);
    }
}

public class WarehouseValidator : AbstractValidator<Warehouse>
{
    public WarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
