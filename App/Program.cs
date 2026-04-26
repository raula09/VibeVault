using Tessera;
using VibeVault;

var app = TesseraApplication.CreateBuilder()
    .UseApp<VibeVaultApp>()
    .ConfigureRuntime(static runtime =>
    {
        runtime.Theme = VibeVaultTheme.DefaultTheme;
        runtime.PointerActivationPolicy = PointerActivationPolicy.SingleClick;
        runtime.Screen = new ScreenOptions
        {
            AltScreen = true,
            WindowTitle = "VibeVault",
            EnableFocusReporting = false,
            EnableBracketedPaste = false,
            MouseTracking = MouseTrackingMode.None
        };
    })
    .Build();

await app.RunAsync();
