namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IPassageProvider
{
    Task<string> GetPassageAsync(int wordCount = 50);
}
