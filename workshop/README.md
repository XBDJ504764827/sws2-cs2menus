# sws2cs2menus Workshop addon

This folder contains the minimal Panorama layout used by the `custom_hud_layout`
menu backend.

```text
workshop/
└── panorama/
    ├── layout/custom_game/sws2cs2menus/menu.xml
    └── styles/custom_game/sws2cs2menus/menu.css
```

The layout provides five clickable item slots, a title, page indicator, comment
line, previous/next page buttons, a Back button and a Close button. All labels
are plain text; colors and visibility are controlled by CSS classes.

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
    └── styles/custom_game/sws2cs2menus/menu.css
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
  "HudLayoutPath": "panorama/layout/custom_game/sws2cs2menus/menu.xml"
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
- Mouse click selects items.
- `<` and `>` change pages.
- `Back` returns to the parent menu.
- `X` closes the menu and releases the cursor.

## Troubleshooting

### No menu appears

Check the server log for `custom_hud_layout` errors. Make sure the client has
finished downloading the addon and reconnect once after it is mounted.

### Menu appears but cannot be clicked

The layout must be the active input-capture layout. Check that
`SetInputCaptureEnabledForPlayer` succeeds and that no other plugin is holding
input capture. Disable `mm-cs2menus` / `mm-cs2admin` during the first test.

### Menu is offset or clipped

Adjust `.menu-box` and `.item` sizes in `menu.css`. Text width cannot be measured
by the server, so keep labels reasonably short.

### Layout not found with `auto`

Set `Backend` to `hudlayout` explicitly, or restart the plugin after the addon is
mounted. The `auto` check runs when `sws2-cs2menus` loads.
