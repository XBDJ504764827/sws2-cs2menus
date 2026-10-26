using SwiftlyS2.Shared.Players;
using Sws2Cs2Menus.Configuration;
using Sws2Cs2Menus.Contracts;

namespace Sws2Cs2Menus.Services;

/// <summary>
/// Internal backend contract. The public plugin exposes <see cref="IMenuService"/>
/// through a proxy so the renderer can be selected at load time.
/// </summary>
internal interface IMenuBackend : IMenuService, IDisposable
{
    void ApplyConfig(MenuConfig config);

    void Shutdown(MenuCloseReason reason);

    void ClearSessions(MenuCloseReason reason);

    void CleanupPlayer(int playerId, MenuCloseReason reason = MenuCloseReason.Disconnected);

    void OnPlayerJoined(IPlayer player);
}
