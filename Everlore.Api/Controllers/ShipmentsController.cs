using Everlore.Domain.Common;
using Everlore.Domain.Shipping;

namespace Everlore.Api.Controllers;

public class ShipmentsController(IRepository<Shipment> repo) : CrudController<Shipment>(repo);
