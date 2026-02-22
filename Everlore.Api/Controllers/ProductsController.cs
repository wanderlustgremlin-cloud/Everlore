using Everlore.Domain.Common;
using Everlore.Domain.Inventory;

namespace Everlore.Api.Controllers;

public class ProductsController(IRepository<Product> repo) : CrudController<Product>(repo);
