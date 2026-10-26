using Sws2Cs2Menus.Contracts;

namespace Sws2Cs2Menus.Menus;

internal sealed class MenuDefinition : IMenuDefinition
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public string? Comment { get; init; }

    public int PageSize { get; init; }

    public string? Permission { get; init; }

    public bool ShowBack { get; init; }

    public bool ShowClose { get; init; }

    public bool CloseOnSelect { get; init; }

    public IReadOnlyList<IMenuOptionDefinition> Options { get; init; } = [];

    public Func<MenuContext, ValueTask>? Closed { get; init; }

    public override string ToString() => $"Menu({Id}, {Title})";
}

internal sealed class MenuOptionDefinition : IMenuOptionDefinition
{
    public required string Id { get; init; }

    public required string Text { get; init; }

    public string? Comment { get; init; }

    public string? Color { get; init; }

    public string? Permission { get; init; }

    public bool Hidden { get; init; }

    public bool Enabled { get; init; } = true;

    public object? Tag { get; init; }

    public IMenuDefinition? SubMenu { get; init; }

    public Func<MenuOptionContext, ValueTask>? Selected { get; init; }

    public override string ToString() => $"Option({Id}, {Text})";
}
