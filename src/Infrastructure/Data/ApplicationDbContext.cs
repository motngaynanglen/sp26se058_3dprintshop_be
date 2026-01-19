using System.Reflection;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<VariantMaterialOption> VariantMaterialOptions => Set<VariantMaterialOption>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        //base.OnModelCreating(builder); remove ASP core identity
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
