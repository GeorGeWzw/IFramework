using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Bus;
using IFramework.Repositories;
using IFramework.Infrastructure;
using Microsoft.Practices.Unity;
using IFramework.Domain;
using IFramework.Event;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.Config;

namespace IFramework.UnitOfWork
{
    public abstract class BaseUnitOfWork : IUnitOfWork
    {
        protected List<Action> _ModelContextCommitActions = new List<Action>();
        protected IMessageStore MessageStore
        {
            get
            {
                return IoCFactory.Resolve<IMessageStore>();
            }
        }

        public BaseUnitOfWork(IDomainEventBus domainEventBus)
        {
            _DomainEventBus = domainEventBus;
        }
        #region IUnitOfWork Members

        protected IDomainEventBus _DomainEventBus;

        public abstract void Commit();

        #endregion

        #region IUnitOfWork Members

        public void RegisterModelContextCommitActions(params Action[] actions)
        {
            _ModelContextCommitActions.AddRange(actions);
        }

        #endregion


        public abstract IRepository<TAggregateRoot> GetRepository<TAggregateRoot>() where TAggregateRoot : class, IAggregateRoot;
       
        public IDomainRepository DomainRepository
        {
            get
            {
                try
                {
                    var repository = IoCFactory.Resolve<IDomainRepository>(new ParameterOverride("unitOfWork", this));
                    return repository;
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
