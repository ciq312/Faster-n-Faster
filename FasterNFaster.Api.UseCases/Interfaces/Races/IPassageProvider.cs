namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IPassageProvider
{
    ValueTask<string> GetPassageAsync(int wordCount = 50);
}
