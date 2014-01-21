using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Repositories;
using IFramework.UnitOfWork;
using Sample.Command;
using Sample.Domain.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;

namespace Sample.CommandHandler.Community
{
    public class CommunityCommandHandler : ICommandHandler<LoginCommand>,
                                           ICommandHandler<RegisterCommand>
    {
        protected IDomainRepository DomainRepository
        {
            get
            {
                return IoCFactory.Resolve<IDomainRepository>();
            }
        }

        public void Handle(LoginCommand command)
        {
            //var account = DomainRepository.Find<Account>(a => a.UserName.Equals(command.UserName)
            //                                                          && a.Password.Equals(command.Password));

            ////(DomainRepository as Framework.EntityFramework.Repositories.IMergeOptionChangable)
            ////    .ChangeMergeOption<Account>(MergeOption.OverwriteChanges);

            //if (account == null)
            //{
            //    throw new SysException(ErrorCode.WrongUsernameOrPassword);
            //}
            var account = new Account();
            account.Login();
            command.Result = new DTO.Account
            {
                ID = Guid.NewGuid(),
                UserName = command.UserName
            };
        }

        public void Handle(RegisterCommand command)
        {
            if (DomainRepository.Find<Account>(a => a.UserName == command.UserName) != null)
            {
                throw new SysException(ErrorCode.UsernameAlreadyExists, string.Format("Username {0} exists!", command.UserName));
            }

            Account account = new Account(command.UserName, command.Password, command.Email);
            DomainRepository.Add(account);
            command.Result = account.ID;
        }
    }
}
