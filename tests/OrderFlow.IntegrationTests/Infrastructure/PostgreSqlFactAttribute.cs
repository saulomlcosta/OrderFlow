namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (!PostgreSqlApiFactory.IsEnabled)
        {
            Skip = "Set ORDERFLOW_RUN_POSTGRESQL_TESTS=true to run PostgreSQL integration tests.";
        }
    }
}
