# Untranslated Stardew - Mod Compatibility Report

**Version**: 1.3.0
**Scope**: Dialogue Display Framework Continued (DDFC), StardewUI, FontSettings, Content Patcher, and Custom Language Packs.

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
- **Verdict**: **Standard branch covered by source inspection; in-game coexistence unverified**. No forced dependency patch needed.

---

## 3. FontSettings (v0.15.1)
- **Source Scope**: Replaces underlying MonoGame font assets (`Game1.smallFont` and `Game1.dialogueFont`) at runtime.
- **Untranslated Stardew Scope**:
  - `BilingualRunPlanner` strictly preserves the active `Game1` font for all primary chunks, automatically honoring any font replacements or zoom levels applied by FontSettings.
  - Secondary chunks consistently use the secondary locale font without mutating primary font settings.
  - `CollectionsNoteImageLayoutHelper` and `LevelUpLayoutHelper` dynamically measure actual runtime font metrics frame-by-frame.
- **Verdict**: **Source-compatible design; runtime visual coexistence remains unverified**. Primary role chunks seamlessly inherit FontSettings custom fonts, while secondary role chunks strictly load their designated locale font.

---

## 4. Content Patcher & Custom Language Packs (Generic Language Sources)
- **Scope**: Custom language packs (e.g. Ukrainian, Thai, Vietnamese, etc.) providing localized assets and fonts.
- **Untranslated Stardew Scope**:
  - Custom language packs register their strings, fonts (`SmallFont`, `DialogueFont`), and content root via the SMAPI dictionary asset `Mods/GiaHung.UntranslatedStardew/LanguageSources` or C# API.
  - `LanguageSourceRegistry` verifies path safety, prevents traversal outside the pack root, and invalidates caches upon pack reload.
- **Verdict**: **Explicit source registration supported**. Arbitrary pack layouts and BMFont-only packs are not auto-discovered. See docs/LOCALE-ROLE-FONTS-1.3.0.md.
