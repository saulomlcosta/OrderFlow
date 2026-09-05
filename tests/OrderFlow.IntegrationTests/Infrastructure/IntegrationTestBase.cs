namespace OrderFlow.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(OrderFlowApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
