using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.EntitySystem;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Sws2Cs2Menus.Configuration;
using Sws2Cs2Menus.Contracts;
using Sws2Cs2Menus.Menus;

namespace Sws2Cs2Menus.Services;

/// <summary>
/// Renders menus through a player-owned custom_hud_layout entity.
/// The companion Panorama addon supplies the panel ids and CSS.
/// </summary>
internal sealed class HudLayoutMenuService : IMenuBackend
{
    private const int ItemSlots = 5;

    private sealed class MenuFrame
    {
        public required IMenuDefinition Menu { get; init; }

        public int Page { get; set; }
    }

    private sealed class PlayerSession
    {
        public required IPlayer Player { get; init; }

        public List<MenuFrame> Stack { get; } = [];

        public CCSCustomHudLayout? Entity { get; set; }

        public Dictionary<string, string> Variables { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Classes { get; } = new(StringComparer.Ordinal);

        public bool InputCapture { get; set; }

        public bool Busy { get; set; }
    }

    private readonly ISwiftlyCore _core;
    private readonly ILogger _logger;
    private readonly object _sync = new();
    private readonly Dictionary<int, PlayerSession> _sessions = [];

    private MenuConfig _config;
    private volatile bool _available;
    private bool _disposed;

    public HudLayoutMenuService(ISwiftlyCore core, MenuConfig config)
    {
        _core = core;
        _logger = core.Logger;
        _config = config;
        _available = true;

        _core.Event.OnCustomHudClicked += OnCustomHudClicked;
        _core.Event.OnClientPutInServer += OnClientPutInServer;
    }

    public bool IsAvailable => _available;

    public event EventHandler<MenuSessionEventArgs>? MenuOpened;

    public event EventHandler<MenuSessionEventArgs>? MenuClosed;

    public static bool IsLayoutAvailable(ISwiftlyCore core, MenuConfig config)
    {
        try
        {
            var path = config.Menu.HudLayoutPath;
            var compiledPath = path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? path[..^4] + ".vxml_c"
                : path;

            return core.GameFileSystem.FileExists(path, "GAME")
                || core.GameFileSystem.FileExists(compiledPath, "GAME")
                || core.GameFileSystem.FileExists(path, "MOD")
                || core.GameFileSystem.FileExists(compiledPath, "MOD");
        }
        catch
        {
            return false;
        }
    }

    public void ApplyConfig(MenuConfig config)
    {
        _config = config;
    }

    public IMenuBuilder CreateMenu(string title)
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
            if (!TryGetSession(player, out var session) || session.Stack.Count <= 1)
            {
                return false;
            }

            var child = session.Stack[^1];
            session.Stack.RemoveAt(session.Stack.Count - 1);
            InvokeClosed(child.Menu, session.Player, MenuCloseReason.Back);
            RaiseMenuClosed(session.Player, child.Menu, MenuCloseReason.Back);
            RenderLocked(session);
            RaiseMenuOpened(session.Player, session.Stack[^1].Menu);
            return true;
        }
    }

    public bool Close(IPlayer player)
    {
        lock (_sync)
        {
            if (!TryGetSession(player, out var session))
            {
                return false;
            }

            CloseSessionLocked(session, MenuCloseReason.Closed);
            return true;
        }
    }

    public bool IsOpen(IPlayer player)
    {
        lock (_sync)
        {
            return TryGetSession(player, out var session)
                && session.Stack.Count > 0
                && session.Entity is { IsValidEntity: true };
        }
    }

    public IMenuDefinition? GetCurrentMenu(IPlayer player)
    {
        lock (_sync)
        {
            return TryGetSession(player, out var session) && session.Stack.Count > 0
                ? session.Stack[^1].Menu
                : null;
        }
    }

    public IReadOnlyList<IMenuDefinition> GetNavigationStack(IPlayer player)
    {
        lock (_sync)
        {
            return TryGetSession(player, out var session)
                ? session.Stack.Select(frame => frame.Menu).ToArray()
                : [];
        }
    }

    public void CloseAll()
    {
        ClearSessions(MenuCloseReason.Closed);
    }

    public void Shutdown(MenuCloseReason reason)
    {
        lock (_sync)
        {
            _available = false;
            CloseAllSessionsLocked(reason);
        }
    }

    public void ClearSessions(MenuCloseReason reason)
    {
        lock (_sync)
        {
            CloseAllSessionsLocked(reason);
        }
    }

    public void CleanupPlayer(int playerId, MenuCloseReason reason = MenuCloseReason.Disconnected)
    {
        lock (_sync)
        {
            if (_sessions.TryGetValue(playerId, out var session))
            {
                CloseSessionLocked(session, reason);
            }
        }
    }

    public void OnPlayerJoined(IPlayer player)
    {
        lock (_sync)
        {
            foreach (var session in _sessions.Values)
            {
                if (session.Entity is { IsValidEntity: true } entity && session.Player.PlayerID != player.PlayerID)
                {
                    HideEntityFromPlayerLocked(entity, player.PlayerID);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _available = false;

        _core.Event.OnCustomHudClicked -= OnCustomHudClicked;
        _core.Event.OnClientPutInServer -= OnClientPutInServer;

        lock (_sync)
        {
            CloseAllSessionsLocked(MenuCloseReason.PluginUnload);
        }
    }

    private MenuOpenResult OpenInternal(IPlayer player, IMenuDefinition menu, bool push)
    {
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

            if (!push || !hasSession || session!.Stack.Count == 0)
            {
                if (hasSession)
                {
                    CloseSessionLocked(session!, MenuCloseReason.Replaced);
                }

                session = new PlayerSession { Player = player };
                _sessions[player.PlayerID] = session;
            }

            session!.Stack.Add(new MenuFrame { Menu = menu });

            if (!EnsureEntityLocked(session))
            {
                CloseSessionLocked(session, MenuCloseReason.Closed);
                return MenuOpenResult.Unavailable;
            }

            RenderLocked(session);
            RaiseMenuOpened(player, menu);
            return MenuOpenResult.Opened;
        }
    }

    private bool EnsureEntityLocked(PlayerSession session)
    {
        if (session.Entity is { IsValidEntity: true })
        {
            return true;
        }

        try
        {
            var entity = _core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
            using var keyValues = new CEntityKeyValues();
            keyValues.SetString("layout", _config.Menu.HudLayoutPath);
            keyValues.SetString("targetname", $"sws2_cs2menus_{session.Player.PlayerID}");
            entity.DispatchSpawn(keyValues);

            if (!entity.IsValidEntity)
            {
                _logger.LogWarning("[sws2-cs2menus] custom_hud_layout spawn failed for {PlayerId}", session.Player.PlayerID);
                return false;
            }

            session.Entity = entity;
            session.Variables.Clear();
            session.Classes.Clear();
            session.InputCapture = false;

            foreach (var other in _core.PlayerManager.GetAllValidPlayers())
            {
                if (other.PlayerID != session.Player.PlayerID)
                {
                    HideEntityFromPlayerLocked(entity, other.PlayerID);
                }
            }

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "[sws2-cs2menus] Failed to create custom_hud_layout for {PlayerId}",
                session.Player.PlayerID);
            return false;
        }
    }

    private void RenderLocked(PlayerSession session)
    {
        if (!_available || session.Stack.Count == 0)
        {
            return;
        }

        var player = session.Player;
        if (!IsUsablePlayer(player))
        {
            CloseSessionLocked(session, MenuCloseReason.InvalidPlayer);
            return;
        }

        if (!EnsureEntityLocked(session) || session.Entity is not { IsValidEntity: true } entity)
        {
            CloseSessionLocked(session, MenuCloseReason.Closed);
            return;
        }

        var frame = session.Stack[^1];
        var menu = frame.Menu;
        var visibleOptions = GetVisibleOptions(player, menu);
        if (visibleOptions.Count == 0)
        {
            CloseSessionLocked(session, MenuCloseReason.PermissionDenied);
            return;
        }

        var pageSize = Math.Clamp(menu.PageSize, 1, ItemSlots);
        var pageCount = Math.Max(1, (visibleOptions.Count + pageSize - 1) / pageSize);
        frame.Page = Math.Clamp(frame.Page, 0, pageCount - 1);

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var pageItems = visibleOptions
            .Skip(frame.Page * pageSize)
            .Take(pageSize)
            .ToArray();

        SetTextLocked(session, "sw_title", menu.Title);
        SetTextLocked(session, "sw_page", $"{frame.Page + 1}/{pageCount}");
        SetTextLocked(session, "sw_comment_text", menu.Comment ?? string.Empty);
        SetTextLocked(session, "sw_footer", localizer["cs2menus.hud.footer"]);
        SetTextLocked(session, "sw_back_text", localizer["cs2menus.hud.back"]);
        SetClassLocked(session, "sw_comment", "hidden", string.IsNullOrWhiteSpace(menu.Comment));

        for (var i = 0; i < ItemSlots; i++)
        {
            var panel = $"sw_item{i}";
            if (i >= pageItems.Length)
            {
                SetClassLocked(session, panel, "hidden", true);
                continue;
            }

            var option = pageItems[i];
            var comment = option.Comment ?? _config.Menu.DefaultComment;
            SetTextLocked(session, $"{panel}_text", option.Text);
            SetTextLocked(session, $"{panel}_comment", comment);
            SetClassLocked(session, panel, "hidden", false);
            SetClassLocked(session, panel, "disabled", !option.Enabled);
            SetClassLocked(session, $"{panel}_comment", "hidden", string.IsNullOrWhiteSpace(comment));
        }

        SetClassLocked(session, "sw_prev", "hidden", frame.Page <= 0);
        SetClassLocked(session, "sw_next", "hidden", frame.Page >= pageCount - 1);
        SetClassLocked(session, "sw_back", "hidden", session.Stack.Count <= 1 || !menu.ShowBack);
        SetClassLocked(session, "sw_close", "hidden", !menu.ShowClose);
        SetClassLocked(session, "sw_root", "hidden", false);

        SetInputCaptureLocked(session, true);
    }

    private void OnCustomHudClicked(IOnCustomHudClickedEvent @event)
    {
        PlayerSession? session;
        MenuFrame? frame = null;
        IMenuOptionDefinition? option = null;
        var buttonId = @event.ButtonId;
        var close = false;
        var back = false;
        var pageDelta = 0;

        lock (_sync)
        {
            if (!TryGetSession(@event.PlayerId, out session) || session.Entity is not { IsValidEntity: true } entity)
            {
                return;
            }

            if (@event.CustomHudLayout is not { IsValidEntity: true } clicked
                || entity.Index != clicked.Index
                || session.Busy
                || session.Stack.Count == 0)
            {
                return;
            }

            frame = session.Stack[^1];

            if (buttonId == "sw_close")
            {
                close = true;
            }
            else if (buttonId == "sw_back")
            {
                back = true;
            }
            else if (buttonId == "sw_prev")
            {
                pageDelta = -1;
            }
            else if (buttonId == "sw_next")
            {
                pageDelta = 1;
            }
            else if (buttonId.StartsWith("sw_item", StringComparison.Ordinal)
                && int.TryParse(buttonId.AsSpan("sw_item".Length), out var index))
            {
                var pageSize = Math.Clamp(frame.Menu.PageSize, 1, ItemSlots);
                var visibleOptions = GetVisibleOptions(session.Player, frame.Menu);
                var itemIndex = frame.Page * pageSize + index;
                if (itemIndex >= 0 && itemIndex < visibleOptions.Count)
                {
                    var candidate = visibleOptions[itemIndex];
                    if (candidate.Enabled)
                    {
                        option = candidate;
                        session.Busy = true;
                    }
                }
            }

            if (close)
            {
                CloseSessionLocked(session, MenuCloseReason.Closed);
                return;
            }

            if (back)
            {
                if (session.Stack.Count <= 1)
                {
                    return;
                }

                var child = session.Stack[^1];
                session.Stack.RemoveAt(session.Stack.Count - 1);
                InvokeClosed(child.Menu, session.Player, MenuCloseReason.Back);
                RaiseMenuClosed(session.Player, child.Menu, MenuCloseReason.Back);
                RenderLocked(session);
                RaiseMenuOpened(session.Player, session.Stack[^1].Menu);
                return;
            }

            if (pageDelta != 0)
            {
                var visibleOptions = GetVisibleOptions(session.Player, frame.Menu);
                var pageSize = Math.Clamp(frame.Menu.PageSize, 1, ItemSlots);
                var pageCount = Math.Max(1, (visibleOptions.Count + pageSize - 1) / pageSize);
                var nextPage = Math.Clamp(frame.Page + pageDelta, 0, pageCount - 1);
                if (nextPage != frame.Page)
                {
                    frame.Page = nextPage;
                    RenderLocked(session);
                }

                return;
            }
        }

        if (session == null || frame == null || option == null)
        {
            return;
        }

        _ = HandleOptionAsync(session, frame, option);
    }

    private async Task HandleOptionAsync(PlayerSession session, MenuFrame frame, IMenuOptionDefinition option)
    {
        var player = session.Player;

        if (!HasPermission(player, option.Permission))
        {
            session.Busy = false;
            return;
        }

        var context = new MenuOptionContext
        {
            Player = player,
            Menu = frame.Menu,
            Service = this,
            Option = option
        };

        try
        {
            if (option.Selected != null)
            {
                await option.Selected(context);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "[sws2-cs2menus] Option '{OptionId}' in menu '{MenuId}' threw for {PlayerName} ({PlayerId})",
                option.Id,
                frame.Menu.Id,
                player.Name,
                player.PlayerID);
        }

        lock (_sync)
        {
            session.Busy = false;

            if (!IsCurrentSession(session) || session.Stack.Count == 0 || session.Stack[^1] != frame)
            {
                return;
            }

            if (option.SubMenu != null)
            {
                session.Stack.Add(new MenuFrame { Menu = option.SubMenu });
                if (EnsureEntityLocked(session))
                {
                    RenderLocked(session);
                    RaiseMenuOpened(player, option.SubMenu);
                }
                else
                {
                    session.Stack.RemoveAt(session.Stack.Count - 1);
                }

                return;
            }

            if (frame.Menu.CloseOnSelect)
            {
                CloseSessionLocked(session, MenuCloseReason.Selected);
            }
        }
    }

    private void OnClientPutInServer(IOnClientPutInServerEvent @event)
    {
        lock (_sync)
        {
            foreach (var session in _sessions.Values)
            {
                if (session.Entity is { IsValidEntity: true } entity && session.Player.PlayerID != @event.PlayerId)
                {
                    HideEntityFromPlayerLocked(entity, @event.PlayerId);
                }
            }
        }
    }

    private void HideEntityFromPlayerLocked(CCSCustomHudLayout entity, int playerId)
    {
        try
        {
            entity.SetTransmitState(false, playerId);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "[sws2-cs2menus] Failed to hide HUD layout from player {PlayerId}", playerId);
        }
    }

    private void CloseAllSessionsLocked(MenuCloseReason reason)
    {
        foreach (var session in _sessions.Values.ToArray())
        {
            CloseSessionLocked(session, reason);
        }

        _sessions.Clear();
    }

    private void CloseSessionLocked(PlayerSession session, MenuCloseReason reason)
    {
        _sessions.Remove(session.Player.PlayerID);

        SetInputCaptureLocked(session, false);

        if (session.Entity is { IsValidEntity: true } entity)
        {
            try
            {
                entity.Despawn();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "[sws2-cs2menus] Failed to despawn HUD layout");
            }
        }

        session.Entity = null;

        for (var i = session.Stack.Count - 1; i >= 0; i--)
        {
            var frame = session.Stack[i];
            InvokeClosed(frame.Menu, session.Player, reason);
            if (i == session.Stack.Count - 1)
            {
                RaiseMenuClosed(session.Player, frame.Menu, reason);
            }
        }

        session.Stack.Clear();
        session.Variables.Clear();
        session.Classes.Clear();
    }

    private void SetTextLocked(PlayerSession session, string panelId, string value)
    {
        if (session.Entity is not { IsValidEntity: true } entity)
        {
            return;
        }

        if (session.Variables.TryGetValue(panelId, out var current) && current == value)
        {
            return;
        }

        try
        {
            // A word joiner prevents the client from localizing values such as "#1".
            entity.SetDialogVariableStringForPlayer(
                session.Player.PlayerID,
                panelId,
                panelId,
                "\u2060" + value);
            session.Variables[panelId] = value;
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "[sws2-cs2menus] Failed to write HUD variable {PanelId}", panelId);
        }
    }

    private void SetClassLocked(PlayerSession session, string panelId, string className, bool enabled)
    {
        if (session.Entity is not { IsValidEntity: true } entity)
        {
            return;
        }

        var key = panelId + "|" + className;
        if (session.Classes.Contains(key) == enabled)
        {
            return;
        }

        try
        {
            entity.SetHasClassForPlayer(
                session.Player.PlayerID,
                panelId,
                className,
                enabled
                    ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
                    : EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass);

            if (enabled)
            {
                session.Classes.Add(key);
            }
            else
            {
                session.Classes.Remove(key);
            }
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "[sws2-cs2menus] Failed to set HUD class {PanelId}/{Class}", panelId, className);
        }
    }

    private void SetInputCaptureLocked(PlayerSession session, bool enabled)
    {
        if (session.Entity is not { IsValidEntity: true } entity || session.InputCapture == enabled)
        {
            return;
        }

        try
        {
            entity.SetInputCaptureEnabledForPlayer(session.Player.PlayerID, enabled);
            session.InputCapture = enabled;
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "[sws2-cs2menus] Failed to set HUD input capture");
        }
    }

    private bool TryGetSession(IPlayer player, out PlayerSession session)
    {
        return TryGetSession(player.PlayerID, out session);
    }

    private bool TryGetSession(int playerId, out PlayerSession session)
    {
        return _sessions.TryGetValue(playerId, out session!);
    }

    private bool IsCurrentSession(PlayerSession session)
    {
        return _sessions.TryGetValue(session.Player.PlayerID, out var current)
            && ReferenceEquals(current, session);
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
