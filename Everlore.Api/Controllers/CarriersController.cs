using Everlore.Domain.Common;
using Everlore.Domain.Shipping;

namespace Everlore.Api.Controllers;

public class CarriersController(IRepository<Carrier> repo) : CrudController<Carrier>(repo);
