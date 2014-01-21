using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using System.Threading.Tasks;
using Sample.Command;

namespace EQueueTest
{
    public class Worker
    {
        ICommandBus _CommandBus;
        public Worker(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }

        public ApiResult<TResult> ActionWithResult<TResult>(ICommand<TResult> command)
        {
            return ExceptionManager.Process<TResult>(() =>
            {
                _CommandBus.Send(command).Wait();
                return command.Result;
            });
        }

        public ApiResult Action(ICommand command)
        {
            var commandGenericInterfaceType = command.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType);
            if (commandGenericInterfaceType != null)
            {
                var resultType = commandGenericInterfaceType.GetGenericArguments().First();
                var result = this.InvokeGenericMethod(resultType, "ActionWithResult", new object[] { command })
                            as ApiResult;
                return result;
            }
            else
            {
                return ExceptionManager.Process(() =>
                {
                    _CommandBus.Send(command).Wait();
                });
            }
        }

        public void DoCommand(List<ICommand> batchCommands)
        {
            batchCommands.ForEach(cmd =>
            {
                Task.Factory.StartNew(() =>
                {
                    Action(cmd);
                });
            });
        }

        internal void StartTest()
        {
            var commands = new List<ICommand>();
            commands.Add(new LoginCommand { UserName = "Ivan", Password = "123456" });
            commands.Add(new LoginCommand { UserName = "Ivan1", Password = "123456" });
            commands.Add(new LoginCommand { UserName = "Ivan12", Password = "123456" });

            var batchCount = 1000;
            int i = 0;
            while (i++ < batchCount)
            {
                DoCommand(commands);
            }
        }
    }
}
