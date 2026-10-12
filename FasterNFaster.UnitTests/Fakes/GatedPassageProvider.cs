using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.UnitTests.Fakes;

public class GatedPassageProvider : IPassageProvider
{
    private readonly TaskCompletionSource<string> passage = new();
    private readonly TaskCompletionSource requested = new();

    public async ValueTask<string> GetPassageAsync(int wordCount = 50)
    {
        requested.SetResult();
        return await passage.Task;
    }

    public Task WaitForRequest() => requested.Task;

    public void Complete(string result) => passage.SetResult(result);
}
