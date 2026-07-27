using CineTraker.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; 
using Microsoft.AspNetCore.Identity;
using CineTraker.Shared.Models;
using CineTraker.Data.Entities;

namespace CineTraker.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MovieEntity> Movies { get; set; }
        public DbSet<ReviewEntity> Reviews { get; set; }
        public DbSet<StreamingSourceEntity> StreamingSources { get; set; }
        public DbSet<UserMapEntity> UserMaps { get; set; }
        public DbSet<MovieRequestEntity> MovieRequests { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MovieEntity>().ToTable("Movies");
            modelBuilder.Entity<ReviewEntity>().ToTable("Reviews");
            modelBuilder.Entity<StreamingSourceEntity>().ToTable("StreamingSources");
            modelBuilder.Entity<UserMapEntity>().ToTable("UserMaps");
            modelBuilder.Entity<MovieRequestEntity>().ToTable("MovieRequests");

            modelBuilder.Entity<MovieEntity>()
                .HasMany(m => m.Sources)
                .WithMany(s => s.Movies)
                .UsingEntity(j => j.ToTable("MovieStreamingSource"));

            modelBuilder.Entity<ReviewEntity>()
                .HasOne<MovieEntity>(r => r.Movie)
                .WithMany()
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StreamingSourceEntity>()
                .HasIndex(s => s.Name)
                .IsUnique();
        }
    }
}