using System.Threading;
using OrderFlow.Api.Orders.CreateOrder;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class CoordinatedOrderCreationDiagnosticHook : IOrderCreationDiagnosticHook
{
    private int _expectedParticipants;
    private int _arrivedParticipants;
    private TaskCompletionSource? _allParticipantsArrived;

    public void Prepare(int expectedParticipants)
    {
        _expectedParticipants = expectedParticipants;
        _arrivedParticipants = 0;
        _allParticipantsArrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Disable()
    {
        _expectedParticipants = 0;
        _arrivedParticipants = 0;
        _allParticipantsArrived = null;
    }

    public async Task AfterStockValidationAsync(CancellationToken cancellationToken)
    {
        var expectedParticipants = _expectedParticipants;
        var allParticipantsArrived = _allParticipantsArrived;

        if (expectedParticipants <= 1 || allParticipantsArrived is null)
        {
            return;
        }

        if (Interlocked.Increment(ref _arrivedParticipants) == expectedParticipants)
        {
            allParticipantsArrived.TrySetResult();
        }

        await allParticipantsArrived.Task.WaitAsync(cancellationToken);
    }
}
