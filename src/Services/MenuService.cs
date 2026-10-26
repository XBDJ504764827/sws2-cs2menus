using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;
using Sws2Cs2Menus.Configuration;
using Sws2Cs2Menus.Contracts;
using Sws2Cs2Menus.Menus;
using MenuBuilderContract = Sws2Cs2Menus.Contracts.IMenuBuilder;
using MenuServiceContract = Sws2Cs2Menus.Contracts.IMenuService;

namespace Sws2Cs2Menus.Services;

/// <summary>
/// Owns player menu sessions and builds them on top of SwiftlyS2's component
/// menu framework. Pagination, focus, parent navigation and input are provided
/// by the framework; this service adds the shared KZ menu contract, permission
/// checks and lifecycle cleanup.
/// </summary>
internal sealed class MenuService : MenuServiceContract, IDisposable
{
    private sealed class MenuFrame
    {
        public required IMenuDefinition Definition { get; init; }

        public required IMenu Menu { get; init; }

        public required IMenuSession Session { get; set; }

        public MenuCloseReason? CloseReason { get; set; }

        public bool SuppressClose { get; set; }

        public bool Closed { get; set; }
    }

    private sealed class PlayerSession
    {
        public required IPlayer Player { get; init; }

        public List<MenuFrame> Stack { get; } = [];

        public bool Replacing { get; set; }

        public int CloseAttempts { get; set; }
    }

    private readonly ISwiftlyCore _core;
    private readonly ILogger _logger;
    private readonly object _sync = new();
    private readonly Dictionary<int, PlayerSession> _sessions = [];
    private readonly HashSet<int> _closingAll = [];

    private MenuConfig _config;
    private volatile bool _available;
    private bool _disposed;

    public MenuService(ISwiftlyCore core, MenuConfig config)
    {
        _core = core;
        _logger = core.Logger;
        _config = config;
        _available = true;
    }

    public bool IsAvailable => _available;

    public event EventHandler<MenuSessionEventArgs>? MenuOpened;

    public event EventHandler<MenuSessionEventArgs>? MenuClosed;

    public void ApplyConfig(MenuConfig config)
    {
        _config = config;
    }

    public MenuBuilderContract CreateMenu(string title)
    {
        var menu = _config.Menu;
        return new MenuBuilder(
            title,
            menu.DefaultPageSize,
            menu.ShowBack,
            menu.ShowClose,
            menu.CloseOnSelect);
    }

    public MenuOpenResult Open(IPlayer player, IMenuDefinition menu)
    {
        return OpenInternal(player, menu, push: false);
    }

    public MenuOpenResult OpenSubMenu(IPlayer player, IMenuDefinition menu)
    {
        return OpenInternal(player, menu, push: true);
    }

    public bool Back(IPlayer player)
    {
        lock (_sync)
        {
            if (!_available || !TryGetSession(player, out var session) || session.Stack.Count == 0)
            {
                return false;
            }

            var top = session.Stack[^1];
            if (top.Menu.Parent == null)
            {
                return false;
            }

            top.CloseReason = MenuCloseReason.Back;
            top.Session.Close();
            return true;
        }
    }

    public bool Close(IPlayer player)
    {
        lock (_sync)
        {
            if (!TryGetSession(player, out var session) || session.Stack.Count == 0)
            {
                return false;
            }

            _closingAll.Add(player.PlayerID);
            session.CloseAttempts = 0;
            CloseTopLocked(session, MenuCloseReason.Closed);
            return true;
        }
    }

    public bool IsOpen(IPlayer player)
    {
        lock (_sync)
        {
            return TryGetSession(player, out var session)
                && session.Stack.Count > 0
                && session.Stack[^1].Session.IsOpen;
        }
    }

    public IMenuDefinition? GetCurrentMenu(IPlayer player)
    {
        lock (_sync)
        {
            return TryGetSession(player, out var session) && session.Stack.Count > 0
                ? session.Stack[^1].Definition
                : null;
        }
    }

    public IReadOnlyList<IMenuDefinition> GetNavigationStack(IPlayer player)
    {
        lock (_sync)
        {
            if (!TryGetSession(player, out var session) || session.Stack.Count == 0)
            {
                return [];
            }

            var stack = new List<IMenuDefinition>();
            var menu = session.Stack[^1].Menu;

            while (menu != null)
            {
                if (menu.Tag is IMenuDefinition definition)
                {
                    stack.Add(definition);
                }

                menu = menu.Parent;
            }

            stack.Reverse();
            return stack;
        }
    }

    public void CloseAll()
    {
        Shutdown(MenuCloseReason.Closed, keepAvailable: true);
    }

    public void Shutdown(MenuCloseReason reason)
    {
        Shutdown(reason, keepAvailable: false);
    }

    /// <summary>
    /// Closes every session with the supplied reason but keeps the service
    /// available. Used for map changes, where the plugin stays loaded.
    /// </summary>
    public void ClearSessions(MenuCloseReason reason)
    {
        Shutdown(reason, keepAvailable: true);
    }

    public void CleanupPlayer(int playerId, MenuCloseReason reason = MenuCloseReason.Disconnected)
    {
        lock (_sync)
        {
            if (!_sessions.TryGetValue(playerId, out var session))
            {
                return;
            }

            if (session.Stack.Count == 0)
            {
                _sessions.Remove(playerId);
                _closingAll.Remove(playerId);
                return;
            }

            foreach (var frame in session.Stack)
            {
                frame.CloseReason = reason;
            }

            if (IsUsablePlayer(session.Player))
            {
                try
                {
                    var menu = session.Stack[^1].Menu;
                    while (menu != null)
                    {
                        menu.Close(session.Player);
                        menu = menu.Parent;
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "[sws2-cs2menus] Failed to close menus for disconnected player {PlayerId}",
                        playerId);
                }
            }

            foreach (var frame in session.Stack.ToArray())
            {
                CloseFrameLocked(session, frame);
            }

            _sessions.Remove(playerId);
            _closingAll.Remove(playerId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Shutdown(MenuCloseReason.PluginUnload, keepAvailable: false);
    }

    private MenuOpenResult OpenInternal(IPlayer player, IMenuDefinition menu, bool push)
    {
        ArgumentNullException.ThrowIfNull(menu);

        if (!_available)
        {
            return MenuOpenResult.Unavailable;
        }

        if (!IsUsablePlayer(player))
        {
            return MenuOpenResult.InvalidPlayer;
        }

        if (!HasPermission(player, menu.Permission))
        {
            return MenuOpenResult.NoPermission;
        }

        if (GetVisibleOptions(player, menu).Count == 0)
        {
            return MenuOpenResult.NoOptions;
        }

        lock (_sync)
        {
            var hasSession = _sessions.TryGetValue(player.PlayerID, out var session);
            var parentMenu = hasSession && session!.Stack.Count > 0 ? session.Stack[^1].Menu : null;

            if (!push || !hasSession || session!.Stack.Count == 0)
            {
                if (!hasSession)
                {
                    session = new PlayerSession { Player = player };
                    _sessions[player.PlayerID] = session;
                }

                session!.Replacing = true;
                foreach (var frame in session.Stack)
                {
                    frame.CloseReason = MenuCloseReason.Replaced;
                }

                IMenu root;
                try
                {
                    root = BuildMenuTree(menu);
                }
                catch (Exception exception)
                {
                    session.Replacing = false;
                    _logger.LogError(exception, "[sws2-cs2menus] Failed to build menu '{MenuId}'", menu.Id);
                    return MenuOpenResult.Unavailable;
                }

                IMenuSession? result;
                try
                {
                    result = root.Open(player);
                }
                catch (Exception exception)
                {
                    session.Replacing = false;
                    _logger.LogError(exception, "[sws2-cs2menus] Failed to open menu '{MenuId}'", menu.Id);
                    return MenuOpenResult.Unavailable;
                }

                session.Replacing = false;
                if (session.Stack.Count == 0 && result != null)
                {
                    AddFrameLocked(session, root, result, menu);
                }

                return MenuOpenResult.Opened;
            }

            IMenu child;
            try
            {
                child = BuildMenuTree(menu);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "[sws2-cs2menus] Failed to build submenu '{MenuId}'", menu.Id);
                return MenuOpenResult.Unavailable;
            }

            child.Parent = menu.ShowBack ? parentMenu : null;

            if (session!.Stack.Count > 0)
            {
                session.Stack[^1].SuppressClose = true;
            }

            session.Replacing = true;
            IMenuSession? childSession;
            try
            {
                childSession = child.Open(player);
            }
            catch (Exception exception)
            {
                session.Replacing = false;
                if (session.Stack.Count > 0)
                {
                    session.Stack[^1].SuppressClose = false;
                }

                _logger.LogError(exception, "[sws2-cs2menus] Failed to open submenu '{MenuId}'", menu.Id);
                return MenuOpenResult.Unavailable;
            }

            session.Replacing = false;

            if (session.Stack.Count == 0 && childSession != null)
            {
                AddFrameLocked(session, child, childSession, menu);
            }

            return MenuOpenResult.Opened;
        }
    }

    private IMenu BuildMenuTree(IMenuDefinition definition, int depth = 0)
    {
        if (depth > 16)
        {
            throw new InvalidOperationException($"Menu nesting is too deep (possible cycle) at '{definition.Id}'.");
        }

        var builder = _core.Menu.CreateMenu(definition.Id)
            .WithItemsPerPage(Math.Clamp(definition.PageSize, 1, MenuServiceConstants.DefaultPageSize));

        var titleStyle = string.IsNullOrWhiteSpace(_config.Menu.Colors.Title)
            ? MenuTextStyle.Default
            : MenuTextStyle.Default.WithColor(_config.Menu.Colors.Title);
        builder.AddHeader(new TitleComponent(definition.Title, $"{definition.Id}.title") { Style = titleStyle });

        if (!string.IsNullOrWhiteSpace(definition.Comment))
        {
            var commentStyle = string.IsNullOrWhiteSpace(_config.Menu.Colors.Comment)
                ? MenuTextStyle.Default
                : MenuTextStyle.Default.WithColor(_config.Menu.Colors.Comment);
            builder.AddHeader(new TextComponent(definition.Comment, $"{definition.Id}.comment") { Style = commentStyle });
        }

        if (!definition.ShowClose)
        {
            builder.WithAction(new MenuActionDescriptor
            {
                Id = new MenuActionId(definition.Id, MenuActions.Close.Name),
                DefaultKey = MenuKey.None,
                Label = MenuActions.Close.Name,
                ShowInFooter = false
            });
        }

        foreach (var option in definition.Options)
        {
            if (option.SubMenu != null)
            {
                var submenu = BuildMenuTree(option.SubMenu, depth + 1);
                var button = new ButtonComponent(
                    option.Text,
                    context => HandleSubMenuAsync(context, option, submenu),
                    option.Id);
                ConfigureComponent(button, option, closeOnSelect: false, option.SubMenu.Permission);
                builder.Add(button);
                continue;
            }

            var optionButton = new ButtonComponent(
                option.Text,
                context => HandleOptionAsync(context, option),
                option.Id);
            ConfigureComponent(optionButton, option, definition.CloseOnSelect);
            builder.Add(optionButton);
        }

        var menu = builder.Build();
        menu.Tag = definition;
        menu.Opened += OnMenuOpened;
        menu.Closed += OnMenuClosed;
        return menu;
    }

    private void ConfigureComponent(
        ButtonComponent component,
        IMenuOptionDefinition option,
        bool closeOnSelect,
        string? extraPermission = null)
    {
        component.Visible = !option.Hidden;
        component.Enabled = option.Enabled;
        component.VisibleWhen = session =>
            !option.Hidden
            && HasPermission(session.Player, option.Permission)
            && HasPermission(session.Player, extraPermission);
        component.EnabledWhen = _ => option.Enabled;
        component.Comment = option.Comment ?? _config.Menu.DefaultComment;
        component.PlaySound = _config.Menu.PlaySound;
        component.CloseOnSelect = closeOnSelect;
        component.Tag = option;

        if (!string.IsNullOrWhiteSpace(_config.Menu.Colors.Disabled))
        {
            component.DisabledColor = _config.Menu.Colors.Disabled;
        }

        if (!string.IsNullOrWhiteSpace(_config.Menu.Colors.Focused))
        {
            component.FocusedColor = _config.Menu.Colors.Focused;
        }

        if (!string.IsNullOrWhiteSpace(option.Color))
        {
            component.Style = MenuTextStyle.Default.WithColor(option.Color);
        }
    }

    private async ValueTask HandleOptionAsync(MenuActionContext context, IMenuOptionDefinition option)
    {
        if (!HasPermission(context.Player, option.Permission))
        {
            return;
        }

        var playerSession = FindPlayerSession(context.Player);
        var frame = playerSession == null ? null : FindFrame(playerSession, context.Menu);
        if (frame == null)
        {
            return;
        }

        var optionContext = new MenuOptionContext
        {
            Player = context.Player,
            Menu = frame.Definition,
            Service = this,
            Option = option
        };

        try
        {
            if (option.Selected != null)
            {
                await option.Selected(optionContext);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "[sws2-cs2menus] Option '{OptionId}' in menu '{MenuId}' threw for {PlayerName} ({PlayerId})",
                option.Id,
                frame.Definition.Id,
                context.Player.Name,
                context.Player.PlayerID);
            return;
        }

        lock (_sync)
        {
            if (FindFrame(playerSession!, context.Menu) == null)
            {
                return;
            }

            if (frame.Definition.CloseOnSelect)
            {
                frame.CloseReason = MenuCloseReason.Selected;
            }
        }
    }

    private async ValueTask HandleSubMenuAsync(
        MenuActionContext context,
        IMenuOptionDefinition option,
        IMenu submenu)
    {
        if (!HasPermission(context.Player, option.Permission)
            || !HasPermission(context.Player, option.SubMenu?.Permission))
        {
            return;
        }

        var playerSession = FindPlayerSession(context.Player);
        var frame = playerSession == null ? null : FindFrame(playerSession, context.Menu);
        if (frame == null)
        {
            return;
        }

        var optionContext = new MenuOptionContext
        {
            Player = context.Player,
            Menu = frame.Definition,
            Service = this,
            Option = option
        };

        try
        {
            if (option.Selected != null)
            {
                await option.Selected(optionContext);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "[sws2-cs2menus] Submenu option '{OptionId}' in menu '{MenuId}' threw for {PlayerName} ({PlayerId})",
                option.Id,
                frame.Definition.Id,
                context.Player.Name,
                context.Player.PlayerID);
            return;
        }

        lock (_sync)
        {
            if (!_available || !_sessions.TryGetValue(context.Player.PlayerID, out var session))
            {
                return;
            }

            if (FindFrame(session, context.Menu) == null)
            {
                return;
            }

            submenu.Parent = option.SubMenu?.ShowBack == true ? context.Menu : null;

            foreach (var item in session.Stack)
            {
                item.SuppressClose = item.Menu == context.Menu;
            }

            session.Replacing = true;
            IMenuSession? childSession;
            try
            {
                childSession = submenu.Open(context.Player);
            }
            catch (Exception exception)
            {
                session.Replacing = false;
                foreach (var item in session.Stack)
                {
                    item.SuppressClose = false;
                }

                _logger.LogError(
                    exception,
                    "[sws2-cs2menus] Failed to open submenu '{MenuId}' for {PlayerName} ({PlayerId})",
                    option.SubMenu?.Id,
                    context.Player.Name,
                    context.Player.PlayerID);
                return;
            }

            session.Replacing = false;

            if (session.Stack.Count == 0 && childSession != null && option.SubMenu != null)
            {
                AddFrameLocked(session, submenu, childSession, option.SubMenu);
            }
        }
    }

    private void OnMenuOpened(IMenuSession session)
    {
        lock (_sync)
        {
            if (session.Player == null || !_sessions.TryGetValue(session.Player.PlayerID, out var playerSession))
            {
                return;
            }

            if (session.Menu.Tag is not IMenuDefinition definition)
            {
                return;
            }

            var existingIndex = playerSession.Stack.FindIndex(frame => frame.Menu == session.Menu);
            if (existingIndex >= 0)
            {
                playerSession.Stack.RemoveRange(existingIndex, playerSession.Stack.Count - existingIndex);
            }

            playerSession.Stack.Add(new MenuFrame
            {
                Definition = definition,
                Menu = session.Menu,
                Session = session
            });

            RaiseMenuOpened(session.Player, definition);
        }
    }

    private void OnMenuClosed(IMenuSession session)
    {
        lock (_sync)
        {
            if (session.Player == null || !_sessions.TryGetValue(session.Player.PlayerID, out var playerSession))
            {
                return;
            }

            var index = playerSession.Stack.FindIndex(frame => frame.Menu == session.Menu);
            if (index < 0)
            {
                return;
            }

            var frame = playerSession.Stack[index];
            frame.Closed = true;
            var suppress = frame.SuppressClose;

            playerSession.Stack.RemoveAt(index);

            if (suppress)
            {
                return;
            }

            var reason = frame.CloseReason
                ?? (frame.Menu.Parent != null ? MenuCloseReason.Back : MenuCloseReason.Closed);
            InvokeClosed(frame.Definition, session.Player, reason);
            RaiseMenuClosed(session.Player, frame.Definition, reason);

            if (playerSession.Stack.Count == 0
                && frame.Menu.Parent == null
                && !playerSession.Replacing
                && !_closingAll.Contains(session.Player.PlayerID))
            {
                _sessions.Remove(session.Player.PlayerID);
            }
        }
    }

    private void CloseTopLocked(PlayerSession session, MenuCloseReason reason)
    {
        if (session.Stack.Count == 0)
        {
            _closingAll.Remove(session.Player.PlayerID);
            _sessions.Remove(session.Player.PlayerID);
            return;
        }

        var top = session.Stack[^1];
        top.CloseReason = reason;
        top.Session.Close();

        if (!_closingAll.Contains(session.Player.PlayerID))
        {
            return;
        }

        _core.Scheduler.NextTick(() => ContinueClose(session.Player.PlayerID));
    }

    private void ContinueClose(int playerId)
    {
        lock (_sync)
        {
            if (!_closingAll.Contains(playerId))
            {
                return;
            }

            if (!_sessions.TryGetValue(playerId, out var session))
            {
                _closingAll.Remove(playerId);
                return;
            }

            session.CloseAttempts++;
            if (session.CloseAttempts > 16)
            {
                _core.Menu.CloseFor(session.Player);
                _sessions.Remove(playerId);
                _closingAll.Remove(playerId);
                return;
            }

            if (session.Stack.Count == 0)
            {
                _sessions.Remove(playerId);
                _closingAll.Remove(playerId);
                return;
            }

            CloseTopLocked(session, MenuCloseReason.Closed);
        }
    }

    private void Shutdown(MenuCloseReason reason, bool keepAvailable)
    {
        lock (_sync)
        {
            if (!keepAvailable)
            {
                _available = false;
            }

            var sessions = _sessions.Values.ToArray();
            foreach (var session in sessions)
            {
                foreach (var frame in session.Stack)
                {
                    frame.CloseReason = reason;
                }
            }

            _core.Menu.CloseAll();

            foreach (var session in sessions)
            {
                foreach (var frame in session.Stack.ToArray())
                {
                    CloseFrameLocked(session, frame);
                }

                _sessions.Remove(session.Player.PlayerID);
            }

            _closingAll.Clear();

            if (keepAvailable)
            {
                _available = true;
            }
        }
    }

    private void CloseFrameLocked(PlayerSession session, MenuFrame frame)
    {
        if (frame.Closed)
        {
            return;
        }

        frame.Closed = true;
        var reason = frame.CloseReason
            ?? (frame.Menu.Parent != null ? MenuCloseReason.Back : MenuCloseReason.Closed);

        if (!frame.SuppressClose)
        {
            InvokeClosed(frame.Definition, session.Player, reason);
            RaiseMenuClosed(session.Player, frame.Definition, reason);
        }
    }

    private void AddFrameLocked(PlayerSession session, IMenu menu, IMenuSession menuSession, IMenuDefinition definition)
    {
        session.Stack.RemoveAll(frame => frame.Menu == menu);
        session.Stack.Add(new MenuFrame
        {
            Definition = definition,
            Menu = menu,
            Session = menuSession
        });
    }

    private PlayerSession? FindPlayerSession(IPlayer player)
    {
        lock (_sync)
        {
            return _sessions.TryGetValue(player.PlayerID, out var session) ? session : null;
        }
    }

    private static MenuFrame? FindFrame(PlayerSession session, IMenu menu)
    {
        return session.Stack.FirstOrDefault(frame => frame.Menu == menu);
    }

    private bool TryGetSession(IPlayer player, out PlayerSession session)
    {
        if (player == null)
        {
            session = null!;
            return false;
        }

        return _sessions.TryGetValue(player.PlayerID, out session!);
    }

    private static bool IsUsablePlayer(IPlayer player)
    {
        return player != null
            && player.IsValid
            && !player.IsFakeClient
            && (player.Controller?.IsValid ?? false);
    }

    private bool HasPermission(IPlayer player, string? permission)
    {
        return string.IsNullOrWhiteSpace(permission)
            || _core.Permission.PlayerHasPermission(player.SteamID, permission);
    }

    private List<IMenuOptionDefinition> GetVisibleOptions(IPlayer player, IMenuDefinition menu)
    {
        return menu.Options
            .Where(option => !option.Hidden
                && HasPermission(player, option.Permission)
                && (option.SubMenu == null || HasPermission(player, option.SubMenu.Permission)))
            .ToList();
    }

    private void InvokeClosed(IMenuDefinition menu, IPlayer player, MenuCloseReason reason)
    {
        if (menu.Closed == null)
        {
            return;
        }

        var context = new MenuContext
        {
            Player = player,
            Menu = menu,
            Service = this,
            Reason = reason
        };

        _ = InvokeClosedAsync(menu, context, reason);
    }

    private async Task InvokeClosedAsync(IMenuDefinition menu, MenuContext context, MenuCloseReason reason)
    {
        try
        {
            await menu.Closed!(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "[sws2-cs2menus] Closed callback for menu '{MenuId}' threw for {PlayerName} ({PlayerId}, {Reason})",
                menu.Id,
                context.Player.Name,
                context.Player.PlayerID,
                reason);
        }
    }

    private void RaiseMenuOpened(IPlayer player, IMenuDefinition menu)
    {
        try
        {
            MenuOpened?.Invoke(this, new MenuSessionEventArgs { Player = player, Menu = menu });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "[sws2-cs2menus] MenuOpened handler threw");
        }
    }

    private void RaiseMenuClosed(IPlayer player, IMenuDefinition menu, MenuCloseReason reason)
    {
        try
        {
            MenuClosed?.Invoke(this, new MenuSessionEventArgs { Player = player, Menu = menu, Reason = reason });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "[sws2-cs2menus] MenuClosed handler threw");
        }
    }
}
