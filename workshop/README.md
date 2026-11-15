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

The layout provides a centered 980 × 660 text-only window on CS2's 1080-high
HUD canvas. A 214-pixel category column stays on the left while the right pane
shows the active list, player action menu or confirmation. Navigation and paging
stay at the bottom of the right pane. The administrator panel has no images or icons. A separate map picker layout can show optional map screenshots and independent CKZ/VNL ratings.

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

## 4. Mount it with MultiAddonManager

Edit:

```text
game/csgo/cfg/multiaddonmanager/multiaddonmanager.cfg
```

Add the ID to `mm_extra_addons`, keeping any existing IDs:

```text
mm_extra_addons "3801628903,3469155349,1234567890"
```

- `3801628903` is the mm-cs2menus addon, if used.
- `3469155349` is the cs2kz addon, if used.
- Replace `1234567890` with your new addon ID.

Restart the server or change the map so MultiAddonManager mounts the addon.
Clients will download it automatically on connect.

## 5. Enable the HUD backend

Edit the plugin config:

```text
game/csgo/addons/swiftlys2/configs/plugins/sws2-cs2menus/config.jsonc
```

Set:

```jsonc
"Menu": {
  "Backend": "hudlayout",
  "HudLayoutPath": "panorama/layout/custom_game/sws2cs2menus/menu.vxml_c"
}
```

`auto` uses `hudlayout` only when the layout is already mounted when the plugin
loads. `hudlayout` forces the custom HUD backend. `classic` forces the old center
HTML menu.

Reload `sws2-cs2menus` or restart the server, then run:

```text
!menudemo
```

## 6. Test checklist

- Client downloaded the Workshop addon.
- `mm_extra_addons` contains the new ID.
- `Backend` is `hudlayout` (or `auto` after the addon is mounted).
- `HudLayoutPath` matches the layout file.
- `!menudemo` opens the HUD.
- The centered panel appears with a short fade/slide animation.
- `!admin` selects the first permitted category and displays its content on the right.
- Player actions / Map management remain visible on the left in all child pages.
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

Set `Backend` to `hudlayout` explicitly, or restart the plugin after the addon is
mounted. The `auto` check runs when `sws2-cs2menus` loads.

## Map reservation pictures

`!yd` in sws2-cs2rockthevote uses the MapPicker layout: four map rows per page,
map names, separate CKZ and VNL badges, local screenshot backgrounds and text
selection buttons. Unrated modes and maps without screenshots each have an
explicit placeholder. All panels remain Panel/Label/Button; no Image element or
client script is required. The admin sidebar never displays these pictures.

The source includes Workshop previews for the 15 maps in mapmanager's
`resources/templates/map-catalog.example.jsonc`. Generate pictures for another
catalog from the menus repository:

```sh
python3 tools/build_map_thumbnails.py --config /path/to/mapmanager/config.jsonc
```

This reads both compact and extended maps and legacy arrays, downloads Steam
preview images, copies PNG/JPEG files into the addon and writes
`map_thumbnails.css`. Classes are derived from the stable map ID, so renaming a
map or changing CKZ/VNL ratings does not require regenerating pictures. Existing
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
