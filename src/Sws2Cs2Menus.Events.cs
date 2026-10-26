using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Events;
using Sws2Cs2Menus.Contracts;

namespace Sws2Cs2Menus;

/// <summary>
/// Lifecycle cleanup. Player sessions are always dropped by player id so a
/// half-disconnected player can never keep a menu alive.
/// </summary>
public partial class Sws2Cs2Menus
{
    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        if (_config.Debug)
        {
            Core.Logger.LogDebug(
                "[sws2-cs2menus] Cleaning up menu session for disconnected player {PlayerId}",
                @event.PlayerId);
        }

        _menus.CleanupPlayer(@event.PlayerId, MenuCloseReason.Disconnected);
    }

    private void OnMapUnload(IOnMapUnloadEvent @event)
    {
        _menus.ClearSessions(MenuCloseReason.MapUnload);

        if (_config.Debug)
        {
            Core.Logger.LogDebug("[sws2-cs2menus] All menu sessions cleared for map unload: {MapName}", @event.MapName);
        }
    }
}
