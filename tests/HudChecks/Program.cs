using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared.Players;
using Sws2Cs2Menus.Configuration;
using Sws2Cs2Menus.Contracts;
using Sws2Cs2Menus.Services;

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}

var legacy = new LegacyBackend();
Check(typeof(IMenuService).Assembly.GetName().Version == new Version(1, 0, 0, 0), "Shared ABI version stays stable across plugin preview/release versions");
Check(typeof(IMenuService).GetProperty("IsCustomHudAvailable") is null, "HUD capability must not extend the shared base menu interface");
using var proxy = new MenuServiceProxy(legacy);
Check(proxy.IsAvailable && !proxy.IsCustomHudAvailable && !proxy.UsesHud, "Classic fallback cannot satisfy a HUD-only consumer");
var services = new ServiceCollection();
services.AddKeyedSingleton<IMenuService>(MenuServiceConstants.InterfaceKey, proxy);
using (var oldProvider = services.BuildServiceProvider())
    Check(oldProvider.GetKeyedService<Func<bool>>(MenuServiceConstants.CustomHudInterfaceKey) is null, "Older providers have no optional HUD capability");
services.AddKeyedSingleton<Func<bool>>(MenuServiceConstants.CustomHudInterfaceKey, () => proxy.IsCustomHudAvailable);
using var provider = services.BuildServiceProvider();
var sharedMenus = provider.GetRequiredKeyedService<IMenuService>(MenuServiceConstants.InterfaceKey);
var hudReady = provider.GetRequiredKeyedService<Func<bool>>(MenuServiceConstants.CustomHudInterfaceKey);
Check(sharedMenus.IsAvailable && !hudReady(), "Keyed BCL callback rejects classic rendering even with an older exported menu DLL");
var hud = new HudBackend();
proxy.ReplaceBackend(hud);
Check(legacy.Closed && legacy.Disposed, "Backend switch closes old sessions before advertising HUD");
Check(proxy.IsCustomHudAvailable && proxy.UsesHud && hudReady(), "Mounted HUD is advertised through the proxy and callback");
hud.Ready = false;
Check(!proxy.IsCustomHudAvailable && !hudReady(), "Lost/unmounted layout immediately withdraws capability");
hud.Ready = true;
hud.Available = false;
Check(!proxy.IsCustomHudAvailable && !hudReady(), "Stopped HUD cannot be used even if its resources remain mounted");
hud.Available = true;
Check(proxy.IsCustomHudAvailable, "Readiness recovers without replacing the shared interface");
var fallback = new LegacyBackend();
proxy.ReplaceBackend(fallback);
Check(hud.Closed && hud.Disposed && !proxy.IsCustomHudAvailable && !hudReady(), "Switching back to classic closes HUD sessions and withdraws capability");
proxy.ReplaceBackend(new HudBackend());
Check(hudReady(), "Previously resolved callback follows subsequent backend replacements");
proxy.Dispose();
Check(!hudReady(), "Disposed backend withdraws readiness from existing consumers");
Console.WriteLine($"{checks} HUD capability regression checks passed.");

// Classic backend uses the internal capability default without extending IMenuService.
class LegacyBackend : IMenuBackend
{
    public bool Available { get; set; } = true;
    public bool Closed { get; private set; }
    public bool Disposed { get; private set; }
    public bool IsAvailable => Available;
    public event EventHandler<MenuSessionEventArgs>? MenuOpened { add { } remove { } }
    public event EventHandler<MenuSessionEventArgs>? MenuClosed { add { } remove { } }
    public IMenuBuilder CreateMenu(string title) => throw new NotSupportedException();
    public MenuOpenResult Open(IPlayer player, IMenuDefinition menu) => MenuOpenResult.Opened;
    public MenuOpenResult OpenSubMenu(IPlayer player, IMenuDefinition menu) => MenuOpenResult.Opened;
    public MenuOpenResult ReplaceCurrent(IPlayer player, IMenuDefinition menu) => MenuOpenResult.Opened;
    public bool Back(IPlayer player) => false;
    public bool Close(IPlayer player) => true;
    public bool IsOpen(IPlayer player) => false;
    public IMenuDefinition? GetCurrentMenu(IPlayer player) => null;
    public IReadOnlyList<IMenuDefinition> GetNavigationStack(IPlayer player) => [];
    public void CloseAll() => Closed = true;
    public void ApplyConfig(MenuConfig config) { }
    public void Shutdown(MenuCloseReason reason) => Available = false;
    public void ClearSessions(MenuCloseReason reason) { }
    public void CleanupPlayer(int playerId, MenuCloseReason reason = MenuCloseReason.Disconnected) { }
    public void OnPlayerJoined(IPlayer player) { }
    public void Dispose() { Disposed = true; Available = false; }
}

sealed class HudBackend : LegacyBackend, IMenuBackend
{
    public bool Ready { get; set; } = true;
    public bool IsCustomHudAvailable => Ready;
}
