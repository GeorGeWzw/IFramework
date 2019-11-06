using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Domain;
using IFramework.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using StackExchange.Redis;
using IDatabase = StackExchange.Redis.IDatabase;

namespace IFramework.EntityFrameworkCore.Redis.Storage
{
    public class RedisDatabase : Database
    {
        private static readonly ConcurrentDictionary<Type, string[]> KeyPropertiesByEntityType = new ConcurrentDictionary<Type, string[]>();
        private readonly IDatabase _database;
        private readonly IDbContextOptions _options;
        private readonly RedisOptionsExtension _redisOptionsExtension;

        public RedisDatabase(DatabaseDependencies dependencies,
                             IDatabase database,
                             IDbContextOptions options) : base(dependencies)
        {
            _database = database;
            _options = options;
            _redisOptionsExtension = _options.FindExtension<RedisOptionsExtension>();
        }

        public static string GetKey(EntityEntry entry)
        {
            var keyProperties = KeyPropertiesByEntityType.GetOrAdd(entry.Entity.GetType(),
                                                                   t => entry.Metadata
                                                                             .FindPrimaryKey()
                                                                             .Properties
                                                                             .Select(property => property.Name)
                                                                             .ToArray());

            var keyParts = keyProperties.Select(propertyName => entry.Property(propertyName)
                                                                     .CurrentValue)
                                        .ToArray();

            return keyParts.First().ToString();
        }


        public override int SaveChanges(IList<IUpdateEntry> entries)
        {
            return SaveChangesAsync(entries).GetAwaiter().GetResult();
        }

        public override async Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = new CancellationToken())
        {
            var transaction = _database.CreateTransaction(_redisOptionsExtension.AsyncState);

            var toDeleteKeys = entries.Where(e => e.EntityState == EntityState.Deleted)
                                      .Select(e =>
                                      {
                                          RedisKey key = GetKey(e.ToEntityEntry());
                                          return key;
                                      })
                                      .ToArray();
            Task<long> deleteTask = null;
            var allTasks = new List<Task>();
            var updateTasks = new List<Task<bool>>();
            if (toDeleteKeys.Length > 0)
            {
                foreach (var deleteKey in toDeleteKeys)
                {
                    transaction.AddCondition(Condition.KeyExists(deleteKey));
                }

                deleteTask = transaction.KeyDeleteAsync(toDeleteKeys);
                allTasks.Add(deleteTask);
            }

            foreach (var updateEntry in entries.Where(e => e.EntityState == EntityState.Added || e.EntityState == EntityState.Modified))
            {
                var key = GetKey(updateEntry.ToEntityEntry());
                if (updateEntry.EntityState == EntityState.Modified && updateEntry.ToEntityEntry().Entity is VersionedAggregateRoot)
                {
                    transaction.AddCondition(Condition.StringEqual(key, ""));
                }

                updateTasks.Add(transaction.StringSetAsync(key,
                                                           updateEntry.ToEntityEntry()
                                                                      .Entity
                                                                      .ToJson()));
            }

            allTasks.AddRange(updateTasks);
            await transaction.ExecuteAsync();
            await Task.WhenAll(allTasks);

            return (int) (deleteTask?.Result ?? 0) + updateTasks.Count(t => t.Result);
        }
    }
}