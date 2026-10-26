using SwiftlyS2.Shared.Players;

namespace Sws2Cs2Menus.Contracts;

/// <summary>
/// Context passed to menu callbacks that are not tied to a single option.
/// </summary>
public class MenuContext
{
    /// <summary>The player the menu belongs to.</summary>
    public required IPlayer Player { get; init; }

    /// <summary>The menu definition.</summary>
    public required IMenuDefinition Menu { get; init; }

    /// <summary>The shared menu service.</summary>
    public required IMenuService Service { get; init; }

    /// <summary>
    /// Why the menu is closing. Only meaningful for closed callbacks.
    /// </summary>
    public MenuCloseReason Reason { get; init; }
}

/// <summary>
/// Context passed to option selection callbacks.
/// </summary>
public sealed class MenuOptionContext : MenuContext
{
    /// <summary>The option that was selected.</summary>
    public required IMenuOptionDefinition Option { get; init; }
}

/// <summary>
/// Immutable menu definition built by <see cref="IMenuBuilder"/>.
/// </summary>
public interface IMenuDefinition
{
    /// <summary>Stable identifier used for diagnostics and callbacks.</summary>
    string Id { get; }

    /// <summary>Title shown at the top of the menu.</summary>
    string Title { get; }

    /// <summary>Optional comment shown under the title.</summary>
    string? Comment { get; }

    /// <summary>Maximum number of options shown on one page (1-5).</summary>
    int PageSize { get; }

    /// <summary>Optional permission required to open this menu.</summary>
    string? Permission { get; }

    /// <summary>Whether a Back action is available when the menu has a parent.</summary>
    bool ShowBack { get; }

    /// <summary>Whether the player can close this menu.</summary>
    bool ShowClose { get; }

    /// <summary>Whether selecting a regular option closes the whole menu.</summary>
    bool CloseOnSelect { get; }

    /// <summary>The menu's options in display order.</summary>
    IReadOnlyList<IMenuOptionDefinition> Options { get; }

    /// <summary>Optional callback invoked when this menu is closed for a player.</summary>
    Func<MenuContext, ValueTask>? Closed { get; }
}

/// <summary>
/// Immutable menu option definition.
/// </summary>
public interface IMenuOptionDefinition
{
    /// <summary>Stable identifier used for diagnostics and callbacks.</summary>
    string Id { get; }

    /// <summary>Text displayed for the option.</summary>
    string Text { get; }

    /// <summary>Optional comment displayed while the option is highlighted.</summary>
    string? Comment { get; }

    /// <summary>Optional text colour in #RRGGBB format.</summary>
    string? Color { get; }

    /// <summary>Optional permission required to see this option.</summary>
    string? Permission { get; }

    /// <summary>Whether the option is always hidden.</summary>
    bool Hidden { get; }

    /// <summary>Whether the option is shown but cannot be selected.</summary>
    bool Enabled { get; }

    /// <summary>Optional caller data associated with the option.</summary>
    object? Tag { get; }

    /// <summary>Optional submenu opened when the option is selected.</summary>
    IMenuDefinition? SubMenu { get; }

    /// <summary>Optional selection callback.</summary>
    Func<MenuOptionContext, ValueTask>? Selected { get; }
}
