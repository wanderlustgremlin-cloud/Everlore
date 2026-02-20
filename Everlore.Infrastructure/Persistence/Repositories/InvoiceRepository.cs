using Everlore.Domain.AccountsReceivable;

namespace Everlore.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(EverloreDbContext context) : Repository<Invoice>(context), IInvoiceRepository;
