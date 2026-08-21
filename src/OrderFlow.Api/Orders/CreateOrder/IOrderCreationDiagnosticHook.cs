namespace OrderFlow.Api.Orders.CreateOrder;

internal interface IOrderCreationDiagnosticHook
{
    Task AfterStockValidationAsync(CancellationToken cancellationToken);
}
