using Everlore.Domain.Shipping;
using FluentValidation;

namespace Everlore.Application.Validators;

public class CarrierValidator : AbstractValidator<Carrier>
{
    public CarrierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContactEmail).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
    }
}

public class ShipmentValidator : AbstractValidator<Shipment>
{
    public ShipmentValidator()
    {
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TrackingNumber).MaximumLength(200);
        RuleFor(x => x.ShipToAddress).MaximumLength(500);
    }
}
