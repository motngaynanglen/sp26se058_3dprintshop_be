using Microsoft.EntityFrameworkCore.Infrastructure;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    //DbSet<TodoList> TodoLists { get; }

    //DbSet<TodoItem> TodoItems { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Manager> Managers { get; }
    DbSet<Staff> Staffs { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<DesignTemplate> DesignTemplates { get; }
    DbSet<Domain.Entities.DesignVariant> DesignVariants { get; }
    DbSet<ConceptTag> ConceptTags { get; }
    DbSet<DesignTag> DesignTags { get; }
    DbSet<Material> Materials { get; }
    DbSet<MaterialPriceHistory> MaterialPriceHistories { get; }
    DbSet<MaterialInventoryTransaction> MaterialInventoryTransactions { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }
    DbSet<DesignWork> DesignWorks { get; }
    DbSet<DesignLog> DesignLogs { get; }
    DbSet<DesignVersionHistory> DesignVersionHistorys { get; }
    DbSet<TechnicalDraft> TechnicalDrafts { get; }
    DbSet<ShippingAddress> ShippingAddresses { get; }
    DbSet<Shipment> Shipments { get; }
    DbSet<Feedback> Feedbacks { get; }
    DbSet<FeedbackImage> FeedbackImages { get; }
    DbSet<ServiceOption> ServiceOptions { get; }
    DbSet<ServicePackage> ServicePackages { get; }
    DbSet<PackageOption> PackageOptions { get; }
    DbSet<ServiceSelection> ServiceSelections { get; }
    DbSet<ServiceSelectionOption> ServiceSelectionOptions { get; }

    //DatabaseFacade Database { get; }

    void HardRemove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
