using Everlore.Domain.AccountsPayable;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class VendorRepository(EverloreDbContext context) : Repository<Vendor>(context), IVendorRepository;
