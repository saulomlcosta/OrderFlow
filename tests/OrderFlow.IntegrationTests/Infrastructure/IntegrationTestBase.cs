namespace OrderFlow.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(OrderFlowApiFactory factory) : IAsyncLifetime
{
    protected OrderFlowApiFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
