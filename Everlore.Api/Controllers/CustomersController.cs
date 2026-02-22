using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Common;

namespace Everlore.Api.Controllers;

public class CustomersController(IRepository<Customer> repo) : CrudController<Customer>(repo);
