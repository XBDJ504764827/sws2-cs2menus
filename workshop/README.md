# sws2cs2menus Workshop addon

本目录是 menus 自定义 HUD 的 Panorama 资源源码。admin 管理面板、`!yd` 预定、无人预定 RTV 的选图投票共用这份布局、CSS 和地图截图；只更新插件 DLL 不会自动编译客户端 HUD。

## 中文部署步骤

1. 在 Steam 安装并启动 **Counter-Strike 2 Workshop Tools**，创建名为 `sws2cs2menus` 的 addon。
2. 将本目录的 `panorama/` 复制到 `Counter-Strike 2/content/csgo_addons/sws2cs2menus/`，保持 XML、CSS 和图片的目录结构及面板 ID 不变。
3. 在 Workshop Tools 编译布局、两个 CSS 文件与图片，发布为玩家可以下载的工坊资源，记录其真实数字 Workshop ID。
4. 安装服务器 MultiAddonManager，在 `csgo/addons/swiftlys2/configs/plugins/sws2-cs2menus/config.jsonc` 的 `Menu` 中填入 `HudLayoutWorkshopId`，保持 `Backend: "auto"` 和 `AutoMountHudAddon: true`。ID 也可作为包内 `resources/hud-addon.json` 默认值，运行配置优先；仓库默认 ID 为空。
5. menus 会将 HUD ID 合并到实时 `mm_extra_addons` 并触发 MAM 刷新，保留其他 addon。无需将地图目录中所有地图 ID 加到此列表；HUD addon 和地图是不同资源。
6. 服务器控制台运行 `sw_hudstatus`，确认 `layoutReady=True`、`renderer=hudlayout`。客户端完成下载后重连，先验证 `!menudemo`，再验证 `!admin`、`!yd` 和无人预定的 RTV 投票。

`auto` / `hudlayout` 在布局未就绪时先回退中心 HTML；`classic` 固定中心 HTML 并关闭自动 HUD 注册。ID 已在列表中不表示资源已下载或挂载。需要同时检查 MAM 日志、`mm_print_searchpaths`、VPK 内是否包含 `panorama/layout/custom_game/sws2cs2menus/menu.vxml_c`，以及客户端是否已获取更新。

### 更新地图截图

评分来自 mapmanager 配置，修改后重开菜单即可，不用重发工坊资源。截图是 addon 内的本地 CSS 背景；Steam 运行时作者/标题信息查询不会替换图片。进入 menus 仓库运行：

```sh
python3 tools/build_map_thumbnails.py --config /path/to/mapmanager/config.jsonc
```

离线时提供图片映射：

```json
{
  "kz_angina": "screenshots/angina.jpg",
  "workshop:3746453872": "screenshots/angina_x.png"
}
```

```sh
python3 tools/build_map_thumbnails.py --config /path/to/mapmanager/config.jsonc --images images.json --offline
```

相对图片路径以映射文件所在目录为基准；旧图有缓存，需要重新获取时先移除对应缓存。重新编译 XML、两个 CSS 文件和图片，更新同一个 Workshop addon；客户端重连，必要时重启客户端清理布局缓存。稳定地图 ID 不变时，仅改名称或评分不需要重做截图。

### 验证组合功能

- `!admin` 应显示玩家、地图、封禁、服务器管理中当前管理员有权限的分类；子页返回和切换分类不执行未确认的动作。
- `!yd` 和无预定 RTV 选图应使用相同截图、四类评分与分页，但前者是预定，后者提交当前投票选票。
- CKZ NUB/PRO、VNL NUB/PRO 分别独立，NUB 为存点、PRO 为裸跳；未知评分和缺图显示占位。
- 每位玩家有独立面板，关闭后释放鼠标捕获；活跃 RTV 中未提交玩家可以用 `!rtv` 重开。
- 浏览器预览只使用样例数据，不能替代实际客户端对中文字体、鼠标、分辨率和布局编译的验证。

完整插件配置与搭配见 [menus 使用指南](../README.md)。下面保留英文资源结构、发布和排错参考。


This folder contains the minimal Panorama layout used by the `custom_hud_layout`
menu backend.

```text
workshop/
└── panorama/
    ├── layout/custom_game/sws2cs2menus/menu.xml
    ├── styles/custom_game/sws2cs2menus/{menu,map_thumbnails}.css
    └── images/custom_game/sws2cs2menus/maps/*.jpg
```

The layout provides a centered 980 × 660 window (1160 × 660 for maps with a sidebar) on CS2's 1080-high
HUD canvas. A 214-pixel category column stays on the left while the right pane
shows the active list, player action menu or confirmation. Navigation and paging
stay at the bottom of the right pane. Player and ban pages are text-only. Administrator Map management, `!yd` and unreserved RTV ballots use the map picker layout with shared screenshots and independent CKZ NUB/PRO and VNL NUB/PRO ratings. No decorative icons are used.

The `!o` menu in cs2kz-metamod was used as an interaction reference; this addon
has its own XML, CSS and server contract. Keep `sw_category0`–`sw_category4` and
all dialog-variable panel IDs in sync with the plugin.

Update the plugin DLLs and recompile both XML and CSS when upgrading. Replace
old compiled `sws2cs2menus` resources when testing locally; restart the client to
clear cached layouts. When distributing through the Workshop, update the same
addon and reconnect after clients download the update.

## 1. Create the addon

1. Install and launch **Counter-Strike 2 Workshop Tools** from Steam.
2. Create a new addon named `sws2cs2menus`.
3. The addon content directory will look like:

```text
Counter-Strike 2/content/csgo_addons/sws2cs2menus/
```

## 2. Add the layout files

Copy the `panorama` folder from this repository into the addon root:

```text
Counter-Strike 2/content/csgo_addons/sws2cs2menus/
└── panorama/
    ├── layout/custom_game/sws2cs2menus/menu.xml
    ├── styles/custom_game/sws2cs2menus/{menu,map_thumbnails}.css
    └── images/custom_game/sws2cs2menus/maps/*.jpg
```

Keep the directory names exactly as shown. The server writes panel IDs such as
`sw_item0` and `sw_close`, and the layout must contain matching IDs.

## 3. Compile and publish

1. Open the addon in Workshop Tools.
2. Let the Panorama compiler build the layout and styles.
3. Use the Workshop publishing dialog to publish the addon.
4. Copy the resulting **Workshop ID**, for example:

```text
1234567890
```

Public or unlisted visibility both work, as long as clients can download it.

## 4. Register the ID once for automatic mounting

Install MultiAddonManager, then set menus' `Menu.HudLayoutWorkshopId` to the
published ID and leave `AutoMountHudAddon` enabled. Alternatively, package
`resources/hud-addon.json` with `{ "WorkshopId": "1234567890" }` as the plugin's
default (replace the example with your real ID). A nonempty config value wins.
The distributed manifest currently has an empty ID until you register your addon.

Menus waits for MAM, merges the ID into the live `mm_extra_addons` ConVar and uses
a normal engine console assignment so MAM's change callback refreshes/downloads/mounts the addons.
`mm_add_addon` only registers an ID and does not refresh; it is no longer used.
Unrelated addon IDs and their order are preserved.
You no longer need to add the HUD ID to `mm_extra_addons` by hand. The existing
MultiAddonManager config may keep IDs used by other addons.
The same Workshop item retains its ID across updates. Steam/Workshop download
access and CS2 client downloads are still required. Reconnect clients after an update.

## 5. Enable the HUD backend

In `game/csgo/addons/swiftlys2/configs/plugins/sws2-cs2menus/config.jsonc`:

```jsonc
{
  "sws2-cs2menus": {
    "Menu": {
      "Backend": "auto",
      "HudLayoutPath": "panorama/layout/custom_game/sws2cs2menus/menu.vxml_c",
      "HudLayoutWorkshopId": "1234567890",
      "AutoMountHudAddon": true
    }
  }
}
```

Replace the example ID with your published HUD addon's ID and merge these fields
into the existing configuration, preserving other settings.

Both `auto` and `hudlayout` use center HTML while resources are absent, then switch
to Custom HUD once the layout is mounted. `classic` keeps the center-HTML renderer
and disables automatic addon registration. Checks run every five seconds and on
map load; failed registrations retry at most every 30 seconds. An existing ID with no ready
layout receives one refresh per map/config cycle, using a changed trailing delimiter
if necessary to trigger the callback without changing addon membership.

Check `sw_hudstatus` in the server console, then reopen `!menudemo` or `!admin`.
For unpublished local compiled-resource testing, set `AutoMountHudAddon: false`
and mount your local compiled addon on both server and client as before.

## 6. Test checklist

- Client downloaded the Workshop addon.
- `mm_extra_addons` contains the new ID.
- `Backend` is `hudlayout` (or `auto` after the addon is mounted).
- `HudLayoutPath` matches the layout file.
- `!menudemo` opens the HUD.
- The centered panel appears with a short fade/slide animation.
- `!admin` selects the first permitted category and displays its content on the right.
- Permitted Player actions / Map management / Ban management / Server management categories remain visible on the left in child pages.
- Switching category from a confirmation page does not execute the pending action.
- Kicks / bans show the target and duration before confirmation.
- Back inside a category preserves the previous page; category roots have no Back action.
- Submenu and page changes replay the option-list animation.
- Mouse click selects items and shows the press effect.
- Previous / Next text buttons change pages.
- `Back` returns to the parent menu.
- The text Close button closes the menu and releases the cursor.
- Map menus show map names and configured Workshop IDs without icons.

## Troubleshooting

### No menu appears

Check the server log for `custom_hud_layout` errors. Make sure the client has
finished downloading the addon and reconnect once after it is mounted.

### Menu appears but cannot be clicked

The layout must be the active input-capture layout. Check that
`SetInputCaptureEnabledForPlayer` succeeds and that no other plugin is holding
input capture. Disable `mm-cs2menus` / `mm-cs2admin` during the first test.

### Menu is offset or clipped

The design uses a 1080-high HUD canvas, scaled by CS2. Check 16:9, 4:3 and
ultrawide clients. Long titles, player names and map names are truncated rather
than expanding rows; test Chinese fonts and long Workshop names in Workshop Tools.
The browser preview is an approximation and cannot validate Panorama compilation.

### Layout not found with `auto`

Run `sw_hudstatus` in the server console. Check the registered ID, MAM installation,
download status and layout path. Availability is rechecked periodically; reopen the
menu after the renderer switches. Reconnect if client resources are stale.

### ID is listed but addon does not load

Older plugin builds used `mm_add_addon`, which only adds the ID to the list.
Update the plugin: it now triggers MAM's refresh callback even when the ID is
already registered. `sw_hudstatus` and the raw alias `hudstatus` both work in
server console; the old build accepts only `hudstatus`.

Check the reported `layoutReady`, layout path and MAM download messages. A listed
ID alone does not prove successful download or mounting. `mm_print_searchpaths`
shows mounted VPK paths. If a VPK is mounted but `layoutReady=False`, inspect its
compiled contents for `panorama/layout/custom_game/sws2cs2menus/menu.vxml_c`.
Steam access or Workshop availability failures must be resolved at the download layer.
After the initial download, MAM can reload the current map; connected clients may
need to reconnect to receive the HUD addon.

## Map reservation pictures

`!yd` in sws2-cs2rockthevote uses the MapPicker layout: four map rows per page,
map names, separate CKZ NUB/PRO and VNL NUB/PRO badges, local screenshot backgrounds and text
selection buttons. Unrated categories and maps without screenshots each have an
explicit placeholder. All panels remain Panel/Label/Button; no Image element or
client script is required. Administrator Map management uses exactly the same MapPicker assets and keys; the left category column remains text-only.

The source includes packaged Workshop previews. Add screenshots for your full mapmanager catalog before publishing. Generate pictures for another
catalog from the menus repository:

```sh
python3 tools/build_map_thumbnails.py --config /path/to/mapmanager/config.jsonc
```

This reads both compact and extended maps and legacy arrays, downloads Steam
preview images, copies PNG/JPEG files into the addon and writes
`map_thumbnails.css`. Classes are derived from the stable map ID, so renaming a
map or changing any mode/run ratings does not require regenerating pictures. Existing
images are cached; delete a cached image first to download a newer screenshot.
Steam failures keep cached pictures and leave other maps with a placeholder.

For offline development, provide an image mapping file (relative paths resolve
against that file):

```json
{
  "kz_angina": "screenshots/angina.jpg",
  "workshop:3746453872": "screenshots/angina_x.png"
}
```

```sh
python3 tools/build_map_thumbnails.py --config /path/to/config.jsonc --images images.json --offline
```

Recompile **XML, both CSS files and images** with Workshop Tools. Mount the new
compiled addon on the server and client for local testing; restart/reconnect the
client to clear cached layouts. Publish an addon update when ready to distribute.
Remote runtime `ImageUrl` values remain metadata for other renderers; the native
HUD cannot assign arbitrary background URLs through its server API.

Browser preview: open `preview/index.html?menu=reservation`. Its ratings are
illustrative sample data, not the actual ratings of those maps. Validate resource
compilation, fonts, input capture and screenshots in a real CS2 client separately.

NUB denotes checkpoints (存点); PRO denotes no checkpoints (裸跳). The four badges form a two-by-two grid: CKZ on the first row, VNL on the second, NUB on the left and PRO on the right. Recompile the updated XML/CSS when upgrading this layout.
