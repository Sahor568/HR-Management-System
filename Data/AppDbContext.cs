using Microsoft.EntityFrameworkCore;

namespace Management
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Add DbSet<TEntity> properties for your entities, e.g.:
        // public DbSet<MyEntity> MyEntities { get; set; }
    }
}