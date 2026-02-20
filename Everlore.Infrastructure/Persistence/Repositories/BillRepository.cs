using Everlore.Domain.AccountsPayable;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class BillRepository(EverloreDbContext context) : Repository<Bill>(context), IBillRepository;
