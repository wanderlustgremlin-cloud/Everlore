using Everlore.Domain.Sales;
using FluentValidation;

namespace Everlore.Application.Validators;

public class SalesOrderValidator : AbstractValidator<SalesOrder>
{
    public SalesOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
    }
}
