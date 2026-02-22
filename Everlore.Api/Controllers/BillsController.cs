using Everlore.Domain.AccountsPayable;
using Everlore.Domain.Common;

namespace Everlore.Api.Controllers;

public class BillsController(IRepository<Bill> repo) : CrudController<Bill>(repo);
