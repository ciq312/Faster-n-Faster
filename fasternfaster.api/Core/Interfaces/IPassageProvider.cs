namespace FasterNFaster.Api.Core.Interfaces;

public interface IPassageProvider
{
    Task<string> GetPassageAsync(int wordCount);
}
