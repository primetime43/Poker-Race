using Godot;
using RacePoker.Core.Services;

namespace RacePoker.Godot;

public partial class Main
{
    private bool _releaseCheck, _releaseWindowReady;

    private void VerifyReleaseReadback()
    {
        var preferences = PlayerPreferences.Load("user://release-check-preferences.cfg");
        var history = new RaceHistoryService(ProjectSettings.GlobalizePath("user://release-check-race-history.json"));
        history.Load();
        bool valid = preferences.HumanPlayers == 4 && !preferences.IncludeComputer
            && history.Data.TotalRaces >= 7 && history.Data.Recent.Count >= 7
            && history.Data.Recent[0].Racers.Count == 4;
        GD.Print(valid ? $"READBACK PASS: a new process restored the four-player setup and {history.Data.TotalRaces} completed races."
            : "READBACK FAIL: expected saved release-check preferences and seven results.");
        GetTree().Quit(valid ? 0 : 1);
    }

    // Opt-in release diagnostics use separate preferences/history and real UI input.
    private async void RunWindowChecks()
    {
        try
        {
            Engine.TimeScale = 4;
            TogglePause();
            async Task KeyPress(Key key)
            {
                GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = true }, true);
                GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = false }, true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            await KeyPress(Key.P);
            if (_paused) throw new InvalidOperationException("P did not resume the race.");
            await KeyPress(Key.S);
            if (!_settingsOpen || !_paused) throw new InvalidOperationException("S did not open settings and pause.");
            bool originalSound = _preferences.SoundEnabled;
            await KeyPress(Key.M);
            if (_preferences.SoundEnabled == originalSound || PlayerPreferences.Load(_preferencesPath).SoundEnabled == originalSound)
                throw new InvalidOperationException("M did not toggle and save sound in settings.");
            await KeyPress(Key.M);
            var output = OS.GetEnvironment("RACE_POKER_CHECK_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) output = ProjectSettings.GlobalizePath("user://release-checks");
            System.IO.Directory.CreateDirectory(output);
            foreach (var size in new[] { new Vector2I(960, 675), new Vector2I(1152, 810), new Vector2I(1600, 900) })
            {
                DisplayServer.WindowSetSize(size);
                await ToSignal(GetTree().CreateTimer(0.2, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var image = GetViewport().GetTexture().GetImage();
                if (image.SavePng(System.IO.Path.Combine(output, $"settings-{size.X}x{size.Y}.png")) != Error.Ok)
                    throw new InvalidOperationException("Could not save window verification image.");
            }
            await KeyPress(Key.F11);
            await ToSignal(GetTree().CreateTimer(0.3, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
                throw new InvalidOperationException("F11 did not enter fullscreen while settings were open.");
            await KeyPress(Key.F11);
            await ToSignal(GetTree().CreateTimer(0.3, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed)
                throw new InvalidOperationException("F11 did not restore windowed mode.");
            DisplayServer.WindowSetSize(new Vector2I(1152, 810));
            await KeyPress(Key.Escape);
            if (_openDialog != null || _paused) throw new InvalidOperationException("Esc did not close settings and restore the race.");
            await KeyPress(Key.G);
            if (!_guideOpen) throw new InvalidOperationException("G did not open payouts.");
            await KeyPress(Key.Escape);
            await KeyPress(Key.H);
            if (!_historyOpen) throw new InvalidOperationException("H did not open history.");
            await KeyPress(Key.Escape);
            var reloaded = new RaceHistoryService(ProjectSettings.GlobalizePath("user://release-check-race-history.json"));
            reloaded.Load();
            if (reloaded.Data.TotalRaces != _history.Data.TotalRaces)
                throw new InvalidOperationException("History did not survive a fresh load.");
            GD.Print($"WINDOW CHECK PASS: three sizes, fullscreen round trip, P/S/M/Esc/G/H input and preference/history reload; saves={OS.GetUserDataDir()}");
            _releaseWindowReady = true;
        }
        catch (Exception error) { FailSmoke(error.Message); }
    }
}
