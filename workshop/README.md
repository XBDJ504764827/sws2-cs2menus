# sws2cs2menus Workshop addon

This folder contains the minimal Panorama layout used by the `custom_hud_layout`
menu backend.

```text
workshop/
└── panorama/
    ├── layout/custom_game/sws2cs2menus/menu.xml
    ├── styles/custom_game/sws2cs2menus/menu.css
    └── images/custom_game/sws2cs2menus/{players,maps,warning}.svg
```

The layout provides a centered 940 × 716 panel on CS2's 1080-high HUD canvas,
with a brand header, administrator name, breadcrumb trail, option count, five list
slots and a fixed footer. Dashboard menus show two feature cards; confirmations
use an amber accent and red destructive actions. Local SVG icons need no external
image service. The server alternates refresh classes for content transitions.

Copy **all** of `panorama/`, including `images/custom_game/sws2cs2menus/`, and
republish the existing addon when upgrading. Updating only the plugin DLL does
not update the client layout. A new client download / reconnect may be needed.

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
    ├── styles/custom_game/sws2cs2menus/menu.css
    └── images/custom_game/sws2cs2menus/{players,maps,warning}.svg
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
- `!admin` shows Player actions and Map management cards.
- Kicks / bans show the target and duration before confirmation.
- Home returns to the root; Back preserves the previous page.
- Submenu and page changes replay the option-list animation.
- Mouse click selects items and shows the press effect.
- `<` and `>` change pages.
- `Back` returns to the parent menu.
- `X` closes the menu and releases the cursor.
- Map menus show local map icons and the configured Workshop IDs.

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
