namespace OrderFlow.Api.Orders.CreateOrder;

internal interface IOrderCreationFailureInjectionHook
{
    Task AfterStockDecrementAsync(CancellationToken cancellationToken);
}
