using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Blueshift.EntityFrameworkCore.MongoDB.Annotations;
using IFramework.Config;
using IFramework.Domain;
using IFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MySql.Data.EntityFrameworkCore.Extensions;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContext : MsDbContext
    {
        public static int Total;
        public DemoDbContext(DbContextOptions options)
            : base(options)
        {
            Interlocked.Add(ref Total, 1);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
        protected const string NextSequenceId = "NEXT VALUE FOR ids";

        public DbSet<User> Users { get; set; }
        //public DbSet<Card> Cards { get; set; }
        public DbSet<Person> Persons { get;set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<Entity>();
            modelBuilder.Ignore<AggregateRoot>();
            modelBuilder.Ignore<TimestampedAggregateRoot>();
            modelBuilder.Ignore<BaseEntity>();
            //modelBuilder.HasSequence<long>("ids")
            //            .StartsAt(1000)
            //            .IncrementsBy(1);

            var userEntity = modelBuilder.Entity<User>();
            //userEntity.HasMany(u => u.Cards)
            //            .WithOne()
            //            .HasForeignKey(c => c.UserId);
            modelBuilder.Ignore<Card>();

            userEntity.HasKey(u => u.Id);
            

            //modelBuilder.Entity<Person>()
            //            .Property(e => e.Id)
            //            .UseMySQLAutoIncrementColumn(nameof(Person));
        }
    }
}