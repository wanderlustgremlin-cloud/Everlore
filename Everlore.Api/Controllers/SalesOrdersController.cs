using Everlore.Domain.Common;
using Everlore.Domain.Sales;

namespace Everlore.Api.Controllers;

public class SalesOrdersController(IRepository<SalesOrder> repo) : CrudController<SalesOrder>(repo);
