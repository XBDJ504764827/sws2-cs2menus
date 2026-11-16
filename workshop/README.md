# sws2cs2menus Workshop addon

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
stay at the bottom of the right pane. Player and ban pages are text-only. Both administrator Map management and `!yd` use the map picker layout with shared screenshots and independent CKZ NUB/PRO and VNL NUB/PRO ratings. No decorative icons are used.

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
"Menu": {
  "Backend": "auto",
  "HudLayoutPath": "panorama/layout/custom_game/sws2cs2menus/menu.vxml_c",
  "HudLayoutWorkshopId": "1234567890", // replace with your published ID
  "AutoMountHudAddon": true
}
```

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
- Player actions / Map management / Ban management remain visible on the left in all child pages.
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
