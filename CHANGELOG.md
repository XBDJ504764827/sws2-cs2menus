# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Live page replacement

- Add ReplaceCurrent to the shared API and both renderers, preserving parent navigation and sidebar selection for administrator status refresh.
- Extend the browser preview with map controls, server tools and the spectate action.

### Fix registered HUD addons not being mounted

- Replace `mm_add_addon` (registration only, refresh defaults to false) with a normal engine `mm_extra_addons` assignment that invokes MAM's download/mount refresh callback.
- Refresh once when an existing HUD ID has no ready layout, preserving other IDs and their order; warn if resources remain absent instead of treating registration as success.
- Register both `sw_hudstatus` and `hudstatus`, and print the live addon list, layout path and readiness.

### Automatic HUD mounting and administrator maps

- Register the HUD Workshop ID once in config or the packaged manifest, then append it through MultiAddonManager without replacing other addons.
- Wait for late-loaded MAM/downloaded layouts, retry registration and switch the shared backend once resources are available.
- Add server-only `sw_hudstatus` diagnostics and a local-development opt-out.
- Widen the sidebar map page to fit shared screenshots and four rating badges; extend the preview with ban management.

### NUB / PRO ratings

- Display four independent CKZ/VNL NUB/PRO badges in a two-by-two grid, with a localized checkpoints/no-checkpoints legend and separate unknown values.

### Map reservations

- Add a MapPicker layout with four rows per page, independent CKZ/VNL badges and optional packaged map screenshots, leaving the administrator sidebar text-only.
- Add a Steam/local screenshot packaging tool, cached/offline fallback and previews for the 15 requested maps; use CSS backgrounds without Image panels or JavaScript.
- Extend the browser preview with `?menu=reservation`; sample ratings are illustrative only.

### Sidebar revision

- Replace feature cards with persistent text categories on the left and functional pages on the right, following the interaction of cs2kz's `!o` menu.
- Automatically open the first enabled category; rebuild live content on category switches and preserve page position when navigating Back inside a category.
- Remove all image/icon panels, SVG assets and glyph output. Use localized text for Close and paging.
- Keep the category column visible through action and confirmation pages, and discard pending confirmations when switching categories.

### Fixed

- Validate clicks against the options actually rendered and recheck current menu permissions; respect hidden navigation actions.
- Align dependency versions with SwiftlyS2 1.5.2 to resolve package downgrades.

- Keep the HUD layout Back label on one line by widening the button, setting an explicit label width and inserting word joiners between characters so older Workshop layouts cannot wrap it vertically.

### Changed

- Replace the old sidebar with a centered dark Custom HUD, mint accents, local vector icons, breadcrumbs, stable paging and Home/Back navigation.
- Add panel entry, submenu/page transition and button press animations driven by alternating HUD classes.

### Added

- Optional dashboard and confirmation presentation hints, destructive action appearance and local player/map/warning icons.
- A standalone interactive browser preview generated from the Panorama source.
- Package Workshop source alongside plugin releases for compilation and publication.

- Minimal `custom_hud_layout` menu backend with mouse-clickable Panorama layout.
- `Menu.Backend` selection (`auto`, `hudlayout`, `classic`) with center-HTML fallback.
- `workshop/` Panorama layout source and Workshop Tools/MultiAddonManager guide.

## [1.0.0] - 2026-10-26

### Added

- Initial release of `sws2-cs2menus`.
- Shared `IMenuService` exposed through SwiftlyS2's shared interface manager.
- Fluent menu and option builders with nested submenus.
- Automatic pagination, Back and Close navigation through SwiftlyS2's component menu framework.
- Menu-level and option-level permission checks, per-player visibility and enabled states.
- Menu opened/closed events and per-menu closed callbacks.
- Player disconnect, map unload and plugin unload cleanup.
- Hot-reloadable configuration and a `!menudemo` test menu.
- English, Simplified Chinese and Traditional Chinese translations.
- Build, CI, PR pre-release and main-branch release workflows.
