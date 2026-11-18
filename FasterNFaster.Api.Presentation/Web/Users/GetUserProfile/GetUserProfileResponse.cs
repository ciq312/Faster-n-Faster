namespace FasterNFaster.Api.Presentation.Web.Users.GetUserProfile;

public record GetUserProfileResponse(string Nick, int Wins, int RacesTyped, float BestWPM, float AvgWPM, float BestAccuracy, float AvgAccuracy, int WordsTyped);
