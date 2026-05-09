namespace FasterNFaster.Tests.Fakes;

public class FakeClock
{
    public DateTime Now { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public Func<DateTime> Func => () => Now;
    public void Advance(TimeSpan by) => Now = Now.Add(by);
}
