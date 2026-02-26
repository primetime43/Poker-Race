using System.IO;
using System.Media;
using RacePoker.Core.Models;

namespace RacePoker.Wpf.Services;

public class SoundService
{
    private readonly GameSettings _settings;
    private readonly string _soundDir;
    private readonly Dictionary<string, SoundPlayer> _players = new();

    public SoundService(GameSettings settings)
    {
        _settings = settings;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _soundDir = Path.Combine(appData, "RacePoker", "Sounds");
        Directory.CreateDirectory(_soundDir);

        GeneratePlaceholders();
    }

    public void Play(string soundName)
    {
        if (!_settings.SoundEnabled)
            return;

        try
        {
            if (!_players.TryGetValue(soundName, out var player))
            {
                var path = Path.Combine(_soundDir, soundName + ".wav");
                if (!File.Exists(path))
                    return;
                player = new SoundPlayer(path);
                player.Load();
                _players[soundName] = player;
            }
            player.Play();
        }
        catch
        {
            // Silently ignore audio errors
        }
    }

    private void GeneratePlaceholders()
    {
        GenerateTone("card_pick", 800, 80);
        GenerateTone("hand_complete", 1000, 150);
        GenerateTone("racing", 600, 400);
        GenerateTone("round_end", 700, 200);
        GenerateTripleTone("game_win");
        GenerateTone("game_over", 300, 300);
    }

    private void GenerateTone(string name, int freq, int durationMs)
    {
        var path = Path.Combine(_soundDir, name + ".wav");
        if (File.Exists(path))
            return;

        var samples = GenerateSineSamples(freq, durationMs);
        WriteWav(path, samples);
    }

    private void GenerateTripleTone(string name)
    {
        var path = Path.Combine(_soundDir, name + ".wav");
        if (File.Exists(path))
            return;

        // Ascending triple tone: C5, E5, G5
        var s1 = GenerateSineSamples(523, 120);
        var s2 = GenerateSineSamples(659, 120);
        var s3 = GenerateSineSamples(784, 200);

        var combined = new short[s1.Length + s2.Length + s3.Length];
        s1.CopyTo(combined, 0);
        s2.CopyTo(combined, s1.Length);
        s3.CopyTo(combined, s1.Length + s2.Length);

        WriteWav(path, combined);
    }

    private static short[] GenerateSineSamples(int freq, int durationMs)
    {
        const int sampleRate = 22050;
        int sampleCount = sampleRate * durationMs / 1000;
        var samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / sampleRate;
            // Apply fade-in/fade-out envelope to avoid clicks
            double envelope = 1.0;
            int fadeLen = Math.Min(sampleCount / 10, 200);
            if (i < fadeLen)
                envelope = (double)i / fadeLen;
            else if (i > sampleCount - fadeLen)
                envelope = (double)(sampleCount - i) / fadeLen;

            double sample = Math.Sin(2 * Math.PI * freq * t) * envelope;
            samples[i] = (short)(sample * 12000);
        }
        return samples;
    }

    private static void WriteWav(string path, short[] samples)
    {
        const int sampleRate = 22050;
        const short bitsPerSample = 16;
        const short channels = 1;
        int dataSize = samples.Length * 2;

        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);

        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16); // chunk size
        bw.Write((short)1); // PCM
        bw.Write(channels);
        bw.Write(sampleRate);
        bw.Write(sampleRate * channels * bitsPerSample / 8); // byte rate
        bw.Write((short)(channels * bitsPerSample / 8)); // block align
        bw.Write(bitsPerSample);

        // data chunk
        bw.Write("data"u8);
        bw.Write(dataSize);
        foreach (var s in samples)
            bw.Write(s);
    }
}
