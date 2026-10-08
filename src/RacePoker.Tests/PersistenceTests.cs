using System.Text.Json;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "RacePoker-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath(string name) => Path.Combine(_directory, name);
    public PersistenceTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Settings_CorruptionRecoversPreviousAtomicSaveAndPreservesDamagedFile()
    {
        var path = FilePath("settings.json");
        var service = new SettingsService(path);
        Assert.True(service.Save(new GameSettings { MaxRounds = 7 }));
        Assert.True(service.Save(new GameSettings { MaxRounds = 12 }));
        Assert.Equal(7, JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path + ".bak"))!.MaxRounds);
        File.WriteAllText(path, "{broken");
        Assert.Equal(7, service.Load().MaxRounds);
        Assert.Contains("backup", service.LastNotice);
        var damaged = Assert.Single(Directory.GetFiles(_directory, "settings.json.corrupt-*"));
        Assert.Equal("{broken", File.ReadAllText(damaged));
        Assert.True(service.Save(new GameSettings { MaxRounds = 8 }));
        Assert.Equal(8, service.Load().MaxRounds);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp-*"));
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("")]
    public void MalformedFiles_UseDefaultsWithoutCrashing(string json)
    {
        File.WriteAllText(FilePath("settings.json"), json);
        File.WriteAllText(FilePath("highscores.json"), json);
        var settings = new SettingsService(FilePath("settings.json"));
        Assert.Equal(10, settings.Load().MaxRounds);
        Assert.NotNull(settings.LastNotice);
        var scores = new HighScoreService(FilePath("highscores.json"));
        scores.Load();
        Assert.Empty(scores.Entries);
        Assert.NotNull(scores.LastNotice);
    }

    [Fact]
    public void Settings_ValidateNumbersEnumsAndStyles()
    {
        File.WriteAllText(FilePath("settings.json"), """{"MaxRounds":-10,"MaxSeats":100,"FinishDistance":0,"PickingTimeoutSeconds":-3,"FinishRule":99,"HighCardRule":99,"RaceIconStyle":null,"HandResultStyle":"bad"}""");
        var settings = new SettingsService(FilePath("settings.json")).Load();
        Assert.Equal(1, settings.MaxRounds);
        Assert.Equal(4, settings.MaxSeats);
        Assert.Equal(1000, settings.FinishDistance);
        Assert.Equal(0, settings.PickingTimeoutSeconds);
        Assert.Equal(FinishRule.EndOfRound, settings.FinishRule);
        Assert.Equal(HighCardRule.HighestRank, settings.HighCardRule);
        Assert.Equal("Image", settings.RaceIconStyle);
        settings.CardSwayDegrees = double.NaN;
        settings.LogoGlowBlur = double.PositiveInfinity;
        settings.Normalize();
        Assert.Equal(10, settings.CardSwayDegrees);
        Assert.Equal(30, settings.LogoGlowBlur);
    }

    [Fact]
    public void FailedSave_ReportsErrorAndKeepsPreviousFile()
    {
        var path = FilePath("settings.json");
        var service = new SettingsService(path);
        Assert.True(service.Save(new GameSettings { MaxRounds = 7 }));
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.False(service.Save(new GameSettings { MaxRounds = 12 }));
            Assert.NotNull(service.LastNotice);
        }
        Assert.Equal(7, service.Load().MaxRounds);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp-*"));
    }

    [Fact]
    public void Leaderboards_PreserveLegacyAndKeepTenPerRulesProfile()
    {
        var path = FilePath("highscores.json");
        File.WriteAllText(path, """[{"Initials":"OLD","Score":999999},null,{"Initials":null,"Score":5},{"Initials":"BAD","Score":-1}]""");
        var scores = new HighScoreService(path);
        scores.Load();
        Assert.Equal(2, scores.GetEntries("legacy").Count);
        Assert.Equal("AAA", scores.GetEntries("legacy")[1].Initials);
        for (int i = 0; i < 15; i++)
            scores.TryAdd(new HighScoreEntry { Initials = "ace!", Score = i, RulesKey = "new", RulesDescription = "New rules" });
        Assert.Equal(10, scores.GetEntries("new").Count);
        Assert.Equal(2, scores.GetEntries("legacy").Count);
        Assert.True(scores.QualifiesForHighScore(0, "other"));
        Assert.False(scores.QualifiesForHighScore(4, "new"));
        scores.Load();
        Assert.Equal(12, scores.Entries.Count);
        Assert.All(scores.GetEntries("new"), e => Assert.Equal("ACE", e.Initials));
        File.WriteAllText(path, "invalid");
        scores.Load();
        Assert.Equal(12, scores.Entries.Count);
        Assert.NotNull(scores.LastNotice);
    }

    [Fact]
    public void RulesProfile_SeparatesGameplayRulesAndCustomPayoutsButIgnoresPresentation()
    {
        var s = new GameSettings(); var table = new PointTable();
        var original = ScoreRules.Key(s, table, 1);
        s.SoundEnabled = false; s.CardSwayDegrees = 0;
        Assert.Equal(original, ScoreRules.Key(s, table, 1));
        Assert.NotEqual(original, ScoreRules.Key(s, table, 2));
        s.MaxRounds++; Assert.NotEqual(original, ScoreRules.Key(s, table, 1)); s.MaxRounds--;
        s.FinishDistance++; Assert.NotEqual(original, ScoreRules.Key(s, table, 1)); s.FinishDistance--;
        s.PickingTimeoutSeconds = 0; Assert.NotEqual(original, ScoreRules.Key(s, table, 1)); s.PickingTimeoutSeconds = 30;
        s.HighCardRule = HighCardRule.SumOfRanks; Assert.NotEqual(original, ScoreRules.Key(s, table, 1)); s.HighCardRule = HighCardRule.HighestRank;
        s.FinishRule = FinishRule.FirstToFinish; Assert.NotEqual(original, ScoreRules.Key(s, table, 1)); s.FinishRule = FinishRule.EndOfRound;
        table.Points[HandCategory.OnePair]++; Assert.NotEqual(original, ScoreRules.Key(s, table, 1));
    }

    [Fact]
    public void CustomPointTable_RecoversCorruptionAndRejectsInvalidPayouts()
    {
        var path = FilePath("pointTable.json");
        File.WriteAllText(path, """{"OnePair":6000,"TwoPair":-1,"Flush":2000000,"Unknown":12}""");
        var table = PointTableService.Load(path);
        Assert.Equal(6000, table.Points[HandCategory.OnePair]);
        Assert.Equal(6700, table.Points[HandCategory.TwoPair]);
        Assert.Equal(15000, table.Points[HandCategory.Flush]);
        File.WriteAllText(path, "{broken");
        Assert.Equal(4500, PointTableService.Load(path).Points[HandCategory.OnePair]);
        Assert.Single(Directory.GetFiles(_directory, "pointTable.json.corrupt-*"));
    }
}
