using Everlore.Domain.Sales;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class SalesOrderRepository(EverloreDbContext context) : Repository<SalesOrder>(context), ISalesOrderRepository;
