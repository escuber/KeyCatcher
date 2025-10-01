
using KeyCatcher_acc;
using KeyCatcher_acc.services;
using KeyCatcher_acc.ViewModels;
using KeyCatcher_acc.Views;
using Microsoft.Maui.LifecycleEvents;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;


#if WINDOWS
using Microsoft.UI.Windowing;          // AppWindow, DisplayArea, etc.
using Windows.Graphics;                // SizeInt32
using WinRT.Interop;                 // WindowNative
#endif
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
         .ConfigureFonts(fonts =>
         {
             fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
             fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
             fonts.AddFont("FontAwesome6FreeBrands.otf", "FontAwesomeBrands");
             fonts.AddFont("FontAwesome6FreeRegular.otf", "FontAwesomeRegular");
             fonts.AddFont("FontAwesome6FreeSolid.otf", "FontAwesomeSolid");
         });
        builder.Services.AddSingleton<IBluetoothLE>(CrossBluetoothLE.Current);
        builder.Services.AddSingleton<IAdapter>(CrossBluetoothLE.Current.Adapter);

        // now your service can resolve IBluetoothLE automatically
       
        // Services
        builder.Services.AddSingleton<KeyCatcherSettingsService>();
        builder.Services.AddSingleton<KeyCatcherBleService>();
        builder.Services.AddSingleton<KeyCatcherWiFiService>();
        builder.Services.AddSingleton<CommHub>();
        builder.Services.AddSingleton<SendGate>();

        // ViewModels
        builder.Services.AddTransient<MainPageViewModel>();

        // Views
        builder.Services.AddTransient<mainpage>();

        ;
#if DEBUG
		builder.Logging.AddDebug();
#endif


        builder.ConfigureLifecycleEvents(events =>
        {
#if WINDOWS
    events.AddWindows(windows =>
    {
        // Fires once per WinUI window (usually one for a MAUI app)
        windows.OnWindowCreated(nativeWindow =>
        {
            // nativeWindow is Microsoft.UI.Xaml.Window  (no .Handler!)
            var hWnd     = WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWin   = AppWindow.GetFromWindowId(windowId);

            appWin?.Resize(new SizeInt32(672, 1104));   // 900 × 1200 px
        });
    });
#endif
        });
        return builder.Build();
	}
}
