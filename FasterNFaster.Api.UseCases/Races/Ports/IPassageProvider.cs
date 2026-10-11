namespace FasterNFaster.Api.UseCases.Races.Ports;

public interface IPassageProvider
{
    ValueTask<string> GetPassageAsync(int wordCount = 50);
}
