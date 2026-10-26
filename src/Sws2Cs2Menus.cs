using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
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

        _configSubscription = options.OnChange(newConfig =>
        {
            Core.Scheduler.NextTick(() =>
            {
                if (!_loaded)
                {
                    return;
                }

                _config = newConfig;
                _menus.ApplyConfig(newConfig);

                Core.Logger.LogInformation(
                    "[sws2-cs2menus] Configuration reloaded (page size: {PageSize}, sounds: {Sounds})",
                    newConfig.Menu.DefaultPageSize,
                    newConfig.Menu.PlaySound);
            });
        });

        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.Event.OnMapUnload += OnMapUnload;

        _loaded = true;

        Core.Logger.LogInformation("[sws2-cs2menus] Plugin loaded (hotReload: {HotReload})", hotReload);
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
        if (mode == "hudlayout")
        {
            if (!HudLayoutMenuService.IsLayoutAvailable(core, config))
            {
                core.Logger.LogWarning(
                    "[sws2-cs2menus] HUD layout backend forced, but layout was not found: {Path}",
                    config.Menu.HudLayoutPath);
            }

            core.Logger.LogInformation("[sws2-cs2menus] Using custom_hud_layout menu backend");
            return hud;
        }

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

        _configSubscription?.Dispose();
        _configSubscription = null;

        _menus?.Dispose();
        _provider?.Dispose();
    }
}
