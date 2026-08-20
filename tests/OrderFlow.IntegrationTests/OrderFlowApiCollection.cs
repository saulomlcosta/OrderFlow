using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests;

[CollectionDefinition(nameof(OrderFlowApiCollection))]
public sealed class OrderFlowApiCollection : ICollectionFixture<OrderFlowApiFactory>;
