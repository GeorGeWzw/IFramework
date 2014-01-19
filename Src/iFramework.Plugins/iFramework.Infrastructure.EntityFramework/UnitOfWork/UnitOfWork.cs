using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.UnitOfWork;
using IFramework.Bus;
using Microsoft.Practices.Unity;
using System.Data.Entity;
using System.Transactions;
using IFramework.Infrastructure;
using IFramework.Config;
using IFramework.Repositories;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Infrastructure.Unity.LifetimeManagers;

namespace IFramework.EntityFramework
{
    public class UnitOfWork : BaseUnitOfWork
    {
        [Dependency(Constants.Configuration.DomainModelContext)]
        public DbContext DomainModelContext { get; set; }


        public UnitOfWork(IDomainEventBus eventBus)
            : base(eventBus)
        {
            _DomainEventBus = eventBus;
        }
        #region IUnitOfWork Members

        public override void Commit()
        {
            // TODO: should make domain events never losed, nedd transaction between
            //       model context and message queue
            using (var scope = new TransactionScope())
            {
                if (DomainModelContext != null)
                {
                    DomainModelContext.SaveChanges();
                }
                scope.Complete();
            }
            if (_DomainEventBus != null)
            {
                _DomainEventBus.Commit();
            }
            if (Configuration.GetAppConfig<bool>("PersistanceMessage"))
            {
                // TODO: persistance command and domain events
                var currentCommandContext = PerMessageContextLifetimeManager.CurrentMessageContext;
                var domainEventContexts = _DomainEventBus.GetMessageContexts();
                MessageStore.Save(currentCommandContext, domainEventContexts);
            }
        }

        #endregion

        public override IRepository<TAggregateRoot> GetRepository<TAggregateRoot>()
        {
            try
            {

                var repository = IoCFactory.Resolve<IRepository<TAggregateRoot>>(new ParameterOverride("dbContext", this.DomainModelContext));
                return repository;
            }
            catch
            {
                throw;
            }
        }
    }
}
