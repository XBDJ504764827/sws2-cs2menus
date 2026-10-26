using SwiftlyS2.Shared.Players;

namespace Sws2Cs2Menus.Contracts;

/// <summary>
/// Result of a request to open a menu.
/// </summary>
public enum MenuOpenResult
{
    /// <summary>The menu was opened.</summary>
    Opened,

    /// <summary>The player does not have the permission required by the menu.</summary>
    NoPermission,

    /// <summary>The player object is missing, disconnected, or a bot.</summary>
    InvalidPlayer,

    /// <summary>The menu definition does not contain any options visible to the player.</summary>
    NoOptions,

    /// <summary>The menu service is not ready.</summary>
    Unavailable
}

/// <summary>
/// Why a menu session ended.
/// </summary>
public enum MenuCloseReason
{
    /// <summary>The player closed the menu.</summary>
    Closed,

    /// <summary>The player navigated back to the parent menu.</summary>
    Back,

    /// <summary>The player selected an option that closes the menu.</summary>
    Selected,

    /// <summary>Another menu replaced the current menu.</summary>
    Replaced,

    /// <summary>The player became invalid or disconnected.</summary>
    Disconnected,

    /// <summary>The player object became invalid while the menu was open.</summary>
    InvalidPlayer,

    /// <summary>The map changed.</summary>
    MapUnload,

    /// <summary>The menu plugin was unloaded.</summary>
    PluginUnload,

    /// <summary>The menu was closed because permission was lost or denied.</summary>
    PermissionDenied
}

/// <summary>
/// Event data raised when a menu is opened or closed for a player.
/// </summary>
public sealed class MenuSessionEventArgs : EventArgs
{
    /// <summary>The player involved in the event.</summary>
    public required IPlayer Player { get; init; }

    /// <summary>The menu involved in the event.</summary>
    public required IMenuDefinition Menu { get; init; }

    /// <summary>Why the menu closed. Only meaningful for close events.</summary>
    public MenuCloseReason Reason { get; init; }
}

/// <summary>
/// Shared KZ menu API. Other plugins resolve this through
/// <c>IInterfaceManager.TryGetSharedInterface&lt;IMenuService&gt;(MenuServiceConstants.InterfaceKey, ...)</c>.
/// </summary>
public interface IMenuService
{
    /// <summary>Whether the service is loaded and can open menus.</summary>
    bool IsAvailable { get; }

    /// <summary>Raised after a menu is displayed for a player.</summary>
    event EventHandler<MenuSessionEventArgs>? MenuOpened;

    /// <summary>Raised after a menu session ends for a player.</summary>
    event EventHandler<MenuSessionEventArgs>? MenuClosed;

    /// <summary>
    /// Creates a menu builder. The returned builder is safe to cache and reuse for
    /// multiple players; each player gets an independent display session.
    /// </summary>
    IMenuBuilder CreateMenu(string title);

    /// <summary>
    /// Opens a menu as a new root menu, replacing any menu the player currently has open.
    /// </summary>
    MenuOpenResult Open(IPlayer player, IMenuDefinition menu);

    /// <summary>
    /// Opens a menu on top of the current menu. The back action returns to the current menu.
    /// </summary>
    MenuOpenResult OpenSubMenu(IPlayer player, IMenuDefinition menu);

    /// <summary>
    /// Navigates back one level. Returns false when the player is already at the root,
    /// in which case the caller may close the menu explicitly.
    /// </summary>
    bool Back(IPlayer player);

    /// <summary>Closes the player's current menu session.</summary>
    bool Close(IPlayer player);

    /// <summary>Returns whether the player has a menu open through this service.</summary>
    bool IsOpen(IPlayer player);

    /// <summary>Gets the menu currently displayed to the player, or null.</summary>
    IMenuDefinition? GetCurrentMenu(IPlayer player);

    /// <summary>Gets the player's menu stack from root to current.</summary>
    IReadOnlyList<IMenuDefinition> GetNavigationStack(IPlayer player);

    /// <summary>Closes every menu owned by this service.</summary>
    void CloseAll();
}
