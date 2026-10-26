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
    private MenuService _menus = null!;
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

        _menus = new MenuService(Core, _config);

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

        interfaceManager.AddSharedInterface<IMenuService, MenuService>(
            MenuServiceConstants.InterfaceKey,
            _menus);
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
