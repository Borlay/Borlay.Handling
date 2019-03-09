using Borlay.Handling.Notations;
using Borlay.Injection;
using NUnit.Framework;
using System;
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
        public async Task HandleSum()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync(0, new IntArgument() { Left = 2, Right = 3 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 5);
        }

        [Test]
        public async Task HandleSumZero()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync(0);

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 1);
        }

        [Test]
        public async Task HandleSumString()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync(0, "s");

            Assert.IsNotNull(result);
            Assert.IsTrue(result is string);
            Assert.IsTrue((string)result == "sums");
        }

        [Test]
        public async Task HandleSumDuo()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync(0, new IntArgument() { Left = 2, Right = 3 }, new IntArgument() { Left = 4, Right = 5 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == (2 + 3 + 4 + 5));
        }

        [Test]
        public async Task HandleSumThree()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync(0, new IntArgument() { Left = 2, Right = 3 }, new IntArgument() { Left = 4, Right = 5 }, new ByteArgument { Left = 6, Right = 7 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == (2 + 3 + 4 + 5 + 6 + 7));
        }

        [Test]
        public async Task HandleMultiplyAsync()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync(1, new IntArgument() { Left = 2, Right = 3 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 6);
        }

        [Test]
        public async Task HandleManySum()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                var result = await handler.HandleAsync(0, new IntArgument() { Left = 2, Right = 3 });
            }

            watch.Stop();
            // s: 100k 0.59s
        }

        [Test]
        public async Task HandleManyMultiplyAsync()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < 100000; i++)
            {
                var result = await handler.HandleAsync(1, new IntArgument() { Left = 2, Right = 3 });
            }

            watch.Stop();
            // 0.48s (0.8) 100k 
        }

        [Test]
        public async Task HandleMinusAsyncWithRole()
        {
            var handler = new HandlerProvider();
            handler.Resolver.LoadFromReference<HandlingTests>();
            handler.LoadFromReference<HandlingTests>();

            var result = await handler.HandleAsync(2, new IntArgument() { Left = 2, Right = 3 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == -1);
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithManyRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider(resolver);
            handler.LoadFromReference<HandlingTests>();

            handler.Resolver.Register(new Roles("Minus", "Calculation"));

            var result = await handler.HandleAsync(2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithCalculationMinusRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider(resolver);
            handler.LoadFromReference<HandlingTests>();

            handler.Resolver.Register(new Roles("Minus", "Calculation"));

            var result = await handler.HandleAsync(2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        //[ExpectedException(typeof(UnauthorizedException))]
        public async Task HandlerByteMinusAsyncWithAdminMinusRolesToOtherResolver()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider(resolver);
            handler.LoadFromReference<HandlingTests>();
            handler.Resolver.Register(new Roles("Minus", "Admin"));

            var handler2 = new HandlerProvider(resolver);
            handler2.LoadFromReference<HandlingTests>();

            var result = await handler2.HandleAsync(2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        public async Task HandlerByteMinusAsyncWithAdminMinusRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider(resolver);
            handler.LoadFromReference<HandlingTests>();

            handler.Resolver.Register(new Roles("Minus", "Admin"));

            var result = await handler.HandleAsync(2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        //[ExpectedException(typeof(UnauthorizedException))]
        public async Task HandlerByteMinusAsyncWithCalculationAdminMinusRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider(resolver);
            handler.LoadFromReference<HandlingTests>();

            handler.Resolver.Register(new Roles("Calculation", "Admin"));

            var result = await handler.HandleAsync(2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
        }

        [Test]
        //[ExpectedException(typeof(UnauthorizedException))]
        public async Task HandlerByteMinusAsyncWithMinusRoles()
        {
            var resolver = new Resolver();
            resolver.LoadFromReference<HandlingTests>();

            var handler = new HandlerProvider(resolver);
            handler.LoadFromReference<HandlingTests>();

            handler.Resolver.Register(new Roles("Minus"));

            var result = await handler.HandleAsync(2, new ByteArgument() { Left = 6, Right = 2 });

            Assert.IsNotNull(result);
            Assert.IsTrue(result is int);
            Assert.IsTrue((int)result == 3);
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
    public class HandlerSum
    {
        [IdAction(0)]
        public int SumZero()
        {
            return 1;
        }

        [IdAction(0)]
        public string SumString(string arg)
        {
            return "sum" + arg;
        }

        [IdAction(0)]
        public int Sum(IntArgument intArgument)
        {
            return intArgument.Left + intArgument.Right;
        }

        [IdAction(0)]
        public int SumDuo(IntArgument firstIntArgument, IntArgument secondIntArgument)
        {
            return firstIntArgument.Left + firstIntArgument.Right + secondIntArgument.Left + secondIntArgument.Right;
        }

        [IdAction(0)]
        public int SumThree(IntArgument firstIntArgument, IntArgument secondIntArgument, ByteArgument byteArgument)
        {
            return firstIntArgument.Left + firstIntArgument.Right + secondIntArgument.Left + secondIntArgument.Right + byteArgument.Left + byteArgument.Right;
        }
    }

    [Handler, Resolve]
    public class HandlerMultiplyAsync
    {
        [IdAction(1)]
        public async Task<int> MultiplyAsync(IntArgument intArgument, [Inject]CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            return intArgument.Left * intArgument.Right;
        }
    }

    //[Role("Calculation")]
    [Handler, Resolve]
    public class HandlerMinusAsyncWithRole
    {
        //[Role("Minus")]
        [IdAction(2)]
        public async Task<int> MultiplyAsync(IntArgument intArgument, CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            return intArgument.Left - intArgument.Right;
        }
    }


    [Role("Calculation")]
    [Role("Admin")]
    [Handler, Resolve]
    public class HandlerByteMinusAsyncWithManyRoles
    {
        [Role("Minus")]
        [IdAction(2)]
        public async Task<int> MultiplyAsync(ByteArgument byteArgument, CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            return byteArgument.Left / byteArgument.Right;
        }
    }
}