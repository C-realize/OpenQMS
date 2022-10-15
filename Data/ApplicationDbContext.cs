using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenQMS.Models;
using OpenQMS.Models.Navigation;

namespace OpenQMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<OpenQMS.Models.AppDocument> AppDocument { get; set; }
        public DbSet<OpenQMS.Models.Training> Training { get; set; }
        public DbSet<UserTraining> UserTraining { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserTraining>().ToTable("UserTraining");

            modelBuilder.Entity<UserTraining>().HasKey(t => new { t.TraineeId, t.TrainingId });

            
        }

        public DbSet<OpenQMS.Models.Change>? Change { get; set; }
        public DbSet<OpenQMS.Models.Product> Product { get; set; }
        public DbSet<OpenQMS.Models.Capa>? Capa { get; set; }
        public DbSet<OpenQMS.Models.Deviation>? Deviation { get; set; }
        public DbSet<OpenQMS.Models.Asset> Asset { get; set; }
    }
}