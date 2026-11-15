namespace Sws2Cs2Menus.Contracts;

/// <summary>Presentation hints used by Custom HUD renderers.</summary>
public enum MenuLayout
{
    List,
    Dashboard,
    Confirmation
}

/// <summary>Optional visual metadata supplied through an option's Tag.</summary>
public sealed record MenuOptionAppearance(string Glyph, bool Destructive = false);
