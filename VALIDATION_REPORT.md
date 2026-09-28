# BlueLobby 4.3.0 — Final Engineering Validation

## Scope

The active product scope is Windows x64 + Linux x64 desktop. Mobile, Android/iOS and macOS are intentionally out of scope.

## Hardened areas

- Journaled patch/restore transaction with durable manifest writes.
- SHA-256 validation for originals, backups and patched/managed files.
- PE/ELF architecture detection instead of filename-based architecture assumptions.
- Bounded API DLL discovery; no drive-wide recursive DLL replacement.
- Path/reparse-point safety checks.
- Per-game process lock plus in-process serialization.
- Restore conflict detection: user-modified files are not silently overwritten.
- Firewall rule ownership tied to the BlueLobby transaction ID.
- UI progress routed through Avalonia's UI dispatcher.
- Steam VDF parsing uses a tokenizer rather than a regex-only parser.
- Steam library discovery supports platform-provided roots, common Linux/Flatpak roots and XDG data roots.
- Heroic discovery includes Linux, Flatpak and Windows AppData/legacy config locations.
- Lutris discovery resolves `$GAMEDIR`, absolute executables and working directories.
- Linux Proton launch path uses Steam AppID launch when the selected executable is a Windows `.exe`; native Linux executables retain direct launch behavior.
- Windows and Linux CI both restore, build and run the integration test executable.
- Windows/Linux release jobs wait for both platform test jobs.
- Settings now carry a schema version and transparently migrate the previous unversioned format.

## External validation basis

- .NET 10 is the current LTS release and is supported through November 14, 2028.
- Avalonia documents `Dispatcher.UIThread` for UI updates from worker threads.
- Valve's Proton documentation confirms per-game `steamapps/compatdata/<appid>/pfx` prefixes and Steam-managed Proton execution.
- Heroic documents Linux/Flatpak and Windows configuration locations and its use of Legendary `installed.json`.
- Lutris documents `exe`, `working_dir`, `$GAMEDIR` and runner-specific launch behavior.
- GitHub's .NET Actions guidance supports matrix/platform-specific build/test jobs.

## Validation limitations

The supplied execution environment does not contain the .NET SDK and outbound package download is unavailable. Therefore the final package was validated with source-level consistency checks, project/reference checks, YAML parsing, targeted fixture tests at the source level, archive integrity checks and manual cross-platform API review. The GitHub Actions pipeline is the authoritative compiler/runtime verification step.


## UI validation

Static UI checks completed for v4.3.0. The updated UI remains Windows/Linux desktop-only and preserves the existing event handlers and patch/restore entry points.


## 2026-09-28 Hardening recheck

- PatchEngine firewall operations use an end-to-end awaitable flow; sync-over-async calls were removed.
- Windows/Linux VPN detection is tied to recognized interfaces and validated address ranges.
- A separate xUnit.net v3 unit-test layer complements the existing integration test executable.
- Release packages now publish SHA-256 checksum files and GitHub artifact provenance attestations.
- UpdateChecker remains inert while URLs are empty; if enabled later, remote/update targets must be HTTPS.
