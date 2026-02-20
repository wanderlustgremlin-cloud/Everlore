using Everlore.Domain.AccountsReceivable;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class CustomerRepository(EverloreDbContext context) : Repository<Customer>(context), ICustomerRepository;
