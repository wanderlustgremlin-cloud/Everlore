using Everlore.Domain.Inventory;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class WarehouseRepository(EverloreDbContext context) : Repository<Warehouse>(context), IWarehouseRepository;
