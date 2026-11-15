<div align="center">
  <h2><strong>SWS2 CS2 Menus</strong></h2>
  <h3>Shared KZ menu service for SwiftlyS2 plugins.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/github/downloads/XBDJ504764827/sws2-cs2menus/total" alt="Downloads">
  <img src="https://img.shields.io/github/v/release/XBDJ504764827/sws2-cs2menus" alt="Release">
  <img src="https://img.shields.io/github/license/XBDJ504764827/sws2-cs2menus" alt="License">
</p>

`sws2-cs2menus` is the base menu plugin for the SWS2 KZ plugin family. Admin,
Whitelist and RockTheVote consume the same menu service instead of writing
their own pagination, navigation, permission and cleanup code.

The primary renderer is SwiftlyS2 Custom HUD (`custom_hud_layout`) with a
companion Panorama addon. It renders a dark text-only window with persistent
categories on the left and the active page on the right. The optional center-HTML
fallback uses `Core.Menu`.

## Features

- Shared `IMenuService` for other SwiftlyS2 plugins via the shared interface manager
- Menu definitions with a fluent builder and nested submenus
- Custom HUD menu backend using `custom_hud_layout` and a Panorama workshop addon
- Automatic fallback to the SwiftlyS2 center-HTML menu when the addon is absent
- Automatic pagination (`PageSize`, clamped to 1-5 entries per page)
- Back navigation to the parent menu and explicit close
- Menu-level and option-level permission checks
- Per-player visibility and enabled state
- Player disconnect, map unload and plugin unload cleanup
- Menu opened/closed events and per-menu closed callbacks
- Hot-reloadable `config.jsonc`
- A built-in `!menudemo` menu to verify pagination, submenus, permissions and cleanup
- Translations: English, 简体中文, 繁體中文

## New administrator HUD

`!admin` in `sws2-cs2admin` opens a two-pane window: **Player actions** and
**Map management** remain in the left category column while lists, player actions
and confirmations appear on the right. The first permitted category opens
automatically. Switching categories discards that category's pending navigation
and rebuilds its live players/maps; Back inside a category preserves its page.
The layout takes its category/right-pane interaction from cs2kz's `!o` menu and
uses original text-only styling without image or icon assets.
Kicks and bans use a separate confirmation screen; map changes use mapmanager's
existing countdown, download and change flow. The HUD only captures input while
open, and each administrator receives an independent panel.

Open [`preview/index.html`](preview/index.html) in a browser for an interactive
preview with sample players and maps. It is generated from the real Panorama XML
and CSS using `python3 tools/build_hud_preview.py`; the browser adapter approximates
Panorama and does not connect to a server. Workshop Tools and a CS2 client are
required to validate native rendering, Chinese fonts, mouse capture and game actions.

Update **both** the menus/admin plugin packages and the Workshop addon. An old
addon cannot display the new category panel IDs. Follow [`workshop/README.md`](workshop/README.md).
The .NET archive also includes the addon source under `workshop-source/` for compilation;
copying source files alone does not install a compiled client addon.

## Requirements

- [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) **1.5.0 or newer**
  (the component menu framework was introduced in 1.5.0)
- .NET 10 SDK (only needed to build from source)
- Optional: **sws2cs2menus Workshop addon** for the `custom_hud_layout` renderer. See [`workshop/README.md`](workshop/README.md).

## Installation

1. Download the latest release zip.
2. Extract the `sws2-cs2menus` folder into `csgo/addons/swiftlys2/plugins/`:

```
csgo/addons/swiftlys2/plugins/
└── sws2-cs2menus/
    ├── sws2-cs2menus.dll
    ├── Sws2Cs2Menus.Contracts.dll
    └── resources/
        ├── exports/Sws2Cs2Menus.Contracts.dll
        ├── templates/config.template.jsonc
        └── translations/
```

3. Start or reload the server, or run `sw plugins load sws2-cs2menus` in the server console.

The contracts dll is installed under `resources/exports/` on purpose. SwiftlyS2
loads it once as a shared assembly so every consumer plugin uses the same types.

## Commands

| Command | Description |
| :--- | :--- |
| `!menudemo` / `!menutest` | Open the demo menu |

The demo is intended for testing the service; it can be disabled in the config.

## Configuration

The first load creates
`csgo/addons/swiftlys2/configs/plugins/sws2-cs2menus/config.jsonc` from the bundled
template. The file is hot-reloaded.

```jsonc
{
  "sws2-cs2menus": {
    "Debug": false,
    "Menu": {
      "DefaultPageSize": 5,
      "PlaySound": true,
      "ShowBack": true,
      "ShowClose": true,
      "CloseOnSelect": true,
      "DefaultComment": "",
      "Backend": "auto",
      "HudLayoutPath": "panorama/layout/custom_game/sws2cs2menus/menu.vxml_c",
      "HudLayoutWorkshopId": "",
      "Colors": {
        "Title": "#70E0C4",
        "Comment": "#91A8B5",
        "Focused": "#70E0C4",
        "Disabled": "#617786"
      }
    },
    "Demo": {
      "EnableCommand": true,
      "CommandPermission": ""
    }
  }
}
```

`DefaultPageSize` is clamped to `1`-`5`.

`Backend` selects the renderer:

- `auto`: use `custom_hud_layout` when `HudLayoutPath` is mounted, otherwise center HTML
- `hudlayout`: force the custom HUD renderer
- `classic`: force the SwiftlyS2 center-HTML renderer

The custom HUD backend requires the Panorama addon from [`workshop/`](workshop/)
to be mounted on clients through MultiAddonManager. See
[`workshop/README.md`](workshop/README.md) for the complete Workshop Tools and
MultiAddonManager procedure.

`HudLayoutWorkshopId` is informational for now; add the published ID to
MultiAddonManager's `mm_extra_addons`.

## Using the menu service

### 1. Reference the contracts assembly

Reference `Sws2Cs2Menus.Contracts.dll` at compile time, but do **not** copy it
into your plugin folder. The copy installed by `sws2-cs2menus` is shared at runtime.

```xml
<Reference Include="Sws2Cs2Menus.Contracts">
  <HintPath>path/to/Sws2Cs2Menus.Contracts.dll</HintPath>
  <Private>false</Private>
</Reference>
```

If your plugin project lives next to this repository, a project reference with
`Private="false"` works too:

```xml
<ProjectReference Include="../sws2-cs2menus/contracts/Sws2Cs2Menus.Contracts.csproj"
                  Private="false" />
```

### 2. Resolve the shared interface

```csharp
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using Sws2Cs2Menus.Contracts;

public partial class MyPlugin : BasePlugin
{
    private IMenuService? _menus;

    public MyPlugin(ISwiftlyCore core) : base(core) { }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        if (interfaceManager.TryGetSharedInterface<IMenuService>(
                MenuServiceConstants.InterfaceKey,
                out var menus))
        {
            _menus = menus;
        }
    }

    public override void Load(bool hotReload) { }

    public override void Unload() { }
}
```

`UseSharedInterface` is called after all plugins load, so load order does not
matter. If `sws2-cs2menus` is missing or failed to load, `_menus` stays null.

> `Sws2Cs2Menus.Contracts.IMenuService` is intentionally separate from
> `SwiftlyS2.Shared.Menu.IMenuService`. If you import both namespaces, alias one
> of them (for example `using KzMenus = Sws2Cs2Menus.Contracts;`).

### 3. Build and open a menu

```csharp
private void ShowMainMenu(IPlayer player)
{
    if (_menus is not { IsAvailable: true })
    {
        player.SendChat("[green][Menus][default] Menu service unavailable.");
        return;
    }

    var submenu = _menus.CreateMenu("My Submenu")
        .PageSize(5)
        .ShowBack()
        .Build();

    var menu = _menus.CreateMenu("My Menu")
        .Id("myplugin.main")
        .PageSize(5)
        .Permission("myplugin.menu")
        .ShowBack()
        .ShowClose()
        .CloseOnSelect(false)
        .AddOption("hello", "Say hello", context =>
        {
            context.Player.SendChat("Hello!");
        })
        .AddSubMenu("submenu", "Open submenu", submenu)
        .AddOption(option => option
            .Id("admin")
            .Text("Admin only")
            .Permission("myplugin.admin"))
        .AddOption(option => option
            .Id("disabled")
            .Text("Disabled option")
            .Disabled())
        .Build();

    var result = _menus.Open(player, menu);
    if (result != MenuOpenResult.Opened)
    {
        player.SendChat($"Could not open menu: {result}");
    }
}
```

A menu definition is immutable and can be reused for multiple players. Every
`Open` creates an independent per-player navigation tree.

## API overview

| Type | Purpose |
| :--- | :--- |
| `IMenuService` | Resolve, build, open, close and navigate menus |
| `IMenuBuilder` | Fluent menu definition builder |
| `IMenuOptionBuilder` | Fluent option builder |
| `IMenuDefinition` | Immutable menu |
| `IMenuOptionDefinition` | Immutable option (text, comment, colour, permission, submenu) |
| `MenuContext` | Player, menu, service and close reason passed to menu callbacks |
| `MenuOptionContext` | The same, plus the selected option |
| `MenuOpenResult` | `Opened`, `NoPermission`, `InvalidPlayer`, `NoOptions`, `Unavailable` |
| `MenuCloseReason` | `Closed`, `Back`, `Selected`, `Replaced`, `Disconnected`, `MapUnload`, `PluginUnload`, `PermissionDenied` |

Useful builder methods:

- `Layout(MenuLayout.Sidebar)` on a root menu makes its first five visible options
  persistent categories. Their callbacks can call `OpenSubMenu` to build live content.
  The first enabled category opens on the next game tick.
- `Layout(MenuLayout.Confirmation)` emphasizes confirmation text and actions.
- The default `MenuLayout.List` preserves ordinary consumers;
  standalone lists use the full content width. The previous `Dashboard` enum value
  remains supported as a compact list for existing callers.
- `Tag(new MenuOptionAppearance("", Destructive: true))` marks destructive text.
  The glyph/image contract fields remain for compatibility; this HUD displays no icons.

- `PageSize(n)` sets the page size (1-5).
- `Permission("...")` gates the menu or an option.
- `Color("#RRGGBB")` sets an option's text colour.
- `Disabled()` and `Hidden()` control option state.
- `AddSubMenu(id, text, menu)` and `SubMenu(menu)` create nested navigation.
- `OnClosed(...)` runs when a menu closes.

Important methods:

- `CreateMenu(title)` starts a definition.
- `Open(player, menu)` opens a new root menu and replaces the player's current menu.
- `OpenSubMenu(player, menu)` opens a child menu on top of the current one.
- `Back(player)` returns to the parent when one exists.
- `Close(player)` closes the whole navigation chain.
- `GetNavigationStack(player)` returns root-to-current definitions.
- `CloseAll()` closes every menu owned by this service.

`IMenuDefinition.Closed` and `IMenuService.MenuClosed` are fired when a menu
closes. The service also cleans up all sessions when a player disconnects, the
map unloads, or the plugin is unloaded.


`Layout(MenuLayout.MapPicker)` displays up to four map rows per page. Supply
`.Tag(new MapMenuOptionAppearance(map.CkzNubTier, map.CkzProTier, map.VnlNubTier, map.VnlProTier, map.Id))` for independent
CKZ NUB/PRO and VNL NUB/PRO badges and a packaged thumbnail keyed by the stable map ID. Missing ratings
show an unknown label for that category, and missing pictures show a placeholder.
Only this layout displays map screenshots; the administrator sidebar stays text-only.
Generate screenshots with `tools/build_map_thumbnails.py` before compiling the addon;
see [map picture instructions](workshop/README.md#map-reservation-pictures).

## Building

```bash
dotnet restore
dotnet build -c Release
dotnet publish -c Release
```

The release package is generated at `build/sws2-cs2menus.zip` and contains both
the plugin and `resources/exports/Sws2Cs2Menus.Contracts.dll`.

The Workshop addon is not part of the .NET package. Its source lives in
`workshop/` and must be published separately with CS2 Workshop Tools.

## Versioning and releases

This repository uses a `develop` -> `main` flow:

- Pull requests run CI: `dotnet format`, a Release build with warnings as errors,
  package validation and a test-package artifact.
- Each PR run gets a traceable preview version such as `1.0.1-pr.12.37`.
- For pull requests from this repository, CI also publishes or updates a
  pre-release named `vX.Y.Z-pr.<PR>.<run>`. It is deleted when the PR closes.
- When a PR is merged into `main`, the release workflow computes the next patch
  version from the latest stable `vX.Y.Z` tag, injects it into `src/VersionInfo.cs`,
  builds the package, creates the tag and publishes a GitHub Release.
- A manual release can be started with `workflow_dispatch` and a `patch`, `minor`
  or `major` bump.

No manual version editing is required. The value committed in `src/VersionInfo.cs`
is only a fallback for local builds.

## Credits

- [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) - plugin framework
  and component menu/custom HUD system
- [FemboyKZ/mm-cs2menus](https://github.com/FemboyKZ/mm-cs2menus) - shared menu
  API and KZ menu behavior reference
- [SwiftlyS2-Plugins](https://github.com/SwiftlyS2-Plugins) - project conventions

## License

[MIT](LICENSE)
