using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<CommandHistory> CommandHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Подключение к SQL Server LocalDB
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=FTPServerDB;Trusted_Connection=true;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Уникальный индекс для логина
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            // Связь между User и CommandHistory
            modelBuilder.Entity<CommandHistory>()
                .HasOne(ch => ch.User)
                .WithMany(u => u.CommandHistory)
                .HasForeignKey(ch => ch.UserId);
        }
    }
}