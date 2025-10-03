using CommunityToolkit.Maui.Views;
using KeyCatcher.services;

namespace KeyCatcher.Popups;

public partial class CountdownPopup : Popup
{
    private int _seconds;
    private readonly SendGate _gate;

    public CountdownPopup(int seconds, SendGate gate)
    {
        InitializeComponent();
        _seconds = seconds;
        _gate = gate;

       // _gate.Block(_seconds);
        Device.StartTimer(TimeSpan.FromSeconds(1), OnTick);
        UpdateLabel();
    }

    private bool OnTick()
    {
        _seconds--;
        UpdateLabel();

        if (_seconds <= 0)
        {
            CloseAsync();
            return false;
        }
        return true;
    }

    private void UpdateLabel() =>
        CountdownLabel.Text = $"{_seconds}s";

    private void Stop_Clicked(object sender, EventArgs e)
    {
        _gate.StopBlock();
        CloseAsync();
    }
}
public class SendHealthGate
{
    private readonly SemaphoreSlim _sem = new(1, 1);

    // For real messages: must wait
    public async Task<T> RunSendAsync<T>(Func<Task<T>> action)
    {
        await _sem.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _sem.Release();
        }
    }

    // For health checks: skip if busy
    public async Task<T?> TryRunHealthCheckAsync<T>(Func<Task<T>> action)
    {
        if (!await _sem.WaitAsync(0)) // immediate try
            return default;           // skip gracefully

        try
        {
            return await action();
        }
        finally
        {
            _sem.Release();
        }
    }
}

