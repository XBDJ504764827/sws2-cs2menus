using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Plugins;
using Sws2Cs2Menus.Configuration;
using Sws2Cs2Menus.Contracts;
using Sws2Cs2Menus.Services;

namespace Sws2Cs2Menus;

/// <summary>
/// Main plugin entry point. Plugin metadata lives in <c>Metadata.cs</c>.
/// </summary>
public partial class Sws2Cs2Menus : BasePlugin
{
    private MenuConfig _config = new();
    private MenuServiceProxy _menus = null!;
    private ServiceProvider _provider = null!;
    private IDisposable? _configSubscription;
    private HudAddonMountService _hudAddon = null!;
    private CancellationTokenSource? _mountCheck;

    private volatile bool _loaded;

    public Sws2Cs2Menus(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeWithTemplate("config.jsonc", "config.template.jsonc")
            .Configure(builder =>
            {
                builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
            });

        var services = new ServiceCollection();
        services.AddSwiftly(Core)
            .AddOptionsWithValidateOnStart<MenuConfig>()
            .BindConfiguration("sws2-cs2menus");

        _provider = services.BuildServiceProvider();
        var options = _provider.GetRequiredService<IOptionsMonitor<MenuConfig>>();
        _config = options.CurrentValue;

        _menus = new MenuServiceProxy(CreateBackend(Core, _config));
        _hudAddon = new HudAddonMountService(Path.Combine(Core.PluginPath, "resources", "hud-addon.json"), Core.Logger,
            () => Core.ConVar.FindAsString("mm_extra_addons")?.ValueAsString,
            value =>
            {
                // Assign through the engine's console path, so MAM's native change callback
                // runs. SwiftlyS2's direct ConVar setters can bypass callbacks. MountList
                // validates the entire value before it is embedded into this command.
                Core.Engine.ExecuteCommand($"mm_extra_addons \"{value}\"\n");
            }, config => HudLayoutMenuService.IsLayoutAvailable(Core, config));

        _configSubscription = options.OnChange(newConfig =>
        {
            Core.Scheduler.NextTick(() =>
            {
                if (!_loaded)
                {
                    return;
                }

                var previous = _config;
                _config = newConfig;
                _menus.ApplyConfig(newConfig);
                if (previous.Menu.Backend != newConfig.Menu.Backend || previous.Menu.HudLayoutPath != newConfig.Menu.HudLayoutPath
                    || previous.Menu.HudLayoutWorkshopId != newConfig.Menu.HudLayoutWorkshopId)
                    _menus.ReplaceBackend(CreateBackend(Core, newConfig));
                _hudAddon.Reset();
                CheckHudResources();

                Core.Logger.LogInformation(
                    "[sws2-cs2menus] Configuration reloaded (page size: {PageSize}, sounds: {Sounds})",
                    newConfig.Menu.DefaultPageSize,
                    newConfig.Menu.PlaySound);
            });
        });

        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.Event.OnMapUnload += OnMapUnload;
        Core.Event.OnMapLoad += OnHudMapLoad;

        _loaded = true;
        _mountCheck = Core.Scheduler.DelayAndRepeatBySeconds(1, 5, CheckHudResources);

        Core.Logger.LogInformation("[sws2-cs2menus] Plugin loaded (hotReload: {HotReload})", hotReload);
    }

    private void OnHudMapLoad(IOnMapLoadEvent @event)
    {
        _hudAddon.Reset();
        Core.Scheduler.NextTick(CheckHudResources);
    }

    private void CheckHudResources()
    {
        if (!_loaded) return;
        _hudAddon.Check(_config);
        if (!string.Equals(_config.Menu.Backend, "classic", StringComparison.OrdinalIgnoreCase)
            && !_menus.UsesHud && HudLayoutMenuService.IsLayoutAvailable(Core, _config))
        {
            _menus.ReplaceBackend(new HudLayoutMenuService(Core, _config));
            Core.Logger.LogInformation("[sws2-cs2menus] HUD resources became available; switched shared menus to Custom HUD");
        }
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        if (_menus == null)
        {
            return;
        }

        interfaceManager.AddSharedInterface<IMenuService, MenuServiceProxy>(
            MenuServiceConstants.InterfaceKey,
            _menus);
    }

    private static IMenuBackend CreateBackend(ISwiftlyCore core, MenuConfig config)
    {
        var mode = (config.Menu.Backend ?? "auto").Trim().ToLowerInvariant();

        if (mode == "classic")
        {
            core.Logger.LogInformation("[sws2-cs2menus] Using classic center-HTML menu backend");
            return new ClassicMenuBackend(core, config);
        }

        var hud = new HudLayoutMenuService(core, config);
        if (HudLayoutMenuService.IsLayoutAvailable(core, config))
        {
            core.Logger.LogInformation("[sws2-cs2menus] Using custom_hud_layout menu backend");
            return hud;
        }

        hud.Dispose();
        core.Logger.LogInformation(
            "[sws2-cs2menus] Custom HUD layout not mounted ({Path}); using classic center-HTML menu backend",
            config.Menu.HudLayoutPath);
        return new ClassicMenuBackend(core, config);
    }

    public override void Unload()
    {
        _loaded = false;

        Core.Event.OnClientDisconnected -= OnClientDisconnected;
        Core.Event.OnMapUnload -= OnMapUnload;
        Core.Event.OnMapLoad -= OnHudMapLoad;
        _mountCheck?.Cancel();
        _mountCheck = null;

        _configSubscription?.Dispose();
        _configSubscription = null;

        _menus?.Dispose();
        _provider?.Dispose();
    }
}
