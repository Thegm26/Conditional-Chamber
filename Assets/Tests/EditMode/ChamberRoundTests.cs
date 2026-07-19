using ChamberLogic;
using NUnit.Framework;

public class ChamberRoundTests
{
    [Test]
    public void LiveChance_UpdatesAfterEveryRevealedShell()
    {
        var round = new ChamberRound(2, 4, 17);
        Assert.That(round.LiveChance, Is.EqualTo(2f / 6f));

        var beforeLive = round.RemainingLive;
        var beforeBlank = round.RemainingBlank;
        var shell = round.Fire();

        Assert.That(round.RemainingTotal, Is.EqualTo(5));
        Assert.That(round.RemainingLive, Is.EqualTo(beforeLive - (shell == Shell.Live ? 1 : 0)));
        Assert.That(round.RemainingBlank, Is.EqualTo(beforeBlank - (shell == Shell.Blank ? 1 : 0)));
        Assert.That(round.LiveChance, Is.EqualTo((float)round.RemainingLive / round.RemainingTotal));
    }
}
