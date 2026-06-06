using System.Reflection;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Common;
using System.Linq.Expressions;

namespace sp26se058_3dprintshop_be.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    //public DbSet<TodoList> TodoLists => Set<TodoList>();

    //public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<DesignTemplate> DesignTemplates => Set<DesignTemplate>();
    public DbSet<DesignVariant> DesignVariants => Set<DesignVariant>();
    public DbSet<ConceptTag> ConceptTags => Set<ConceptTag>();
    public DbSet<DesignTag> DesignTags => Set<DesignTag>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialPriceHistory> MaterialPriceHistories => Set<MaterialPriceHistory>();
    public DbSet<MaterialInventoryTransaction> MaterialInventoryTransactions => Set<MaterialInventoryTransaction>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<DesignWork> DesignWorks => Set<DesignWork>();
    public DbSet<DesignLog> DesignLogs => Set<DesignLog>();
    public DbSet<DesignVersionHistory> DesignVersionHistorys => Set<DesignVersionHistory>();
    public DbSet<TechnicalDraft> TechnicalDrafts => Set<TechnicalDraft>();
    public DbSet<ShippingAddress> ShippingAddresses => Set<ShippingAddress>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<FeedbackImage> FeedbackImages => Set<FeedbackImage>();
    public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<PackageOption> PackageOptions => Set<PackageOption>();
    public DbSet<ServiceSelection> ServiceSelections => Set<ServiceSelection>();
    public DbSet<ServiceSelectionOption> ServiceSelectionOptions => Set<ServiceSelectionOption>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        //base.OnModelCreating(builder); remove ASP core identity
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // Kiểm tra xem thực thể có kế thừa BaseAuditableEntity không
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Tạo Lambda Expression: e => e.Deleted == null
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var propertyMethodInfo = typeof(EF).GetMethod("Property")!.MakeGenericMethod(typeof(DateTimeOffset?));
                var deletedProperty = Expression.Call(propertyMethodInfo, parameter, Expression.Constant(nameof(BaseAuditableEntity.Deleted)));
                var compareExpression = Expression.MakeBinary(ExpressionType.Equal, deletedProperty, Expression.Constant(null, typeof(DateTimeOffset?)));
                var lambda = Expression.Lambda(compareExpression, parameter);

                // Áp dụng bộ lọc
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);

                var indexBuilder = builder.Entity(entityType.ClrType)
                .HasIndex(nameof(BaseAuditableEntity.Deleted));

                if (Database.IsMySql())
                {
                    indexBuilder.HasFilter("`Deleted` IS NULL");
                }
            }
        }
    }
    public void HardRemove<TEntity>(TEntity entity) where TEntity : class
    {
        if (entity is BaseAuditableEntity auditable)
        {
            auditable.DeletedBy = "HARD_DELETE_FLAG";
        }
        this.Set<TEntity>().Remove(entity);
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Enum>()
            .HaveConversion<string>();
    }
}
