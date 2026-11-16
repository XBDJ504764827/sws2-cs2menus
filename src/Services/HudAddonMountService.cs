using Microsoft.Extensions.Logging;
using Sws2Cs2Menus.Configuration;

namespace Sws2Cs2Menus.Services;

/// <summary>Uses MAM's public append command, preserving all unrelated addons and configuration files.</summary>
internal sealed class HudAddonMountService
{
    private readonly ILogger _logger;
    private readonly Func<string?> _getAddons;
    private readonly Action<string> _execute;
    private readonly Func<MenuConfig, bool> _layoutReady;
    private readonly Func<DateTimeOffset> _now;
    private readonly string _manifestPath;
    private DateTimeOffset _nextAttempt;
    private string _lastState = "";
    public string Status { get; private set; } = "Waiting for initialization";

    internal HudAddonMountService(string manifestPath, ILogger logger, Func<string?> getAddons,
        Action<string> execute, Func<MenuConfig, bool> layoutReady, Func<DateTimeOffset>? now = null)
    {
        _manifestPath = manifestPath;
        _logger = logger;
        _getAddons = getAddons;
        _execute = execute;
        _layoutReady = layoutReady;
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public void Reset() { _nextAttempt = DateTimeOffset.MinValue; _lastState = ""; }

    public void Check(MenuConfig config)
    {
        if (!config.Menu.AutoMountHudAddon || string.Equals(config.Menu.Backend, "classic", StringComparison.OrdinalIgnoreCase))
        {
            Report("Automatic HUD mounting disabled", false);
            return;
        }
        try
        {
            var id = HudAddonSettings.Resolve(config.Menu.HudLayoutWorkshopId, _manifestPath);
            if (id.Length == 0)
            {
                Report("HUD Workshop ID is not registered. Set Menu.HudLayoutWorkshopId once, or package resources/hud-addon.json. Local mounted resources still work.", true);
                return;
            }
            var addons = _getAddons();
            if (addons is null)
            {
                Report("MultiAddonManager is unavailable; automatic HUD mounting will retry when it loads.", true);
                return;
            }
            if (HudAddonSettings.Contains(addons, id))
            {
                Report(_layoutReady(config)
                    ? $"HUD addon {id} registered; layout ready"
                    : $"HUD addon {id} registered; waiting for layout download/mount", false);
                return;
            }
            if (_now() < _nextAttempt) return;
            _nextAttempt = _now().AddSeconds(30);
            _execute($"mm_add_addon {id}");
            Report($"Requested HUD addon {id} through MultiAddonManager; waiting for registration and resources", false);
        }
        catch (Exception exception)
        {
            Report($"HUD addon mounting failed: {exception.Message}", true);
        }
    }

    private void Report(string state, bool warning)
    {
        Status = state;
        if (_lastState == state) return;
        _lastState = state;
        if (warning) _logger.LogWarning("[sws2-cs2menus] {Status}", state);
        else _logger.LogInformation("[sws2-cs2menus] {Status}", state);
    }
}
