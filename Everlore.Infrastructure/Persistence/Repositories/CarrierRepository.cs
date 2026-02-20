using Everlore.Domain.Shipping;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class CarrierRepository(EverloreDbContext context) : Repository<Carrier>(context), ICarrierRepository;
