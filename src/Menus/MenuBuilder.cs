using Sws2Cs2Menus.Contracts;

namespace Sws2Cs2Menus.Menus;

internal sealed class MenuBuilder : IMenuBuilder
{
    private readonly List<IMenuOptionDefinition> _options = [];
    private readonly int _defaultPageSize;
    private readonly bool _defaultShowBack;
    private readonly bool _defaultShowClose;
    private readonly bool _defaultCloseOnSelect;

    private string _id = $"menu-{Guid.NewGuid():N}"[..13];
    private readonly string _title;
    private string? _comment;
    private int _pageSize;
    private string? _permission;
    private bool _showBack;
    private bool _showClose;
    private bool _closeOnSelect;
    private Func<MenuContext, ValueTask>? _closed;

    public MenuBuilder(
        string title,
        int defaultPageSize,
        bool defaultShowBack,
        bool defaultShowClose,
        bool defaultCloseOnSelect)
    {
        _title = string.IsNullOrWhiteSpace(title) ? "Menu" : title;
        _defaultPageSize = Math.Clamp(defaultPageSize, 1, MenuServiceConstants.DefaultPageSize);
        _defaultShowBack = defaultShowBack;
        _defaultShowClose = defaultShowClose;
        _defaultCloseOnSelect = defaultCloseOnSelect;

        _pageSize = _defaultPageSize;
        _showBack = _defaultShowBack;
        _showClose = _defaultShowClose;
        _closeOnSelect = _defaultCloseOnSelect;
    }

    public IMenuBuilder Id(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            _id = id;
        }

        return this;
    }

    public IMenuBuilder Comment(string? comment)
    {
        _comment = comment;
        return this;
    }

    public IMenuBuilder PageSize(int pageSize)
    {
        _pageSize = Math.Clamp(pageSize, 1, MenuServiceConstants.DefaultPageSize);
        return this;
    }

    public IMenuBuilder Permission(string? permission)
    {
        _permission = string.IsNullOrWhiteSpace(permission) ? null : permission;
        return this;
    }

    public IMenuBuilder ShowBack(bool show = true)
    {
        _showBack = show;
        return this;
    }

    public IMenuBuilder ShowClose(bool show = true)
    {
        _showClose = show;
        return this;
    }

    public IMenuBuilder CloseOnSelect(bool close = true)
    {
        _closeOnSelect = close;
        return this;
    }

    public IMenuBuilder OnClosed(Func<MenuContext, ValueTask>? handler)
    {
        _closed = handler;
        return this;
    }

    public IMenuBuilder AddOption(string id, string text, Func<MenuOptionContext, ValueTask>? onSelected = null)
    {
        _options.Add(new MenuOptionDefinition
        {
            Id = string.IsNullOrWhiteSpace(id) ? $"option-{_options.Count + 1}" : id,
            Text = text,
            Selected = onSelected
        });

        return this;
    }

    public IMenuBuilder AddOption(Action<IMenuOptionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var optionBuilder = new MenuOptionBuilder();
        configure(optionBuilder);
        _options.Add(optionBuilder.Build());
        return this;
    }

    public IMenuBuilder AddOption(IMenuOptionDefinition option)
    {
        ArgumentNullException.ThrowIfNull(option);
        _options.Add(option);
        return this;
    }

    public IMenuBuilder AddSubMenu(string id, string text, IMenuDefinition subMenu)
    {
        ArgumentNullException.ThrowIfNull(subMenu);

        _options.Add(new MenuOptionDefinition
        {
            Id = string.IsNullOrWhiteSpace(id) ? $"option-{_options.Count + 1}" : id,
            Text = text,
            SubMenu = subMenu
        });

        return this;
    }

    public IMenuDefinition Build()
    {
        return new MenuDefinition
        {
            Id = _id,
            Title = _title,
            Comment = _comment,
            PageSize = Math.Clamp(_pageSize, 1, MenuServiceConstants.DefaultPageSize),
            Permission = _permission,
            ShowBack = _showBack,
            ShowClose = _showClose,
            CloseOnSelect = _closeOnSelect,
            Options = _options.ToArray(),
            Closed = _closed
        };
    }
}

internal sealed class MenuOptionBuilder : IMenuOptionBuilder
{
    private string _id = $"option-{Guid.NewGuid():N}"[..15];
    private string _text = string.Empty;
    private string? _comment;
    private string? _color;
    private string? _permission;
    private bool _hidden;
    private bool _enabled = true;
    private object? _tag;
    private IMenuDefinition? _subMenu;
    private Func<MenuOptionContext, ValueTask>? _selected;

    public IMenuOptionBuilder Id(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            _id = id;
        }

        return this;
    }

    public IMenuOptionBuilder Text(string text)
    {
        _text = text;
        return this;
    }

    public IMenuOptionBuilder Comment(string? comment)
    {
        _comment = comment;
        return this;
    }

    public IMenuOptionBuilder Color(string? hexColor)
    {
        _color = string.IsNullOrWhiteSpace(hexColor) ? null : hexColor;
        return this;
    }

    public IMenuOptionBuilder Permission(string? permission)
    {
        _permission = string.IsNullOrWhiteSpace(permission) ? null : permission;
        return this;
    }

    public IMenuOptionBuilder Disabled(bool disabled = true)
    {
        _enabled = !disabled;
        return this;
    }

    public IMenuOptionBuilder Hidden(bool hidden = true)
    {
        _hidden = hidden;
        return this;
    }

    public IMenuOptionBuilder Tag(object? tag)
    {
        _tag = tag;
        return this;
    }

    public IMenuOptionBuilder OnSelected(Func<MenuOptionContext, ValueTask>? handler)
    {
        _selected = handler;
        return this;
    }

    public IMenuOptionBuilder SubMenu(IMenuDefinition? subMenu)
    {
        _subMenu = subMenu;
        return this;
    }

    public IMenuOptionDefinition Build()
    {
        return new MenuOptionDefinition
        {
            Id = _id,
            Text = string.IsNullOrWhiteSpace(_text) ? _id : _text,
            Comment = _comment,
            Color = _color,
            Permission = _permission,
            Hidden = _hidden,
            Enabled = _enabled,
            Tag = _tag,
            SubMenu = _subMenu,
            Selected = _selected
        };
    }
}
