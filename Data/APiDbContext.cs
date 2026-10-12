using Crud.Interfaces;
using Crud.Models;
using CrudData.Models;
using Microsoft.EntityFrameworkCore;

namespace Crud.Data
{
    public class APiDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;
        public APiDbContext(DbContextOptions<APiDbContext> options, ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;

        }
        public DbSet<People> People { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<User> User { get; set; }
        //public DbSet<Post> Posts { get; set; }   //i do not want to create table for post because i am using it for api call only
        public DbSet<Postdetails> Postdetails { get; set; }

        private void UpdateAuditEntities()
        {
            var entries = ChangeTracker.Entries<IAuditableEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = _currentUserService.GetCurrentUserId();
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedBy = _currentUserService.GetCurrentUserId();
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                }
            }
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditEntities();
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
