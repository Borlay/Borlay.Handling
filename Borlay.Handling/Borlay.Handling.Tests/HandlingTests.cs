using Borlay.Handling.Notations;
using Borlay.Injection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling.Tests
{
    public class HandlingTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void MethodTaskHash()
        {
            var hash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(CalculatorResult));
            var thash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(Task<CalculatorResult>));

            Assert.IsNotNull(hash);
            Assert.AreEqual(hash, thash);
        }

        [Test]
        public void MethodTaskArrayHash()
        {
            var hash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(CalculatorResult[]));
            var thash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(Task<CalculatorResult[]>));

            Assert.IsNotNull(hash);
            Assert.AreEqual(hash, thash);
        }

        [Test]
        public void MethodTaskGenericArrayHash()
        {
            var hash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(Tuple<Tuple<CalculatorResult>>[]));
            var thash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(Task<Tuple<Tuple<CalculatorResult>>[]>));

            Assert.IsNotNull(hash);
            Assert.AreEqual(hash, thash);
        }

        [Test]
        public void MethodTaskVoidHash()
        {
            var hash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(void));
            var thash = TypeHasher.GetMethodString(new Type[] { typeof(int) }, typeof(Task));

            Assert.IsNotNull(hash);
            Assert.AreEqual(hash, thash);
        }

        [Test]
        public async Task CalculatorAddDuoTest()
        {
            var handlerProvider = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handlerProvider.LoadFromReference<HandlingTests>();

            resolver.Register(new CalculatorParameter() { First = 10 });

            //var sum = InterfaceHandling.CreateHandler<ICalculator, InterfaceHandlerTest<ICalculator>>(new HandlerArgument() { Arg = "5" });
            var value = await handlerProvider.HandleAsync<CalculatorResult>(resolver.CreateSession(), "", 1,
                new CalculatorArgument() { Left = 2, Right = 3 },
                new CalculatorArgument() { Left = 4, Right = 5 });
            var result = (CalculatorResult)value;

            Assert.AreEqual(24, result.Result);
        }

        [Test]
        public async Task CalculatorAddStringTest()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            resolver.Register(new CalculatorParameter() { First = 10 });

            //var sum = InterfaceHandling.CreateHandler<ICalculator, InterfaceHandlerTest<ICalculator>>(new HandlerArgument() { Arg = "5" });
            var result = await handler.HandleAsync<CalculatorResult>(resolver.CreateSession(), "IAddString", 1, "6");
            Assert.AreEqual(7, result.Result);
        }

        [Test]
        public async Task CalculatorAddStringWithScopeTest()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            resolver.Register(new CalculatorParameter() { First = 10 });

            //var sum = InterfaceHandling.CreateHandler<ICalculator, InterfaceHandlerTest<ICalculator>>(new HandlerArgument() { Arg = "5" });
            var value = await handler.HandleAsync<CalculatorResult>(resolver.CreateSession(), "IAddString", 1, "6");
            var result = (CalculatorResult)value;

            Assert.AreEqual(7, result.Result);
        }

        [Test]
        public async Task CalculatorAddStringWithScopeAndActionNameTest()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            resolver.Register(new CalculatorParameter() { First = 10 });

            //var sum = InterfaceHandling.CreateHandler<ICalculator, InterfaceHandlerTest<ICalculator>>(new HandlerArgument() { Arg = "5" });
            var value = await handler.HandleAsync<CalculatorResult>(resolver.CreateSession(), "CalculatorScope", "AddAsync", "6");
            var result = (CalculatorResult)value;

            Assert.AreEqual(7, result.Result);
        }

        [Test]
        public async Task HandleSum()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 0, new IntArgument() { Left = 2, Right = 3 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 5);
        }

        [Test]
        public async Task HandleSumZero()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 0, new object[] { });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 1);
        }

        [Test]
        public async Task HandleSumString()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync<string>(resolver.CreateSession(), 0, "s");

            Assert.IsNotNull(result);
            Assert.IsTrue(result is string);
            Assert.IsTrue((string)result == "sums");
        }

        [Test]
        public async Task HandleSumDuo()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 0, new object[] {
                new IntArgument() { Left = 2, Right = 3 },
                new IntArgument() { Left = 4, Right = 5 }
                });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == (2 + 3 + 4 + 5));
        }

        [Test]
        public async Task HandleSumThree()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 0, new object[] {
                new IntArgument() { Left = 2, Right = 3 },
                new IntArgument() { Left = 4, Right = 5 },
                new ByteArgument { Left = 6, Right = 7 }
                });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == (2 + 3 + 4 + 5 + 6 + 7));
        }

        [Test]
        public async Task HandleMultiplyAsync()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 1, new IntArgument() { Left = 2, Right = 3 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 6);
        }

        [Test]
        public async Task HandleManySum()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            resolver.Register(new CalculatorParameter() { First = 10 });

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                //var result = await handler.HandleAsync(0, new IntArgument() { Left = 2, Right = 3 });
                //var result = await handler.HandleAsync(0, new IntArgument() { Left = 2, Right = 3 });

                var value = await handler.HandleAsync<CalculatorResult>(resolver.CreateSession(), 1, new object[] {
                new CalculatorArgument() { Left = 2, Right = 3 },
                });
            }

            watch.Stop();
            // s: 100k 0.59s
        }

        [Test]
        public async Task HandleManyMultiplyAsync()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                var result = await handler.HandleAsync<int>(resolver.CreateSession(), 1, new IntArgument() { Left = 2, Right = 3 });
            }

            watch.Stop();
            // 0.48s (0.8) 100k 
        }

        [Test]
        public async Task HandleMinusAsyncWithRole()
        {
            var handler = new HandlerProvider();
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 2, new IntArgument() { Left = 2, Right = 3 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == -1);
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithManyRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider();
            handler.LoadFromReference<HandlingTests>();

            resolver.Register(new Roles("Minus", "Calculation"));

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithCalculationMinusRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider();
            handler.LoadFromReference<HandlingTests>();

            resolver.Register(new Roles("Minus", "Calculation"));

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithAdminMinusRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider();
            handler.LoadFromReference<HandlingTests>();

            resolver.Register(new Roles("Minus", "Admin"));

            var result = await handler.HandleAsync<int>(resolver.CreateSession(), 2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithCalculationAdminMinusRoles()
        {
            Assert.ThrowsAsync<UnauthorizedException>(async () =>
            {
                var resolver = new Resolver();
                resolver.LoadFromReference<HandlingTests>();

                var handler = new HandlerProvider();
                handler.LoadFromReference<HandlingTests>();

                resolver.Register(new Roles("Calculation", "Admin"));

                var result = await handler.HandleAsync<int>(resolver.CreateSession(), 2, new ByteArgument() { Left = 6, Right = 2 });

                Assert.IsNotNull(result);
                Assert.IsTrue(result is int);
                Assert.IsTrue((int)result == 3);
            });
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithMinusRoles()
        {
            Assert.ThrowsAsync<UnauthorizedException>(async () =>
            {
                var resolver = new Resolver();
                resolver.LoadFromReference<HandlingTests>();

                var handler = new HandlerProvider();
                handler.LoadFromReference<HandlingTests>();

                resolver.Register(new Roles("Minus"));

                var result = await handler.HandleAsync<int>(resolver.CreateSession(), 2, new ByteArgument() { Left = 6, Right = 2 });

                Assert.IsNotNull(result);
                Assert.IsTrue(result is int);
                Assert.IsTrue((int)result == 3);
            });
        }
    }

    public class IntArgument
    {
        public int Left { get; set; }
        public int Right { get; set; }
    }

    public class ByteArgument
    {
        public byte Left { get; set; }
        public byte Right { get; set; }
    }


    [Handler, Resolve(Singletone = true)]
    [Scope("")]
    public class HandlerSum
    {
        

        [Action("0")]
        public string SumString(string arg)
        {
            return "sum" + arg;
        }

        [Action("0")]
        public int SumZero()
        {
            return 1;
        }

        [Action("0")]
        public int Sum(IntArgument intArgument)
        {
            return intArgument.Left + intArgument.Right;
        }

        [Action("0")]
        public int SumDuo(IntArgument firstIntArgument, IntArgument secondIntArgument)
        {
            return firstIntArgument.Left + firstIntArgument.Right + secondIntArgument.Left + secondIntArgument.Right;
        }

        [Action("0")]
        public int SumThree(IntArgument firstIntArgument, IntArgument secondIntArgument, ByteArgument byteArgument)
        {
            return firstIntArgument.Left + firstIntArgument.Right + secondIntArgument.Left + secondIntArgument.Right + byteArgument.Left + byteArgument.Right;
        }
    }

    [Handler, Resolve]
    [Scope("")]
    public class HandlerMultiplyAsync
    {
        [Action("1")]
        public async Task<int> MultiplyAsync(IntArgument intArgument, [Inject]CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            return intArgument.Left * intArgument.Right;
        }
    }

    //[Role("Calculation")]
    [Handler, Resolve]
    [Scope("")]
    public class HandlerMinusAsyncWithRole
    {
        //[Role("Minus")]
        [Action("2")]
        public async Task<int> MultiplyAsync(IntArgument intArgument, [Inject]CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            return intArgument.Left - intArgument.Right;
        }
    }


    [Role("Calculation", "Admin")]
    [Handler, Resolve]
    [Scope("")]
    public class HandlerByteMinusAsyncWithManyRoles
    {
        [Role("Minus")]
        [Action("2")]
        public async Task<int> MultiplyAsync(ByteArgument byteArgument, [Inject]CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            return byteArgument.Left / byteArgument.Right;
        }
    }
}