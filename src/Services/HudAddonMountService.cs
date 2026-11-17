using Microsoft.Extensions.Logging;
using Sws2Cs2Menus.Configuration;

namespace Sws2Cs2Menus.Services;

/// <summary>Updates MAM's addon ConVar through its normal change callback to register AND refresh resources.</summary>
internal sealed class HudAddonMountService
{
    private readonly ILogger _logger;
    private readonly Func<string?> _getAddons;
    private readonly Action<string> _setAddons;
    private readonly Func<MenuConfig, bool> _layoutReady;
    private readonly Func<DateTimeOffset> _now;
    private readonly string _manifestPath;
    private DateTimeOffset _nextAttempt;
    private DateTimeOffset _waitingSince;
    private bool _refreshRequested;
    private string _lastState = "";
    public string Status { get; private set; } = "Waiting for initialization";

    internal HudAddonMountService(string manifestPath, ILogger logger, Func<string?> getAddons,
        Action<string> setAddons, Func<MenuConfig, bool> layoutReady, Func<DateTimeOffset>? now = null)
    {
        _manifestPath = manifestPath;
        _logger = logger;
        _getAddons = getAddons;
        _setAddons = setAddons;
        _layoutReady = layoutReady;
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public void Reset()
    {
        _nextAttempt = DateTimeOffset.MinValue;
        _refreshRequested = false;
        _lastState = "";
    }

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
                if (_layoutReady(config))
                {
                    Report($"HUD addon {id} registered; layout ready", false);
                    return;
                }
                if (_refreshRequested)
                {
                    var overdue = _now() - _waitingSince >= TimeSpan.FromSeconds(60);
                    Report(overdue
                        ? $"HUD addon {id} registered but layout still unavailable: {config.Menu.HudLayoutPath}. Check MultiAddonManager download errors, Steam access and compiled addon contents; registration alone does not confirm mounting."
                        : $"HUD addon {id} registered; waiting for layout download/mount after refresh", overdue);
                    return;
                }
            }
            if (_now() < _nextAttempt) return;
            _nextAttempt = _now().AddSeconds(30);
            // mm_add_addon defaults bRefresh=false: it only changes the list. A normal
            // mm_extra_addons write invokes MAM's callback and RefreshAddons instead.
            _setAddons(HudAddonSettings.MountList(addons, id));
            _refreshRequested = true;
            _waitingSince = _now();
            Report($"Requested HUD addon {id} through mm_extra_addons change callback; waiting for registration and resources", false);
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
