using Godot;
using RacePoker.Core.Enums;

namespace RacePoker.Godot;

public sealed class PlayerPreferences
{
    public bool SoundEnabled { get; set; } = true;
    public bool ShowLastCardHints { get; set; } = true;
    public int NextRoundDelaySeconds { get; set; } = 6;
    public ComputerDifficulty Difficulty { get; set; } = ComputerDifficulty.Normal;
    public int HumanPlayers { get; set; } = 1;
    public bool IncludeComputer { get; set; } = true;

    public static PlayerPreferences Load(string path)
    {
        using var file = new ConfigFile();
        if (file.Load(path) != Error.Ok) return new PlayerPreferences();
        return new PlayerPreferences
        {
            SoundEnabled = ReadBool(file, "audio", "sound_enabled"),
            ShowLastCardHints = ReadBool(file, "assistance", "last_card_hints"),
            NextRoundDelaySeconds = ReadDelay(file),
            Difficulty = ReadDifficulty(file),
            HumanPlayers = ReadHumanPlayers(file),
            IncludeComputer = ReadBool(file, "race", "include_computer")
        };
    }

    private static int ReadHumanPlayers(ConfigFile file)
    {
        var value = file.GetValue("race", "human_players", 1);
        return value.VariantType == Variant.Type.Int ? (int)Math.Clamp(value.AsInt64(), 1, 4) : 1;
    }

    private static ComputerDifficulty ReadDifficulty(ConfigFile file)
    {
        var value = file.GetValue("race", "computer_difficulty", "Normal");
        return value.VariantType == Variant.Type.String && Enum.TryParse<ComputerDifficulty>(value.AsString(), out var difficulty)
            && Enum.IsDefined(difficulty) ? difficulty : ComputerDifficulty.Normal;
    }

    private static int ReadDelay(ConfigFile file)
    {
        var value = file.GetValue("race", "next_round_delay_seconds", 6);
        return value.VariantType == Variant.Type.Int ? (int)Math.Clamp(value.AsInt64(), 1, 10) : 6;
    }

    private static bool ReadBool(ConfigFile file, string section, string key)
    {
        var value = file.GetValue(section, key, true);
        return value.VariantType != Variant.Type.Bool || value.AsBool();
    }

    public Error Save(string path)
    {
        using var file = new ConfigFile();
        file.SetValue("audio", "sound_enabled", SoundEnabled);
        file.SetValue("assistance", "last_card_hints", ShowLastCardHints);
        file.SetValue("race", "next_round_delay_seconds", NextRoundDelaySeconds);
        file.SetValue("race", "computer_difficulty", Difficulty.ToString());
        file.SetValue("race", "human_players", HumanPlayers);
        file.SetValue("race", "include_computer", IncludeComputer);
        return file.Save(path);
    }
}
