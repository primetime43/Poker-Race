using Godot;

namespace RacePoker.Godot;

public sealed class PlayerPreferences
{
    public bool SoundEnabled { get; set; } = true;
    public bool ShowLastCardHints { get; set; } = true;

    public static PlayerPreferences Load(string path)
    {
        using var file = new ConfigFile();
        if (file.Load(path) != Error.Ok) return new PlayerPreferences();
        return new PlayerPreferences
        {
            SoundEnabled = ReadBool(file, "audio", "sound_enabled"),
            ShowLastCardHints = ReadBool(file, "assistance", "last_card_hints")
        };
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
        return file.Save(path);
    }
}
