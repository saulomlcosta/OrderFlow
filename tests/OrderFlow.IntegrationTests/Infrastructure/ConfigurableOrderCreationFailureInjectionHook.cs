using System.Threading;
using OrderFlow.Api.Orders.CreateOrder;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class ConfigurableOrderCreationFailureInjectionHook : IOrderCreationFailureInjectionHook
{
    private int _shouldThrow;

    public void Enable()
    {
        Interlocked.Exchange(ref _shouldThrow, 1);
    }

    public void Disable()
    {
        Interlocked.Exchange(ref _shouldThrow, 0);
    }

    public Task AfterStockDecrementAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _shouldThrow, 0, 1) == 1)
        {
            throw new InvalidOperationException("Controlled failure after stock decrement.");
        }

        return Task.CompletedTask;
    }
}
