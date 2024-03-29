﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Project.Core.Abstract;
using Project.DAL.CustomMigrations;
using Project.Entity.Entities;

namespace Project.DAL.DatabaseContext
{
    public class DataContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUtilService _utilService;

        public DataContext(DbContextOptions<DataContext> options, IHttpContextAccessor httpContextAccessor, IUtilService utilService)
               : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _utilService = utilService;
        }

        public DbSet<User> Users { get; set; }

        public DbSet<RequestLog> RequestLogs { get; set; }

        public DbSet<ResponseLog> ResponseLogs { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Book> Books { get; set; }

        public DbSet<Author> Authors { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditProperties();
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
             optionsBuilder.UseLazyLoadingProxies();
        }

        /* migration commands
          dotnet ef --startup-project ../Project.API migrations add initial --context DataContext
          dotnet ef --startup-project ../Project.API database update --context DataContext
        */

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(m => m.Username).IsUnique();
            modelBuilder.Entity<User>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<RequestLog>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<ResponseLog>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Category>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Book>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<Author>().HasQueryFilter(m => !m.IsDeleted);

            DataSeed.Seed(modelBuilder);
        }

        private void SetAuditProperties()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is AuditableEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            string tokenString = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    // var originalValues = entityEntry.OriginalValues.ToObject();
                    // var currentValues = entityEntry.CurrentValues.ToObject();
                    ((AuditableEntity)entityEntry.Entity).CreatedAt = DateTime.Now;
                    ((AuditableEntity)entityEntry.Entity).CreatedBy = _utilService.GetUserIdFromToken(tokenString);
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    Entry((AuditableEntity)entityEntry.Entity).Property(p => p.CreatedAt).IsModified = false;
                    Entry((AuditableEntity)entityEntry.Entity).Property(p => p.CreatedBy).IsModified = false;

                    if (((AuditableEntity)entityEntry.Entity).IsDeleted)
                    {
                        Entry((AuditableEntity)entityEntry.Entity).Property(p => p.ModifiedBy).IsModified = false;
                        Entry((AuditableEntity)entityEntry.Entity).Property(p => p.ModifiedAt).IsModified = false;

                        ((AuditableEntity)entityEntry.Entity).DeletedAt = DateTime.Now;
                        ((AuditableEntity)entityEntry.Entity).DeletedBy = _utilService.GetUserIdFromToken(tokenString);
                    }
                    else
                    {
                        ((AuditableEntity)entityEntry.Entity).ModifiedAt = DateTime.Now;
                        ((AuditableEntity)entityEntry.Entity).ModifiedBy = _utilService.GetUserIdFromToken(tokenString);
                    }
                }
            }
        }
    }
}