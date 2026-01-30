using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;
using Celeste.Core.Services;

namespace Celeste.Android;

/// <summary>
/// GameActivity - Inicia e hospeda o MonoGame Game principal em fullscreen.
/// Chamada pelo host Kotlin quando usuário clicar "Iniciar Jogo".
/// </summary>
[Activity(
    Label = "@string/app_name",
    MainLauncher = false,
    ScreenOrientation = ScreenOrientation.Landscape,
    LaunchMode = LaunchMode.SingleInstance,
    Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
    ConfigChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden)]
public class GameActivity : Microsoft.Xna.Framework.AndroidGameActivity
{
    private CelesteGame? _game;
    private string? _contentRoot;
    private bool _fpsEnabled = false;
    private bool _verboseLogs = false;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Obter extras do intent
        _contentRoot = Intent?.GetStringExtra("contentRoot");
        _fpsEnabled = Intent?.GetBooleanExtra("fpsEnabled", false) ?? false;
        _verboseLogs = Intent?.GetBooleanExtra("verboseLogs", false) ?? false;

        if (string.IsNullOrEmpty(_contentRoot))
        {
            Android.Util.Log.Error("CelesteGame", "ContentRoot não foi passado no Intent");
            Finish();
            return;
        }

        // Inicializar game
        _game = new CelesteGame(_contentRoot, _fpsEnabled, _verboseLogs);
        SetContentView(_game);

        // Fullscreen imersivo
        ApplyFullscreen();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplyFullscreen();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            ApplyFullscreen();
        }
    }

    private void ApplyFullscreen()
    {
        try
        {
            var flags = (Android.Views.SystemUiFlags)
                (Android.Views.SystemUiFlags.ImmersiveSticky |
                 Android.Views.SystemUiFlags.LayoutStable |
                 Android.Views.SystemUiFlags.LayoutHideNavigation |
                 Android.Views.SystemUiFlags.HideNavigation);

            Window?.DecorView.SystemUiVisibility = flags;
        }
        catch { }
    }
}
