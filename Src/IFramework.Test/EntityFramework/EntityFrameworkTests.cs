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
using IFramework.Infrastructure;
using IFramework.JsonNet;
using IFramework.Log4Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ObjectProvider = IFramework.DependencyInjection.Unity.ObjectProvider;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
    {
        public static string MySqlConnectionStringName = "DemoDbContext.MySql";
        public static string ConnectionStringName = "DemoDbContext";
        public static string MongoDbConnectionStringName = "DemoDbContext.MongoDb";
        public static string RedisConnectionStringName = "DemoDbContext.Redis";

        public DemoDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            var optionsBuilder = new DbContextOptionsBuilder<DemoDbContext>();
            //optionsBuilder.UseMySQL(configuratoin.GetConnectionString(MySqlConnectionStringName));
            optionsBuilder.UseRedis(configuration.GetConnectionString(RedisConnectionStringName));
            //optionsBuilder.UseSqlServer(configuratoin.GetConnectionString(ConnectionStringName));
            //optionsBuilder.UseMongoDb(configuratoin.GetConnectionString(MongoDbConnectionStringName));
            return new DemoDbContext(optionsBuilder.Options);
        }
    }


    public class EntityFrameworkTests
    {
        public EntityFrameworkTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            var services = new ServiceCollection();
            services.AddAutofacContainer()
                    .AddConfiguration(configuration)
                    .AddCommonComponents()
                    //.UseMicrosoftDependencyInjection()
                    //.UseUnityContainer()
                    //.UseAutofacContainer()
                    //.UseConfiguration(configuration)
                    //.UseCommonComponents()
                    .AddJsonNet()
                    .AddLog4Net()
                    .AddDbContextPool<DemoDbContext>(options =>
                    {
                        options.UseLazyLoadingProxies();
                        options.EnableSensitiveDataLogging();
                        //options.UseMongoDb(Configuration.Instance.GetConnectionString(DemoDbContextFactory.MongoDbConnectionStringName));
                        //options.UseMySQL(Configuration.Instance.GetConnectionString(DemoDbContextFactory.MySqlConnectionStringName));
                        //options.UseMySql(Configuration.Instance.GetConnectionString(DemoDbContextFactory.MySqlConnectionStringName));
                        //options.UseInMemoryDatabase(nameof(DemoDbContext));
                        //options.UseSqlServer(Configuration.Instance.GetConnectionString(DemoDbContextFactory.ConnectionStringName));
                        options.UseRedis(Configuration.Instance.GetConnectionString(DemoDbContextFactory.RedisConnectionStringName), 7);
                    });

            ObjectProviderFactory.Instance.Build(services);
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var dbContext = serviceScope.GetService<DemoDbContext>();
               // dbContext.Database.EnsureCreated();
            }
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
                    var person = new Person(id, $"ivan_{DateTime.Now.Ticks}");
                    dbContext.Persons.Add(person);
                    await dbContext.SaveChangesAsync();
                }

                using (var serviceScope = ObjectProviderFactory.CreateScope())
                {
                    var dbContext = serviceScope.GetService<DemoDbContext>();
                    var person = await dbContext.Persons.FindAsync(id).ConfigureAwait(false);
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
                var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
                var serviceProvider = serviceScope.GetService<IServiceProvider>();
                if (serviceProvider == null)
                {
                    Assert.NotNull(serviceProvider);
                }

                try
                {
                    var dbContext = serviceScope.GetService<DemoDbContext>();
                    if (dbContext == null)
                    {
                        logger.LogError((serviceScope as ObjectProvider)?.UnityContainer.Registrations.ToJson());
                        Assert.NotNull(dbContext);
                    }

                    var user = new User($"ivan_{DateTime.Now.Ticks}",
                                        "male",
                                        new UserProfile(new Address("china",
                                                                    "shanghai",
                                                                    "baker"),
                                                        "football"));
                    user.AddCard("ICBC");
                    user.AddCard("CCB");
                    user.AddCard("ABC");

                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                    scope.Complete();
                }
                catch (Exception e)
                {
                    logger.LogError((object)e);
                    //Console.WriteLine(e);
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
                //await AddUserTest();
                var tasks = new List<Task>();
                for (int i = 0; i < 5; i++)
                {
                    tasks.Add(GetUsersTest());
                }

                await Task.WhenAll(tasks);

                logger.LogDebug($"incremented : {DemoDbContext.Total}");
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
                                                                   new TransactionOptions {
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
        public async Task GetUsersTest()
        {
            using (var scope = ObjectProviderFactory.CreateScope())
            {
                scope.SetContextData("TenantId", DateTime.Now.Ticks);
                var serviceProvider = scope.GetService<IServiceProvider>();
                if (serviceProvider == null)
                {
                    var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
                    logger.LogError((scope as ObjectProvider)?.UnityContainer.Registrations.ToJson());
                    Assert.NotNull(serviceProvider);
                }

                //var options = new DbContextOptionsBuilder<DemoDbContext>();
                //options.UseMongoDb(Configuration.Instance.GetConnectionString(DemoDbContextFactory.MongoDbConnectionStringName));

                var dbContext = scope.GetService<DemoDbContext>();
                //dbContext.InitializeTenant();
                try
                {
                    var user = await dbContext.Users.FindAsync("5D6E104A0CCE415AD44F17AE")
                                              .ConfigureAwait(false);
                    // var connection = dbContext.GetMongoDbDatabase();
                    var users = await dbContext.Users
                                               //.Include(u => u.Cards)
                                               //.FindAll(u => !string.IsNullOrWhiteSpace(u.Name))
                                               .Take(10)
                                               .ToListAsync()
                                               .ConfigureAwait(false);
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


        [Fact]
        public async Task ModifyOwnedObjectTest()
        {
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var dbContext = serviceScope.GetService<DemoDbContext>();
                var user = await dbContext.Users
                                          //.Include(u => u.UserProfile)
                                          //.ThenInclude(p => p.Address)
                                          .FirstOrDefaultAsync()
                                          .ConfigureAwait(false);
                //await user.LoadReferenceAsync(u => u.UserProfile)
                //          .ConfigureAwait(false);
                await user.ReloadAsync()
                          .ConfigureAwait(false);

                user.ModifyProfile(user.UserProfile.Clone(new {
                    Address = user.UserProfile.Address.Clone(new {City = $"beijing.{DateTime.Now.Ticks}"}),
                    Hobby = "basketball"
                }));
                //user.RemoveCards();
                await dbContext.SaveChangesAsync()
                               .ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task RemoveUserCardTest()
        {
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var dbContext = serviceScope.GetService<DemoDbContext>();
                var user = await dbContext.Users.FirstOrDefaultAsync()
                                          .ConfigureAwait(false);
                var card = user.Cards.FirstOrDefault();
                if (card != null)
                {
                    user.RemoveCard(card);
                }

                //user.RemoveCards();
                await dbContext.SaveChangesAsync()
                               .ConfigureAwait(false);
            }
        }
    }
}