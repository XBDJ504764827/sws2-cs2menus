namespace Sws2Cs2Menus.Contracts;

/// <summary>
/// Convenience overloads that avoid <see cref="ValueTask"/> ceremony in simple callbacks.
/// </summary>
public static class MenuBuilderExtensions
{
    /// <summary>Adds an option with a synchronous callback.</summary>
    public static IMenuBuilder AddOption(this IMenuBuilder builder, string id, string text, Action<MenuOptionContext> onSelected)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(onSelected);

        return builder.AddOption(id, text, context =>
        {
            onSelected(context);
            return ValueTask.CompletedTask;
        });
    }

    /// <summary>Sets a synchronous menu-closed callback.</summary>
    public static IMenuBuilder OnClosed(this IMenuBuilder builder, Action<MenuContext> handler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(handler);

        return builder.OnClosed(context =>
        {
            handler(context);
            return ValueTask.CompletedTask;
        });
    }

    /// <summary>Sets a synchronous selection callback.</summary>
    public static IMenuOptionBuilder OnSelected(this IMenuOptionBuilder builder, Action<MenuOptionContext> handler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(handler);

        return builder.OnSelected(context =>
        {
            handler(context);
            return ValueTask.CompletedTask;
        });
    }
}
