namespace MediaBridge;

public static class YandexLike
{
    public const int Port = 9223;
    public const string Flag = "--remote-debugging-port=9223";
    private const string PageUrl = "music-application://";
    private const string Button = "const b = document.querySelector('[data-test-id=\"PLAYERBAR_DESKTOP\"] [data-test-id=\"LIKE_BUTTON\"]');";

    private static string _state = "none";
    private static DateTime _stateAt = DateTime.MinValue;

    public static bool IsApp(string appId)
    {
        return appId.Contains("yandex", StringComparison.OrdinalIgnoreCase) && appId.Contains("music", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> DebugStateAsync()
    {
        if ((DateTime.UtcNow - _stateAt).TotalSeconds < 5) return _state;
        _stateAt = DateTime.UtcNow;
        if (!IsApp(MediaSessionService.CurrentAppId)) return _state = "none";
        return _state = await DevTools.IsOpenAsync(Port) ? "ok" : "closed";
    }

    public static async Task<bool?> QueryAsync()
    {
        const string js = "(() => {" + Button +
            "  if (!b) return 'NO_BUTTON';" +
            "  return b.getAttribute('aria-pressed') === 'true' ? 'LIKED' : 'PLAIN';" +
            "})()";
        string? result = await DevTools.EvaluateAsync(Port, PageUrl, js);
        if (result == "LIKED") return true;
        if (result == "PLAIN") return false;
        return null;
    }

    public static async Task<bool?> ToggleAsync()
    {
        const string js = "(() => {" + Button +
            "  if (!b || b.disabled) return 'NO_BUTTON';" +
            "  const was = b.getAttribute('aria-pressed') === 'true';" +
            "  b.click();" +
            "  return was ? 'REMOVED' : 'ADDED';" +
            "})()";
        string? result = await DevTools.EvaluateAsync(Port, PageUrl, js);
        if (result == "ADDED") return true;
        if (result == "REMOVED") return false;
        return null;
    }

    public static void Heal()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "YandexMusic");
        if (!Directory.Exists(dir)) return;

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] folders =
        {
            Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs"),
            Path.Combine(appData, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };
        string prefix = dir + Path.DirectorySeparatorChar;
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var link in Directory.EnumerateFiles(folder, "*.lnk"))
            {
                string name = Path.GetFileName(link);
                if (!name.Contains("ндекс", StringComparison.Ordinal) && !name.Contains("andex", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    SpotifyFlags.FixShortcut(link, Flag, target =>
                        target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && !Path.GetFileName(target).StartsWith("Uninstall", StringComparison.OrdinalIgnoreCase));
                }
                catch { }
            }
        }
    }
}
