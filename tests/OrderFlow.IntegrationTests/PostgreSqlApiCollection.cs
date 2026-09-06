using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests;

[CollectionDefinition(nameof(PostgreSqlApiCollection), DisableParallelization = true)]
public sealed class PostgreSqlApiCollection : ICollectionFixture<PostgreSqlApiFactory>;
