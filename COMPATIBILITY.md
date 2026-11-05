# Untranslated Stardew - Mod Compatibility Report

**Version**: 1.2.1  
**Scope**: Dialogue Display Framework Continued (DDFC), StardewUI, and FontSettings compatibility evaluation.

---

## 1. Dialogue Display Framework Continued (DDFC v0.7.6)
- **Source Scope**: Harmony patches specifically target `StardewValley.Menus.DialogueBox` (text rendering and color token parsing inside NPC conversations).
- **Untranslated Stardew Scope**: Targets `IClickableMenu.drawToolTip`, `SkillsPage`, `LevelUpMenu`, `PowersTab`, and `CollectionsPage`.
- **Verdict**: **No overlapping hook found in reviewed source; runtime coexistence not yet verified**. No intersection or overlap with item tooltips, collection pages, or profession menus. No dedicated compatibility adapter is required.

---

## 2. StardewUI (v0.6.1)
- **Source Scope**: `StardewUI.Framework.Views.ViewMenu.DrawTooltip` delegates item tooltip rendering directly to vanilla `IClickableMenu.drawToolTip(..., TooltipData.Item, ...)`.
- **Untranslated Stardew Scope**: `TooltipPatch` intercepts `IClickableMenu.drawToolTip`, which automatically covers StardewUI item hover menus out-of-the-box.
- **Custom StarML Widgets**: For arbitrary custom StarML labels or standalone mod views, third-party authors can query secondary names programmatically via `IUntranslatedStardewApi.GetNames(item)`.
- **Verdict**: **Standard Branch Covered**. No forced dependency patch needed.

---

## 3. FontSettings (v0.15.1)
- **Source Scope**: Replaces underlying MonoGame font assets (`Game1.smallFont` and `Game1.dialogueFont`) at runtime. Standalone `makefont.exe` (.NET 5) and Shared compile-time project (`.shproj`) generate font assets.
- **Untranslated Stardew Scope**:
  - `CollectionsNoteImageLayoutHelper` measures actual `Game1.smallFont.MeasureString` dynamically each frame.
  - `LevelUpLayoutHelper` measures actual `Game1.dialogueFont` and `Game1.smallFont` frame-by-frame, dynamically adjusting subtitle positioning and applying bounded scaling to fit within the measured vertical gap.
- **Verdict**: **Uses runtime font measurements; visual verification still required**. Zero cached font heights; adapts to runtime font and line-spacing substitutions without requiring hard runtime dependencies or custom adapters.
