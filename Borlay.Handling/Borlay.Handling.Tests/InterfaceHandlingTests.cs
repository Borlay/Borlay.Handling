using Borlay.Injection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Borlay.Handling.Tests
{
    public class InterfaceHandlingTests
    {
        [Test]
        public async Task SumInterfaceHandlingFromResolver()
        {
            Resolver resolver = new Resolver();
            resolver.Register(new HandlerArgument() { Arg = "5" });

            var sum = resolver.CreateSession().CreateHandler<ISum, InterfaceHandlerTest<ISum>>();
            var value = await sum.Sum("opa", "o");
            Assert.AreEqual("Sumopao5", value);
        }

        [Test]
        public async Task SumInterfaceHandlingFromArguments()
        {
            var sum = InterfaceHandling.CreateHandler<ISum, InterfaceHandlerTest<ISum>>(new HandlerArgument() { Arg = "5" });
            var value = await sum.Sum("opa", "o");
            Assert.AreEqual("Sumopao5", value);
        }

        [Test]
        public async Task SumArgInterfaceHandlingFromArguments()
        {
            var sum = InterfaceHandling.CreateHandler<ISumArg, InterfaceHandlerTest<ISumArg>>(new HandlerArgument() { Arg = "5" });
            var value = await sum.Sum(20, new SumArgument() { Param2 = "a" });
            //var value = await sum.Sum(20, "10");
            Assert.AreEqual("Sum20Param: a5", value);
        }

        [Test]
        //[ExpectedException(typeof(NotImplementedException))]
        public async Task SumPropertyThrowNotImplemented()
        {
            var sum = InterfaceHandling.CreateHandler<ISumArg, InterfaceHandlerTest<ISumArg>>(new HandlerArgument() { Arg = "5" });
            var value = sum.Prop;
        }

        [Test]
        public async Task SumIntResultThrow()
        {
            try
            {
                var sum = InterfaceHandling.CreateHandler<ISumIntResult, InterfaceHandlerTest<ISumIntResult>>(new HandlerArgument() { Arg = "5" });
                throw new Exception("Should throw exception");
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("Handler and interface return types"))
                    throw;
            }
        }

        [Test]
        public async Task SumNotTaskThrow()
        {
            try
            {
                var sum = InterfaceHandling.CreateHandler<ISumNotTask, InterfaceHandlerTest<ISumNotTask>>(new HandlerArgument() { Arg = "5" });
                throw new Exception("Should throw exception");
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("Interfeice return type should inherit Task"))
                    throw;
            }
        }

        [Test]
        public async Task SumInterfaceHandlingMany()
        {
            Resolver resolver = new Resolver();
            resolver.Register(new HandlerArgument() { Arg = "5" });

            var sum = InterfaceHandling.CreateHandler<ISum, InterfaceHandlerTest<ISum>>(resolver.CreateSession());

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                var value = await sum.Sum("opa", "o");
            }

            watch.Stop();

            // 100k 0.17s
        }
    }

    public interface ISum
    {
        string Prop { get; set; }

        Task<string> Sum(string param, string param2);
    }

    public interface ISumIntResult
    {
        int Sum(int param, int param2);
    }

    public interface ISumNotTask
    {
        string Sum(string param, string param2);
    }

    public interface ISumArg
    {
        string Prop { get; set; }

        int Prop2 { get; set; }

        Task<string> Sum(int param, SumArgument param2); // SumArgument
    }

    public class Suma
    {
        public virtual async Task<string> Sum(string param, string param2)
        {
            return "";
        }
    }

    public class SumArgument
    {
        public string Param2 { get; set; }

        public override string ToString()
        {
            return $"Param: {Param2}";
        }
    }

    public class HandlerArgument
    {
        public string Arg { get; set; }
    }


    public class InterfaceHandlerTest<TActAs> : IInterfaceHandler
    {
        private readonly HandlerArgument handlerArgument;

        public InterfaceHandlerTest(int handlerArgument)
        {
        }

        public InterfaceHandlerTest(HandlerArgument handlerArgument)
        {
            this.handlerArgument = handlerArgument;
        }

        public object HandleAsync(string methodName, object[] args)
        {
            var methodInfo = typeof(TActAs).GetRuntimeMethod(methodName, args.Select(a => a.GetType()).ToArray());
            string result = "";
            result += methodInfo.Name;
            foreach (var a in args)
            {
                result += a.ToString();
            }

            result += handlerArgument.Arg;

            var tcs = new TaskCompletionSource<string>();
            tcs.SetResult(result);

            var task = tcs.GetType().GetRuntimeMethod("get_Task", new Type[0]).Invoke(tcs, null);

            return task;
        }
    }
}
