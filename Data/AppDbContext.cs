using Microsoft.EntityFrameworkCore;
using TestPOSApp.Models;

namespace TestPOSApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Item> Items { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Transaction)
                .WithMany(t => t.Items)
                .HasForeignKey(ti => ti.TransactionId);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Item)
                .WithMany()
                .HasForeignKey(ti => ti.ItemId);
        }
    }
}
