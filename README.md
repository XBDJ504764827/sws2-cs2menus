# SWS2 CS2 Menus

供 SWS2 插件共享的菜单服务，负责自定义 HUD、分页、返回、关闭、点击回调、权限检查和玩家会话清理。`!admin` 的管理面板、`!yd` 的预定面板，以及无人预定时 RTV 的地图选票均复用本插件。

## 与其他 SWS2 插件搭配

| 插件 | 职责 | 与本插件的关系 |
| --- | --- | --- |
| [sws2-cs2menus](https://github.com/XBDJ504764827/sws2-cs2menus) | 共享菜单、分页、导航、地图选择 HUD | 本插件 |
| [sws2-cs2mapmanager](https://github.com/XBDJ504764827/sws2-cs2mapmanager) | 统一地图目录、评分、工坊下载和可取消换图 | 独立基础服务；共同服务 admin / RTV |
| [sws2-cs2admin](https://github.com/XBDJ504764827/sws2-cs2admin) | 管理员权限、玩家管理、封禁、换图控制 | 依赖本插件显示管理面板 |
| [sws2-cs2rockthevote](https://github.com/XBDJ504764827/sws2-cs2rockthevote) | 玩家预定地图、RTV 发起投票与选图 | 依赖本插件显示预定和选图面板 |
| [sws2-flashlight](https://github.com/XBDJ504764827/sws2-flashlight) | 随视角移动的手电筒 | 可选，独立照明功能 |
| [sws2-nightvision](https://github.com/XBDJ504764827/sws2-nightvision) | 玩家独立夜视与亮度偏好 | 可选，独立夜视功能 |

常用组合：玩家换图安装 **menus + mapmanager + rockthevote**；需要管理面板时再加 **admin**；手电筒和夜视按需加入。工坊地图自动下载和 HUD 工坊资源自动挂载还需要服务器安装 **MultiAddonManager**。本套件不提供 KZ 计时、检查点或 CKZ/VNL 模式本身。

## 安装与快速验证

需要 SwiftlyS2 **1.5.0 或更新版本**。自定义 HUD 还需要已编译的 Panorama addon；生产环境自动挂载使用 MultiAddonManager。没有 addon 时可用中心 HTML 菜单。

1. 下载本仓库发布包或 CI 测试包，完整解压 `sws2-cs2menus/` 到 `csgo/addons/swiftlys2/plugins/`。
2. 保留 `Sws2Cs2Menus.Contracts.dll`、`resources/exports/`、翻译和 `resources/hud-addon.json`。合约用于共享接口，不要与其他插件混用不兼容版本。
3. 重启服务器，或在服务器控制台运行 `sw plugins load sws2-cs2menus`。
4. 进入游戏运行 `!menudemo` / `!menutest`，验证分页、子菜单、返回和关闭；此演示不依赖 admin 或 RTV，其示例地图不等于 mapmanager 的真实目录。
5. 要使用图片式 HUD，完成下面的 addon 配置后检查 `sw_hudstatus`，客户端重连，再重开菜单。

| 命令 | 执行位置 | 用途 |
| --- | --- | --- |
| `!menudemo` / `!menutest` | 游戏聊天 | 演示菜单 |
| `sw_hudstatus` / `hudstatus` | 服务器控制台 | 显示 HUD ID、MAM 注册、布局可用性和当前渲染器 |

`!admin` 由 admin 注册，`!yd` / `!rtv` 由 rockthevote 注册；只安装 menus 不会获得这些业务功能。

## 配置

首次加载生成 `csgo/addons/swiftlys2/configs/plugins/sws2-cs2menus/config.jsonc`，修改后热重载。

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
      "AutoMountHudAddon": true,
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

| 字段 | 说明 |
| --- | --- |
| `Menu.Backend` | `auto` 默认自动选择；`hudlayout` 优先 HUD；`classic` 固定中心 HTML |
| `Menu.HudLayoutWorkshopId` | 自己发布的 **HUD addon** 数字 ID；为空时读取包内 `resources/hud-addon.json` |
| `Menu.HudLayoutPath` | 编译布局路径，通常保持模板默认值 |
| `Menu.AutoMountHudAddon` | 默认 `true`，通过 MAM 自动注册；本地手动挂载测试可设 `false` |
| `Menu.DefaultPageSize` | 普通菜单每页 1–5 项，地图选择布局每页最多 4 张 |
| `Menu.PlaySound` / `ShowBack` / `ShowClose` / `CloseOnSelect` | 菜单默认交互行为，消费者可覆盖 |
| `Menu.Colors` / `DefaultComment` | 公共颜色与说明文字默认值 |
| `Demo.EnableCommand` | 是否允许打开演示，默认 `true` |
| `Demo.CommandPermission` | 演示所需 SwiftlyS2 权限节点，空字符串为所有玩家可用 |

`auto` 和 `hudlayout` 在布局资源缺失时先用中心 HTML，资源挂载后切到 HUD；`classic` 不自动注册 HUD addon。渲染器切换会关闭现有菜单，重新输入 `!admin`、`!yd`，或在活跃投票中输入 `!rtv` 打开。

## 配置可点击的自定义 HUD

1. 按 [Workshop 详细说明](workshop/README.md) 在 CS2 Workshop Tools 创建 addon，将本仓库 `workshop/panorama/` 放入内容目录，编译 XML、CSS 和截图。
2. 发布为客户端可下载的工坊资源，获得真实 Workshop ID。仓库自带 `resources/hud-addon.json` 的 ID 为空，需要你注册自己的资源。
3. 将该 ID 写入运行配置 `Menu.HudLayoutWorkshopId`，保留 `Backend: "auto"` 和 `AutoMountHudAddon: true`。也可在分发包 manifest 中提供默认 ID，但配置值优先；正式部署建议在运行配置保存 ID，避免升级包中的空 manifest 覆盖它。
4. 确认 MultiAddonManager 已加载。menus 会合并 ID 到实时 `mm_extra_addons`，保留原有其他 addon 及顺序；通过引擎正常设置触发 MAM 的下载/刷新，不重写 MAM 配置文件。
5. 在服务器控制台运行 `sw_hudstatus`，检查 `layoutReady=True` 与 `renderer=hudlayout`。客户端完成下载后重连，用 `!menudemo` 验证。

HUD ID 与 mapmanager 中的地图工坊 ID 不同；本插件不会从地图标题或布局路径推断 HUD ID。**.NET 发布包只有插件与资源源码相关配置，不会代替 Workshop Tools 编译/发布 addon。** 服务器和客户端都需要编译资源；只复制 XML 或 DLL 不能让客户端显示新布局。

插件每 5 秒及地图/配置变化时检查资源，失败注册写入最多每 30 秒重试；ID 已列出不表示下载和挂载成功。对已注册但布局缺失的资源，每个地图/配置周期最多触发一次刷新；等待下载时不会不断重刷。卸载本插件不会移除可能被其他插件共用的 addon。

## 与 admin、RTV 的界面配合

| 使用入口 | 界面与动作 | 业务由谁处理 |
| --- | --- | --- |
| `!admin` | 左侧玩家/地图/封禁/服务器管理分类，右侧列表与确认 | admin 校验权限并执行动作，地图操作交给 mapmanager |
| `!yd` | 四张地图一页，截图、四类评分；选择后关闭 | rockthevote 记录预定 |
| 无预定 `!rtv` 达到门槛 | 复用地图选择布局，每人提交一票 | rockthevote 统计多数/平票随机，mapmanager 换图 |

每位玩家使用独立面板和导航状态，只有打开期间捕获输入。新根菜单会替换该玩家当前菜单，多个插件不会同时堆叠两个根面板；投票期间关闭或被替换后，未选图玩家可用 `!rtv` 重开。

取消/暂停 RTV、统计票数、管理员资格均由业务插件负责；menus 本身不维护地图顺序、预定或封禁。

## 地图截图与四类评分

地图 HUD 使用稳定地图 ID 对应的本地截图，展示 CKZ NUB、CKZ PRO、VNL NUB、VNL PRO。NUB 为存点，PRO 为裸跳；未知评分及缺图分别显示占位。admin、`!yd` 和 RTV 共用这些资源。

评分来自 mapmanager，改配置后重开菜单即可。截图是 addon 内 CSS 背景，运行时 Steam 预览 URL 不会自动替换它。更换地图目录后，可在本仓库运行：

```sh
python3 tools/build_map_thumbnails.py --config /path/to/mapmanager/config.jsonc
python3 tools/build_map_thumbnails.py --config /path/to/mapmanager/config.jsonc --images images.json --offline
python3 tools/build_hud_preview.py
```

离线图片映射、缓存处理和编译步骤见 [截图说明](workshop/README.md#map-reservation-pictures)。随后重新编译 XML、两个 CSS 文件及图片，更新同一个 HUD addon，客户端重连。仅改评分/显示名称无需重做截图，稳定 ID 改变时需更新图片映射。

浏览器可打开 [管理面板预览](preview/index.html) 和 [预定面板预览](preview/reservation.html)。它们使用样例数据，不连接服务器，也不能验证原生字体、输入捕获或 Panorama 编译。

## 常见问题与升级

| 现象 | 检查方法 |
| --- | --- |
| 能打开菜单但不是自定义 HUD | `Backend`、真实 HUD ID、`sw_hudstatus` 中 `layoutReady`，客户端重连 |
| ID 已注册但 `layoutReady=False` | 检查 MAM 下载日志、`mm_print_searchpaths` 和 VPK 是否包含默认 `.vxml_c` 布局 |
| 有面板但不能点击 | 确认输入捕获正常，无其他菜单插件占用；首次排查可停用旧 mm-cs2menus / mm-cs2admin |
| 中文、位置或边缘显示异常 | 在 Workshop Tools 和实际 CS2 客户端验证字体、分辨率、编译 CSS，浏览器预览仅供参考 |
| 管理/预定菜单提示服务不可用 | 检查插件加载日志和配套共享合约，不要只替换某个旧 DLL |

共享合约更新时配套更新 menus、mapmanager、admin、rockthevote 后重启服务器。布局/截图变化需要另外更新 HUD addon；单纯服务端逻辑修改不要求重新发布布局。下面保留开发接口示例、构建与发布参考。

## Using the menu service

### 1. Reference the contracts assembly

Reference `Sws2Cs2Menus.Contracts.dll` at compile time, with `Private=false`. Deploy the official packages intact, including their
`resources/exports/` assemblies; do not manually substitute incompatible contract DLLs.
SwiftlyS2 uses the exported shared assembly at runtime.

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
using SwiftlyS2.Shared.Players;

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

`UseSharedInterface` resolves interfaces after plugin loading. For manual
installation and diagnosis, load providers before consumers. If `sws2-cs2menus` is missing or failed to load, `_menus` stays null.

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
        .AddOption("example", "Example", context => context.Player.SendChat("Selected"))
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
| `MenuCloseReason` | `Closed`, `Back`, `Selected`, `Replaced`, `Disconnected`, `InvalidPlayer`, `MapUnload`, `PluginUnload`, `PermissionDenied` |

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
- `ReplaceCurrent(player, menu)` refreshes the current page while preserving its parent and sidebar category.
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
`!yd`, unreserved RTV ballots and administrator Map management use this layout and the same screenshot assets. The left category column remains text-only.
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

## Refreshing a live page

`IMenuService.ReplaceCurrent(player, definition)` replaces the displayed page while preserving its parent and sidebar selection. Permission/player/visible-option validation runs before replacement; without a session it opens a root. Both HUD and classic backends implement this operation. Admin map controls use it to refresh status after cancel/pause/resume without growing the navigation stack. Upgrade consumer plugins with the matching contract DLL.

### Requiring the new CS2 HUD

HUD readiness is published as an optional shared `Func<bool>` at the versioned key `sws2-cs2menus.custom-hud.v1` (`MenuServiceConstants.CustomHudInterfaceKey`). Resolve it alongside `IMenuService` during each `UseSharedInterface` call, and invoke it before opening a HUD-only menu. A missing callback means unavailable. `IsAvailable` alone also accepts classic rendering. The callback checks the current backend and mounted layout, and becomes false when menus unloads.

```csharp
private Func<bool>? _customHudAvailable;

public override void UseSharedInterface(IInterfaceManager interfaces)
{
    _menus = interfaces.TryGetSharedInterface<IMenuService>(MenuServiceConstants.InterfaceKey, out var menus) ? menus : null;
    _customHudAvailable = interfaces.TryGetSharedInterface<Func<bool>>(MenuServiceConstants.CustomHudInterfaceKey, out var ready) ? ready : null;
}

private bool CustomHudReady => _menus?.IsAvailable == true && _customHudAvailable?.Invoke() == true;
```

The base menu interface has no HUD readiness member. SwiftlyS2 may select an older same-name contract DLL from another installed plugin; adding a default interface member does not update that loaded assembly and caused `MissingMethodException` in preview 1.0.5-dev.35. Update **menus and armory together**, then fully restart the CS2 process to clear loaded assemblies. This HUD query change works with the previous base contract and does not require rebuilding the other consumers. Preserve each official package's exports.

`python3 tools/check_hud_compatibility.py --plugin build/net10.0/sws2-cs2menus.dll` runs readiness checks normally and again after preloading a frozen legacy contract, then JIT-compiles the actual plugin methods against it without invoking CS2 native operations. It reproduces the missing-member failure on the old implementation. The fixture freezes `IMenuService` and constants from commit `3c0ec3b6aca0016d82f365f32acddeba338ae439`; other types are shared with the current source. These checks and ZIP validation run in CI and release builds.
