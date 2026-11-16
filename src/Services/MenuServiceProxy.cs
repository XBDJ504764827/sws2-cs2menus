using SwiftlyS2.Shared.Players;
using Sws2Cs2Menus.Configuration;
using Sws2Cs2Menus.Contracts;

namespace Sws2Cs2Menus.Services;

/// <summary>
/// Forwards the shared menu API to the selected renderer backend.
/// </summary>
internal sealed class MenuServiceProxy : IMenuBackend
{
    private IMenuBackend _backend;

    public bool UsesHud => _backend is HudLayoutMenuService;

    public MenuServiceProxy(IMenuBackend backend)
    {
        _backend = backend;
        _backend.MenuOpened += ForwardOpened;
        _backend.MenuClosed += ForwardClosed;
    }

    private void ForwardOpened(object? sender, MenuSessionEventArgs args) => MenuOpened?.Invoke(this, args);
    private void ForwardClosed(object? sender, MenuSessionEventArgs args) => MenuClosed?.Invoke(this, args);

    public void ReplaceBackend(IMenuBackend backend)
    {
        _backend.CloseAll();
        _backend.MenuOpened -= ForwardOpened;
        _backend.MenuClosed -= ForwardClosed;
        _backend.Dispose();
        _backend = backend;
        _backend.MenuOpened += ForwardOpened;
        _backend.MenuClosed += ForwardClosed;
    }

    public bool IsAvailable => _backend.IsAvailable;

    public event EventHandler<MenuSessionEventArgs>? MenuOpened;

    public event EventHandler<MenuSessionEventArgs>? MenuClosed;

    public IMenuBuilder CreateMenu(string title) => _backend.CreateMenu(title);

    public MenuOpenResult Open(IPlayer player, IMenuDefinition menu) => _backend.Open(player, menu);

    public MenuOpenResult OpenSubMenu(IPlayer player, IMenuDefinition menu) => _backend.OpenSubMenu(player, menu);

    public bool Back(IPlayer player) => _backend.Back(player);

    public bool Close(IPlayer player) => _backend.Close(player);

    public bool IsOpen(IPlayer player) => _backend.IsOpen(player);

    public IMenuDefinition? GetCurrentMenu(IPlayer player) => _backend.GetCurrentMenu(player);

    public IReadOnlyList<IMenuDefinition> GetNavigationStack(IPlayer player) => _backend.GetNavigationStack(player);

    public void CloseAll() => _backend.CloseAll();

    public void ApplyConfig(MenuConfig config) => _backend.ApplyConfig(config);

    public void Shutdown(MenuCloseReason reason) => _backend.Shutdown(reason);

    public void ClearSessions(MenuCloseReason reason) => _backend.ClearSessions(reason);

    public void CleanupPlayer(int playerId, MenuCloseReason reason = MenuCloseReason.Disconnected)
        => _backend.CleanupPlayer(playerId, reason);

    public void OnPlayerJoined(IPlayer player) => _backend.OnPlayerJoined(player);

    public void Dispose() => _backend.Dispose();
}
