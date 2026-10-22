# Launcher 1.0.23 — HDR options, live HP/MP and configurable shortcuts

- Accepts local root `package.json` launch preferences such as `"chromium-args": "--force-color-profile=srgb"` in normal and runtime-control launches. Keep the original `main` entry and valid JSON. Exact original-source/reconstruction checks remain in place.
- Adds live HP/MP bars with current/maximum values for the active party leader, visible across control-panel tabs.
- Refines navigation, rounded controls and smooth hover highlights while retaining themes, opacity, resizing and header-only dragging.
- **Ctrl+Shift+G** replaces F8 as the default. Click the shortcut button in the panel header to record another combination. The binding is saved per control session and also works detached. The on-screen Controls button remains available.

Game data, English translations and save locations are unchanged. All earlier streaming, translation refresh, status-panel and updater safeguards are retained. No need to remove/reapply the English overlay for this launcher update.

English application also handles brief Windows file-lock/remove errors with bounded atomic-replacement retries. Hash guards preserve external edits; persistent failures remain explicit. This was reproduced in ZIP overlay checks and in controlled file-handle tests.

For HDR: close the game, edit its root `package.json`, and append `--force-color-profile=srgb` to the existing `chromium-args` string if present. Retain an original archive for future exact-source reconstruction. HDR display appearance has not been verified on an HDR monitor.

For Bitdefender-affected 1.0.19–1.0.21 users, follow the README's one-time ZIP transition. Bitdefender validation of the earlier updater correction remains pending; this release does not change antivirus settings or restore quarantined files.
