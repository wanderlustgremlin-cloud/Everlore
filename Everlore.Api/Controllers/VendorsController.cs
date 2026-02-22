using Everlore.Domain.AccountsPayable;
using Everlore.Domain.Common;

namespace Everlore.Api.Controllers;

public class VendorsController(IRepository<Vendor> repo) : CrudController<Vendor>(repo);
