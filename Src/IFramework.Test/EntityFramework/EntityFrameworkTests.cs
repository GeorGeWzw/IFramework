using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Domain;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using IFramework.JsonNet;
using IFramework.Log4Net;
using IFramework.MessageStores.MongoDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Xunit;
using ObjectProvider = IFramework.DependencyInjection.Unity.ObjectProvider;
using TransactionOptions = System.Transactions.TransactionOptions;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
    {
        public static string MySqlConnectionStringName = "DemoDbContext.MySql";
        public static string ConnectionStringName = "DemoDbContext";
        public static string MongoDbConnectionStringName = "DemoDbContext.MongoDb";

        public DemoDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            var configuratoin = builder.Build();
            var optionsBuilder = new DbContextOptionsBuilder<DemoDbContext>();
            //optionsBuilder.UseMySQL(configuratoin.GetConnectionString(MySqlConnectionStringName));
            //optionsBuilder.UseSqlServer(configuratoin.GetConnectionString(ConnectionStringName));
            optionsBuilder.UseMongoDb(configuratoin.GetConnectionString(MongoDbConnectionStringName));
            return new DemoDbContext(optionsBuilder.Options);
        }
    }


    public class EntityFrameworkTests
    {
        public EntityFrameworkTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            Configuration.Instance
                         //.UseMicrosoftDependencyInjection()
                         //.UseUnityContainer()
                         .UseAutofacContainer()
                         .UseConfiguration(builder.Build())
                         .UseCommonComponents()
                         .UseJsonNet()
                         .UseLog4Net()
                         .UseDbContext<DemoDbContext>(options =>
                         {
                             options.EnableSensitiveDataLogging();
                             options.UseMongoDb(Configuration.Instance.GetConnectionString(DemoDbContextFactory.MongoDbConnectionStringName));
                             //options.UseMySQL(Configuration.Instance.GetConnectionString(DemoDbContextFactory.MySqlConnectionStringName));
                             //options.UseInMemoryDatabase(nameof(DemoDbContext));
                             //options.UseSqlServer(Configuration.Instance.GetConnectionString(DemoDbContextFactory.ConnectionStringName));
                         });

            ObjectProviderFactory.Instance.Build();
            //using (var serviceScope = ObjectProviderFactory.CreateScope())
            //{
            //    var dbContext = serviceScope.GetService<DemoDbContext>();
            //    dbContext.Database.Migrate();
            //}
        }

        public class DbTest : IDisposable
        {
            public DbTest(int count)
            {
                Count = count;
            }

            public int Count { get; }

            public void Dispose()
            {
                Console.Write("dispose");
            }
        }

        [Fact]
        public async Task AddPresonTest()
        {
            try
            {
                var id = DateTime.Now.Ticks;
                using (var serviceScope = ObjectProviderFactory.CreateScope())
                {
                    var dbContext = serviceScope.GetService<DemoDbContext>();
                    var person = new Person(id, "ivan");
                    dbContext.Persons.Add(person);
                    await dbContext.SaveChangesAsync();
                }

                using (var serviceScope = ObjectProviderFactory.CreateScope())
                {
                    var dbContext = serviceScope.GetService<DemoDbContext>();
                    var person =  await dbContext.Persons.FindAsync(id).ConfigureAwait(false);
                    Assert.NotNull(person);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Fact]
        public async Task AddUserTest()
        {
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                                                    new TransactionOptions {IsolationLevel = IsolationLevel.ReadCommitted},
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var dbContext = serviceScope.GetService<DemoDbContext>();
                    if (dbContext == null)
                    {
                        var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
                        logger.LogError((serviceScope as ObjectProvider)?.UnityContainer.Registrations.ToJson());
                        Assert.NotNull(dbContext);
                    }

                    var user = new User($"ivan_{DateTime.Now.Ticks}", "male");
                    user.AddCard("ICBC");
                    user.AddCard("CCB");
                    user.AddCard("ABC");

                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                    scope.Complete();
                    //var client = dbContext.GetMongoDbClient();
                    //var database = dbContext.GetMongoDbDatabase();
                    //var conn = dbContext.GetMongoDbConnection();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        [Fact]
        public async Task ConcurrentTest()
        {
            var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());

            try
            {
                var start = DateTime.Now;
                //await AddUserTest();
                var tasks = new List<Task>();
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(InternalGetUsersTest(i));
                    //tasks.Add(AddUserTest());
                }

                await Task.WhenAll(tasks);

                logger.LogDebug($"incremented : {DemoDbContext.Total} cost: {(DateTime.Now - start).TotalMilliseconds} ms");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"incremented : {DemoDbContext.Total}");
                throw;
            }
        }


        [Fact]
        public async Task ConcurrentUdpateTest()
        {
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var concurrencyProcessor = serviceScope.GetService<IConcurrencyProcessor>();
                var dbContext = serviceScope.GetService<DemoDbContext>();
                using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                   new TransactionOptions
                                                                   {
                                                                       IsolationLevel = IsolationLevel.ReadCommitted
                                                                   },
                                                                   TransactionScopeAsyncFlowOption.Enabled))
                {
                    await concurrencyProcessor.ProcessAsync(async () =>
                    {
                        var account = await dbContext.Users.FirstOrDefaultAsync();
                        account.ModifyName($"ivan{DateTime.Now}");
                        await dbContext.SaveChangesAsync();
                    });
                    transactionScope.Complete();
                }
            }
        }

        [Fact]
        public void DbContextPoolTest()
        {
            int hashCode1, hashCode2;
            var services = new ServiceCollection();
            services.AddScoped<DbTest>();
            using (var scope = ObjectProviderFactory.CreateScope(provider => provider.RegisterInstance(new DbTest(3))))
            {
                var dbContext = scope.GetService<DemoDbContext>();
                hashCode1 = dbContext.GetHashCode();
                dbContext.Database.AutoTransactionsEnabled = false;
                var dbTest = scope.GetService<DbTest>();
                Assert.Equal(3, dbTest.Count);
            }

            using (var scope = ObjectProviderFactory.CreateScope(provider => provider.RegisterInstance(new DbTest(1))))
            {
                var dbContext = scope.GetService<DemoDbContext>();
                hashCode2 = dbContext.GetHashCode();
                Assert.True(dbContext.Database.AutoTransactionsEnabled);
                var dbTest = scope.GetService<DbTest>();
                Assert.Equal(1, dbTest.Count);
            }

            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public Task GetUsersTest()
        {
            return InternalGetUsersTest(0);
        }

        private async Task InternalGetUsersTest(int i)
        {
            using (var scope = ObjectProviderFactory.CreateScope())
            {
                var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());

                //var serviceProvider = scope.GetService<IServiceProvider>();
                //if (serviceProvider == null)
                //{
                //    var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
                //    logger.LogError((scope as ObjectProvider)?.UnityContainer.Registrations.ToJson());
                //    Assert.NotNull(serviceProvider);
                //}

                //var options = new DbContextOptionsBuilder<DemoDbContext>();
                //options.UseMongoDb(Configuration.Instance.GetConnectionString(DemoDbContextFactory.MongoDbConnectionStringName));
               
                var dbContext = scope.GetService<DemoDbContext>();
                try
                {
                    var database = dbContext.GetMongoDbDatabase();
                    //var user = await database.GetCollection<User>("users")
                    //                         .FindAsync(new ExpressionFilterDefinition<User>(u => u.Id == $"ivan_{DateTime.Now.Ticks}"))
                    //                         .ConfigureAwait(false);
                    var user = dbContext.Users
                                              .Find($"ivan_{DateTime.Now.Ticks}");
                                              //.ConfigureAwait(false);
                    logger.LogDebug($"Get users {i}");
                    // var connection = dbContext.GetMongoDbDatabase();
                    //var users = await dbContext.Users
                    //                           //.Include(u => u.Cards)
                    //                           //.FindAll(u => !string.IsNullOrWhiteSpace(u.Name))
                    //                           .Take(10)
                    //                           .ToListAsync()
                    //                           .ConfigureAwait(false);
                    //foreach (var u in users)
                    //{
                    //    await u.LoadCollectionAsync(u1 => u1.Cards);
                    //    Assert.True(u.Cards.Count > 0);
                    //    //Assert.Equal(u.GetDbContext<DemoDbContext>().GetHashCode(), dbContext.GetHashCode());
                    //}
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }
}