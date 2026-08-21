namespace OrderFlow.Api.Orders.CreateOrder;

internal sealed class NoOpOrderCreationDiagnosticHook : IOrderCreationDiagnosticHook
{
    public Task AfterStockValidationAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
