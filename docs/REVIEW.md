# Code Review Report: Untranslated Stardew

- **Review Date**: 2026-11-05
- **Working Tree**: `E:\Sao lưu\UntranslatedStardew`
- **Scope**: Item tooltips bilingual names, search API, RawContentProvider lifecycle, Harmony patches, tool upgrade handling, flavor format precedence.
- **Result**: **No actionable correctness bug found**. Code compiles cleanly and all 78 regression/smoke tests pass.

---

## I. Detailed Verification Summary

1. **Flavor Precedence & Recursion**:
   - `ItemNameResolver.cs:105-117`: `displayNameFormat` is strictly prioritized over `preserveType`, faithfully reproducing vanilla `Object.GetObjectDisplayName` precedence.
   - Preserved item resolution (`ResolveFlavoredObject`) and format resolution (`ResolveDisplayNameFormat`) query `GetSecondaryNameById`, which delegates only to raw metadata (`ResolveFromParsedData`), ensuring zero infinite recursion risk.
   - Specific override templates (e.g. `DriedFruit_Flavored_(O)258_Name`) are correctly used directly without double English pluralization (`ItemNameResolver.cs:374`).

2. **Tool Instance Naming & Cache**:
   - `ItemCacheKey.cs`: Correctly isolates tools by `UpgradeLevel` and objects by `PreserveType`, `PreservedId`, and `DisplayNameFormat`.
   - `ItemNameResolver.cs:197-209`: Properly supports both 1.6 native upgraded tool IDs (e.g. `CopperAxe`) and legacy instances (`Axe` + `UpgradeLevel = 1`).

3. **Provider Lifecycle & Content Isolation**:
   - `RawContentProvider.cs`: Isolated MonoGame `ContentManager` instances prevent pollution of global vanilla asset caches and locale states.
   - `ModEntry.cs:87-93`: Re-initializes provider on `GameLaunched` to lazily bind `Game1.content.ServiceProvider`.
   - On GMCM config change or mod unload (`Dispose`), previous providers and internal `ContentManager`s are disposed cleanly.

4. **Harmony Geometry**:
   - `Patches/TooltipPatch.cs:13-35`: Correctly targets static `IClickableMenu.drawToolTip` with `ref string hoverTitle`.
   - `IClickableMenu.drawHoverText` in vanilla SV natively calculates multiline string heights using `Game1.dialogueFont.MeasureString`, automatically expanding the tooltip box vertically.

5. **Search API & Normalization**:
   - `Core/SearchMatcher.cs`: Correctly strips diacritics via Unicode FormD decomposition and normalizes Vietnamese `đ`/`Đ` -> `d`/`D`.
   - `UntranslatedStardewApi.cs`: Stable wrapper holding dynamic delegates (`_getResolver`, `_getConfig`), safely handling config updates without breaking consumer references.

---

## II. Runtime Coverage Boundaries (For In-Game Verification)

1. **Font Glyph Support**:
   - If secondary language is set to a non-Latin script (e.g., `zh`, `ja`, `ru`, `ko`) while the main game runs in English, `Game1.dialogueFont` lacks those glyph sheets unless a font mod or multilingual font pack is installed.
2. **Third-party Virtual Assets**:
   - Disclosed limit: `RawContentProvider` reads disk XNBs directly, so Content Patcher in-memory virtual edits to vanilla string tables won't be seen by the secondary provider.
3. **Custom Third-party UI Menus**:
   - Menus bypassing `IClickableMenu.drawToolTip` require integration with `IUntranslatedStardewApi`.
