using CineTraker.Models;
using Microsoft.EntityFrameworkCore;

namespace CineTraker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Review>()
                .HasOne<Movie>()
                .WithMany()
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade); 
        }
    }
}