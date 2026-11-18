using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FasterNFaster.IntegrationTests;

public class GatedDbCommandCounter : DbCommandCounter
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

    private TaskCompletionSource gate = OpenGate();

    public void Hold() => Volatile.Write(ref gate, NewGate());

    public void Release() => Volatile.Read(ref gate).TrySetResult();

    public async Task WaitUntilCountOrTimeoutAsync(int count, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        while (Count < count && elapsed.Elapsed < timeout)
            await Task.Delay(PollInterval);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await Volatile.Read(ref gate).Task;
        return result;
    }

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource OpenGate()
    {
        var open = NewGate();
        open.SetResult();
        return open;
    }
}
