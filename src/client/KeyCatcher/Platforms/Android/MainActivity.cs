//using Android.App;
//using Android.Content.PM;
//using Android.OS;

//namespace BleTestingAndDev;

//[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
//public class MainActivity : MauiAppCompatActivity
//{
//}
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace BleTestingAndDev;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateHidden)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Make the window resize for the keyboard so popup buttons stay visible
        Window?.SetSoftInputMode(SoftInput.AdjustResize | SoftInput.StateHidden);

        // Handle a share intent if we were launched with one
        TryHandleShareIntent(Intent);
    }

    // Handle a new share intent when the app is already running (LaunchMode.SingleTop)
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        TryHandleShareIntent(intent);
    }

    static void TryHandleShareIntent(Intent? intent)
    {
        try
        {
            if (intent?.Action == Intent.ActionSend && intent.Type == "text/plain")
            {
                var sharedText = intent.GetStringExtra(Intent.ExtraText);
                if (!string.IsNullOrWhiteSpace(sharedText))
                {
                    Microsoft.Maui.Storage.Preferences.Set("PendingShareText", sharedText);

                    var app = Microsoft.Maui.Controls.Application.Current;
                    app?.Dispatcher.Dispatch(async () =>
                    {
                        var shell = app.Windows
                            .OfType<Microsoft.Maui.Controls.Window>()
                            .Select(w => w.Page)
                            .OfType<Microsoft.Maui.Controls.Shell>()
                            .FirstOrDefault();

                        if (shell != null)
                        {
                            await shell.GoToAsync("HomePage");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Shell not found; navigation not possible.");
                        }
                    });
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Share Intent] Exception: {ex}");
        }
    }
}

