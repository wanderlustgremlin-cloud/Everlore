using Everlore.Domain.Shipping;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class ShipmentRepository(EverloreDbContext context) : Repository<Shipment>(context), IShipmentRepository;
