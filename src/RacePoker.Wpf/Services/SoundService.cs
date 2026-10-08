using System.IO;
using System.Media;
using RacePoker.Core.Models;

namespace RacePoker.Wpf.Services;

public sealed class SoundService : IDisposable
{
    private const int SampleRate = 22050;
    private readonly GameSettings _settings;
    private readonly string _soundDir;
    private readonly Dictionary<string, SoundPlayer> _players = new();

    public SoundService(GameSettings settings, string? soundDirectory = null)
    {
        _settings = settings;
        _soundDir = soundDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RacePoker", "Sounds");
    }

    public void Play(string soundName)
    {
        if (!_settings.SoundEnabled) return;
        try
        {
            if (!_players.TryGetValue(soundName, out var player))
            {
                // v2 names avoid loading the earlier auto-generated placeholder beeps.
                var custom = Path.Combine(_soundDir, soundName + ".v2.wav");
                player = File.Exists(custom) ? new SoundPlayer(custom) : new SoundPlayer(new MemoryStream(CreateSound(soundName)));
                player.Load();
                _players[soundName] = player;
            }
            player.Play();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        { /* Audio failures must not interrupt a race. */ }
    }

    internal static byte[] CreateSound(string name)
    {
        double duration = name switch { "card_pick" => 0.09, "hand_complete" => 1.0, "racing" => 0.8, "game_win" => 1.8, _ => 0.45 };
        var mix = new double[(int)(duration * SampleRate)];
        var rng = new Random(42);
        void Note(double frequency, double start, double length, double volume)
        {
            for (int i = 0; i < (int)(length * SampleRate); i++)
            {
                int at = (int)(start * SampleRate) + i;
                if (at >= mix.Length) break;
                double t = (double)i / SampleRate;
                double envelope = Math.Min(1, t / 0.008) * Math.Pow(Math.Max(0, 1 - t / length), 1.7);
                double wave = Math.Sin(2 * Math.PI * frequency * t) + 0.25 * Math.Sin(4 * Math.PI * frequency * t);
                mix[at] += wave * envelope * volume;
            }
        }
        void Hoof(double start, double volume)
        {
            for (int i = 0; i < 1300; i++)
            {
                int at = (int)(start * SampleRate) + i;
                if (at >= mix.Length) break;
                double t = (double)i / SampleRate;
                mix[at] += (0.65 * Math.Sin(2 * Math.PI * 140 * t) + 0.35 * (rng.NextDouble() * 2 - 1)) * Math.Exp(-t * 70) * volume;
            }
        }
        switch (name)
        {
            case "card_pick":
                Hoof(0, 0.24); Note(1400, 0.01, 0.06, 0.1); break;
            case "hand_complete":
                for (int i = 0; i < 8; i++) Hoof(i * 0.11 + (i % 2) * 0.025, 0.2);
                Note(523.25, 0, 0.3, 0.12); Note(659.25, 0.1, 0.3, 0.12); Note(783.99, 0.2, 0.45, 0.12); break;
            case "racing":
                for (int i = 0; i < 7; i++) Hoof(i * 0.1 + (i % 2) * 0.025, 0.28); break;
            case "game_win":
                double[] melody = { 523.25, 659.25, 783.99, 1046.5 };
                for (int i = 0; i < melody.Length; i++) Note(melody[i], i * 0.18, 0.3, 0.18);
                foreach (double f in new[] { 523.25, 659.25, 783.99, 1046.5 }) Note(f, 0.8, 0.9, 0.11);
                break;
            case "game_over": Note(392, 0, 0.2, 0.15); Note(261.63, 0.15, 0.3, 0.15); break;
            default: Note(659.25, 0, 0.3, 0.15); Note(783.99, 0.1, 0.3, 0.15); break;
        }
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8); writer.Write(36 + mix.Length * 2); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate);
        writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(mix.Length * 2);
        foreach (var sample in mix) writer.Write((short)(Math.Clamp(sample, -1, 1) * 28000));
        writer.Flush();
        return stream.ToArray();
    }

    public void Dispose()
    {
        foreach (var player in _players.Values) { player.Stop(); player.Stream?.Dispose(); player.Dispose(); }
        _players.Clear();
    }
}
