namespace Sws2Cs2Menus.Configuration;

/// <summary>
/// Plugin configuration bound from the "sws2-cs2menus" section of config.jsonc.
/// </summary>
public sealed class MenuConfig
{
    /// <summary>Enable debug logging.</summary>
    public bool Debug { get; set; }

    /// <summary>Menu rendering and behavior settings.</summary>
    public MenuDefaults Menu { get; set; } = new();

    /// <summary>Settings for the built-in demo menu.</summary>
    public MenuDemo Demo { get; set; } = new();
}

/// <summary>
/// Defaults applied to every menu created through the service.
/// </summary>
public sealed class MenuDefaults
{
    /// <summary>Default number of options per page. Clamped to 1-5.</summary>
    public int DefaultPageSize { get; set; } = 5;

    /// <summary>Whether menu interaction sounds are played.</summary>
    public bool PlaySound { get; set; } = true;

    /// <summary>Whether the Back action is available for submenus by default.</summary>
    public bool ShowBack { get; set; } = true;

    /// <summary>Whether menus can be closed by default.</summary>
    public bool ShowClose { get; set; } = true;

    /// <summary>Whether a selection closes the menu by default.</summary>
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>Optional default comment shown when an option has no comment.</summary>
    public string DefaultComment { get; set; } = string.Empty;

    /// <summary>Color settings shared by all menus.</summary>
    public MenuColors Colors { get; set; } = new();

}

/// <summary>
/// Menu palette. Values use the #RRGGBB format.
/// </summary>
public sealed class MenuColors
{
    /// <summary>Color of the menu title.</summary>
    public string Title { get; set; } = "#FFD700";

    /// <summary>Color of the menu comment.</summary>
    public string Comment { get; set; } = "#AAAAAA";

    /// <summary>Color applied to the focused option.</summary>
    public string Focused { get; set; } = "#FFD700";

    /// <summary>Color of disabled options.</summary>
    public string Disabled { get; set; } = "#808080";
}

/// <summary>
/// Demo command settings. The demo menu is useful for verifying the service
/// without installing another plugin.
/// </summary>
public sealed class MenuDemo
{
    /// <summary>Whether the demo command is registered.</summary>
    public bool EnableCommand { get; set; } = true;

    /// <summary>Optional permission required to use the demo command.</summary>
    public string CommandPermission { get; set; } = string.Empty;
}
