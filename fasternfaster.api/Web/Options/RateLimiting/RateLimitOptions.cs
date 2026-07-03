namespace FasterNFaster.Api.Web.Options.RateLimiting;

public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;
    public WindowLimit Global { get; set; } = new() { PermitLimit = 300 };
    public WindowLimit AuthStrict { get; set; } = new() { PermitLimit = 5 };
    public WindowLimit AuthModerate { get; set; } = new() { PermitLimit = 10 };
    public WindowLimit Lookup { get; set; } = new() { PermitLimit = 30 };

    public class WindowLimit
    {
        public int PermitLimit { get; set; }
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    }
}
