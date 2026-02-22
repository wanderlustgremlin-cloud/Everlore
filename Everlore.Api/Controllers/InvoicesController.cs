using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Common;

namespace Everlore.Api.Controllers;

public class InvoicesController(IRepository<Invoice> repo) : CrudController<Invoice>(repo);
