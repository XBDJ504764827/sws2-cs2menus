namespace Sws2Cs2Menus.Contracts;

/// <summary>
/// Constants shared by the menu service and its consumers.
/// </summary>
public static class MenuServiceConstants
{
    /// <summary>
    /// Key used to register and resolve <see cref="IMenuService"/> through SwiftlyS2's
    /// shared interface manager.
    /// </summary>
    public const string InterfaceKey = "sws2-cs2menus";

    /// <summary>
    /// Optional shared <see cref="Func{TResult}"/> of bool reporting mounted Custom HUD readiness.
    /// A separate, versioned key keeps old exported menu assemblies binary compatible.
    /// Resolve alongside the menu service; a missing callback means HUD is unavailable.
    /// </summary>
    public const string CustomHudInterfaceKey = "sws2-cs2menus.custom-hud.v1";

    /// <summary>
    /// Default number of options shown on one page.
    /// </summary>
    public const int DefaultPageSize = 5;
}
