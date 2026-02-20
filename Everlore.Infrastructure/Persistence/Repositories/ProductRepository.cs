using Everlore.Domain.Inventory;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class ProductRepository(EverloreDbContext context) : Repository<Product>(context), IProductRepository;
