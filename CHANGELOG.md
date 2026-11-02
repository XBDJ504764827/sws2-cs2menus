# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Fixed

- Keep the HUD layout Back label on one line by widening the button, setting an explicit label width and inserting word joiners between characters so older Workshop layouts cannot wrap it vertically.

### Changed

- Restyle the HUD layout as a full-height left sidebar with compact option rows, a bottom navigation bar and clearer hover states.
- Add sidebar entry, submenu/page transition and button press animations driven by alternating HUD classes.

### Added

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
