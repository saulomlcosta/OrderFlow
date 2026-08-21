namespace OrderFlow.Api.Orders.CreateOrder;

internal sealed class NoOpOrderCreationFailureInjectionHook : IOrderCreationFailureInjectionHook
{
    public Task AfterStockDecrementAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
