namespace OrderFlow.IntegrationTests.Infrastructure;

internal sealed class MutableTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    internal void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    internal void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }
}
