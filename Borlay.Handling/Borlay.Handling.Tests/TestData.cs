using Borlay.Handling.Notations;
using Borlay.Injection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Borlay.Handling.Tests
{
    public class CalculatorArgument
    {
        public int Left { get; set; }

        public int Right { get; set; }
    }

    public class CalculatorResult
    {
        public int Result { get; set; }
    }

    public class CalculatorParameter
    {
        public int First { get; set; }
    }

    [Resolve]
    [Handler]
    public interface IAddString
    {
        [IdAction(1, CanBeCached = true, CacheReceivedResponse = true)]
        Task<CalculatorResult> AddAsync(string argument);
    }

    [Resolve(Singletone = false )]
    [Handler]
    public interface ICalculator //: IAddString
    {

        [IdAction(1, CanBeCached = true, CacheReceivedResponse = true)]
        Task<CalculatorResult> AddAsync(CalculatorArgument argument, [Inject]CancellationToken cancellationToken);

        [IdAction(1, CanBeCached = true, CacheReceivedResponse = true)]
        Task<CalculatorResult> AddAsync();

        [IdAction(1, CanBeCached = true, CacheReceivedResponse = true)]
        Task<CalculatorResult> AddAsync(CalculatorArgument argument, CalculatorArgument argument2, [Inject]CancellationToken cancellationToken);

        [NameAction]
        Task<CalculatorResult> Subsync(CalculatorArgument argument, [Inject]CancellationToken cancellationToken);
    }

    [Resolve]
    [Handler]
    [Role("Merge")]
    public interface IMerge
    {
        [NameAction]
        Task<CalculatorResult> MergeAsync(CalculatorArgument argument, [Inject]CancellationToken cancellationToken);
    }

    [NameScope("add")]
    public class CalculatorScope : IAddString
    {
        public async Task<CalculatorResult> AddAsync(string argument)
        {
            return new CalculatorResult() { Result = 1 + int.Parse(argument) };
        }
    }

    public class Calculator : ICalculator, IMerge
    {
        private readonly CalculatorParameter calculatorParameter;

        public Calculator(CalculatorParameter calculatorParameter)
        {
            this.calculatorParameter = calculatorParameter;
        }

        public async Task<CalculatorResult> AddAsync(CalculatorArgument argument, [Inject]CancellationToken cancellationToken)
        {
            return new CalculatorResult() { Result = argument.Left + argument.Right + calculatorParameter.First };
        }

        [IdAction(1, CanBeCached = true, CacheReceivedResponse = true)]
        public async Task<CalculatorResult> AddAsync(string argument)
        {
            return new CalculatorResult() { Result = calculatorParameter.First + int.Parse(argument) };
        }

        public async Task<CalculatorResult> AddAsync()
        {
            return new CalculatorResult() { Result = calculatorParameter.First };
        }

        public async Task<CalculatorResult> AddAsync(CalculatorArgument argument, CalculatorArgument argument2, [Inject]CancellationToken cancellationToken)
        {
            return new CalculatorResult() { Result = argument.Left + argument.Right + argument2.Left + argument2.Right + calculatorParameter.First };
        }

        public async Task<CalculatorResult> MergeAsync(CalculatorArgument argument, [Inject]CancellationToken cancellationToken)
        {
            return new CalculatorResult() { Result = argument.Left * argument.Right * calculatorParameter.First };
        }

        public async Task<CalculatorResult> Subsync(CalculatorArgument argument, [Inject]CancellationToken cancellationToken)
        {
            return new CalculatorResult() { Result = argument.Left - argument.Right - calculatorParameter.First };
        }
    }
}
