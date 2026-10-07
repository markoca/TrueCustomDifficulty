using System.Windows.Threading;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Keeps short-lived radar feedback separate from the radar's normal instruction/status text.
/// Transient messages automatically expire and reveal the previous text again.
/// </summary>
internal sealed class RadarStatusMessage
{
    private readonly DispatcherTimer _timer;
    private readonly Action _changed;
    private string _persistentText = string.Empty;
    private string _transientText = string.Empty;

    public RadarStatusMessage(Action changed)
    {
        _changed = changed;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _timer.Tick += (_, _) => ClearTransient();
    }

    public string Text => string.IsNullOrWhiteSpace(_transientText)
        ? _persistentText
        : _transientText;

    public void SetPersistent(string text)
    {
        _persistentText = text ?? string.Empty;
        _changed();
    }

    public void ShowTransient(string text)
    {
        _transientText = text ?? string.Empty;
        _timer.Stop();

        if (!string.IsNullOrWhiteSpace(_transientText))
            _timer.Start();

        _changed();
    }

    public void Clear()
    {
        _timer.Stop();
        _transientText = string.Empty;
        _persistentText = string.Empty;
        _changed();
    }

    public void ClearTransient()
    {
        if (string.IsNullOrWhiteSpace(_transientText) && !_timer.IsEnabled)
            return;

        _timer.Stop();
        _transientText = string.Empty;
        _changed();
    }
}
