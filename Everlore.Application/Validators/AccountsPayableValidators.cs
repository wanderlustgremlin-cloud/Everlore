using Everlore.Domain.AccountsPayable;
using FluentValidation;

namespace Everlore.Application.Validators;

public class VendorValidator : AbstractValidator<Vendor>
{
    public VendorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public class BillValidator : AbstractValidator<Bill>
{
    public BillValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.BillNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0);
    }
}
