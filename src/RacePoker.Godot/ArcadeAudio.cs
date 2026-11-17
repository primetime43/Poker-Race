using Godot;

namespace RacePoker.Godot;

/// <summary>Small synthesized cues played by Godot's audio mixer; no external audio files.</summary>
public partial class ArcadeAudio : Node
{
    public bool Muted { get; private set; }
    private AudioStreamPlayer _pick = null!, _reward = null!, _finish = null!;

    public override void _Ready()
    {
        _pick = Player(Tone(new[] { 520f, 780f }, 0.11f), -16);
        _reward = Player(Tone(new[] { 523.25f, 659.25f, 783.99f }, 0.65f), -12);
        _finish = Player(Tone(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 1.5f), -12);
    }

    private AudioStreamPlayer Player(AudioStream stream, float volume)
    {
        var player = new AudioStreamPlayer { Stream = stream, VolumeDb = volume, MaxPolyphony = 4 };
        AddChild(player);
        return player;
    }

    public void Pick(int index)
    {
        if (Muted) return;
        _pick.PitchScale = 1 + index * 0.09f;
        _pick.Play();
    }

    public void Reward() { if (!Muted) _reward.Play(); }
    public void Finish() { if (!Muted) _finish.Play(); }
    public void ToggleMute()
    {
        Muted = !Muted;
        if (Muted) Stop();
    }

    public void Stop() { _pick.Stop(); _reward.Stop(); _finish.Stop(); }
    public void SetPaused(bool paused)
    {
        _pick.StreamPaused = _reward.StreamPaused = _finish.StreamPaused = paused;
    }

    public override void _ExitTree()
    {
        Stop();
        _pick.Stream = _reward.Stream = _finish.Stream = null;
    }

    private static AudioStreamWav Tone(float[] notes, float seconds)
    {
        const int rate = 22050;
        var data = new byte[(int)(seconds * rate) * 2];
        for (int i = 0; i < data.Length / 2; i++)
        {
            float t = i / (float)rate;
            double sample = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                double time = t - n * Math.Min(0.09, seconds / (notes.Length * 2));
                if (time >= 0) sample += Math.Sin(time * notes[n] * Math.Tau) * Math.Exp(-time * 5);
            }
            sample *= Math.Min(1, t / 0.008f) * Math.Min(1, (seconds - t) / 0.03f) / notes.Length;
            short value = (short)(Math.Clamp(sample, -1, 1) * 24000);
            data[i * 2] = (byte)(value & 255);
            data[i * 2 + 1] = (byte)(value >> 8);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Data = data };
    }
}
