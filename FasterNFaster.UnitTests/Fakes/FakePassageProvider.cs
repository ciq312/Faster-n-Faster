using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.UnitTests.Fakes;

public class FakePassageProvider(string passage) : IPassageProvider
{
    public List<int> RequestedWordCounts { get; } = [];

    public ValueTask<string> GetPassageAsync(int wordCount = 50)
    {
        RequestedWordCounts.Add(wordCount);
        return new ValueTask<string>(passage);
    }
}
