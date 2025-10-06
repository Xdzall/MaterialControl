using Microsoft.EntityFrameworkCore;    
using Material_Control.Models;

namespace Material_Control.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<PartModel> Parts { get; set; }
        public DbSet<InventoryItemModel> InventoryItems { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<MaterialModel> Materials { get; set; }
        public DbSet<PendingApproval> PendingApproval { get; set; }
        public DbSet<PendingApprovalParts> PendingApprovalParts { get; set; }
        public DbSet<PendingApprovalMaterials> PendingApprovalMaterials { get; set; }
    }
}