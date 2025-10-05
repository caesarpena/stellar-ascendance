
namespace webapi.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using webapi.Models;

    public class MediaItemsContext : IdentityDbContext
    {
        public MediaItemsContext(DbContextOptions<MediaItemsContext> options) 
            : base(options)
        {

        }
        public DbSet<MediaItem> MediaItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

    }
} 