using Everlore.Domain.Common;
using Everlore.Domain.Inventory;

namespace Everlore.Api.Controllers;

public class WarehousesController(IRepository<Warehouse> repo) : CrudController<Warehouse>(repo);
