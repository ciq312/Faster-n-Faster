namespace FasterNFaster.Tests;

public static class ConcurrentRunner
{
    public static async Task RunTogether(IEnumerable<Func<Task>> actions)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = actions
            .Select(action => Task.Run(async () =>
            {
                await start.Task;
                await action();
            }))
            .ToList();

        start.SetResult();
        await Task.WhenAll(tasks);
    }
}
