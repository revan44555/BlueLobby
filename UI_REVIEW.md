# BlueLobby 4.3.0 UI Review

## Scope
Windows x64 + Linux x64 desktop only. No Android/iOS/macOS target was introduced.

## Design changes
- Desktop-first two-column shell with clearer brand/navigation hierarchy.
- Stronger page header and section hierarchy.
- Cards now use centralized surface/border/radius metrics.
- Primary action is visually separated from utility actions.
- Status chips are grouped by importance.
- Activity log receives a dedicated technical surface.
- Settings/friends/discovery areas use consistent spacing and controls.
- Narrow desktop/Steam Deck window behavior remains supported without turning the app into a mobile UI.

## Compatibility goal
UI changes are isolated to `MainWindow.Ui.cs`, `Ui.cs`, `ThemeManager.cs`, and localization additions. Patch/restore/transaction core logic is not intentionally modified by this UI pass.

## Verification
- C# brace-balance/static structure checks performed.
- Localization keys used by the UI shell were added for Turkish, English and German.
- Final archive is root-flat: extracting it does not create an extra `BlueLobby/BlueLobby/...` layer.
- Real `dotnet build/test` remains delegated to the repository CI because this execution environment does not have the .NET SDK installed.
