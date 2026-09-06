namespace OrderFlow.IntegrationTests.Infrastructure;

public abstract class PostgreSqlIntegrationTestBase(PostgreSqlApiFactory factory) : IAsyncLifetime
{
    protected PostgreSqlApiFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
