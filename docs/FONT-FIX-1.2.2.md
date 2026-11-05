# UI Missing-Glyphs Bug Fix Report & Acceptance (v1.2.2)

## 1. Problem Statement & Root Cause Analysis
- **Root Cause**: In vanilla Stardew Valley (`Game1.cs:2360-2362`), `Game1.dialogueFont` (`Fonts/SpriteFont1`) and `Game1.smallFont` (`Fonts/SmallFont`) are loaded strictly once for the primary game language. Vietnamese translation packs (`Kou.ViethoaSV`) condition font loading on `When: { "Language": "vi" }`. When the primary game language is English (`en`) or any non-Vietnamese locale, Content Patcher skips loading Vietnamese font assets.
- **DefaultCharacter Behavior**: Binary inspection of vanilla and mod XNB fonts confirms `DefaultCharacter = '*'` (ASCII 42). When MonoGame's `SpriteBatch.DrawString` or `SpriteFont.MeasureString` encounters characters outside the active primary font (such as Vietnamese diacritics or CJK ideographs), it substitutes them with `*` (e.g., `Cá nóc` renders as `C* n*c`).
- **Sizing & Text Wrapping Discrepancies**: Game menus (e.g., `Game1.parseText`, `PowersTab`, `CollectionsPage`, `LevelUpMenu`) measure widths using primary fonts. Missing glyphs measure as `*`, resulting in incorrect wrapping boundaries and overlapping UI elements.

---

## 2. Architecture: Per-Language Font Runs
- **Isolated Asset Loading**: Uses dedicated, isolated `Microsoft.Xna.Framework.Content.ContentManager` instances for vanilla content and mod content (`Kou.ViethoaSV`). Never invokes `LocalizedContentManager.CurrentLanguageCode` or pollutes `LocalizedContentManager.localizedAssetNames`.
- **Primary Font Preservation**: Keeps `Game1.dialogueFont` and `Game1.smallFont` as the primary fonts. Any dynamic font overrides installed by `FontSettings` are honored automatically.
- **Run Segmentation & Baseline Consistency**: Text blocks are segmented into contiguous font runs. Where an entire line contains characters requiring the secondary font, the entire line is rendered using the secondary font to preserve uniform vertical baselines, kerning, and accent positioning.
- **Unicode NFC Normalization**: Secondary text is normalized to Unicode Normalization Form C (`NormalizationForm.FormC`) prior to measurement and rendering, preventing decomposed combining marks (NFD) from separating or turning into `*`.
- **Process-Wide Harmony Entrypoints with Strict Active-Scope Passthrough**: `SpriteFont.MeasureString` and `SpriteBatch.DrawString` overloads are patched via Harmony. They only intercept when `BilingualRenderScope.IsActive == true` and the text is tracked in `BilingualStringTracker`. Outside active bilingual surfaces or for untracked text, methods return `true` immediately to execute MonoGame native logic 100% byte-for-byte unchanged.
- **Idempotent Scope Cookie (`ScopeCookie`)**: Uses reference-type `ScopeCookie` (`class`) with `Interlocked.Exchange` disposal. Prevents premature scope closure when both Harmony `Postfix` and `Finalizer` execute on the same invocation or across nested menus.
- **Recursion Guard**: Thread-local `IsMeasuringActive` and `IsRenderingActive` flags wrapped in `try ... finally` blocks guard both `TryMeasureString`, `TryDrawString`, and internal run planning against infinite recursion even when glyphs are missing in both fonts.
- **NFC Fast-Path Preservation**: When input is decomposed (NFD) but all precomposed characters exist in the primary font, a normalized single-font plan is constructed so precomposed glyphs render cleanly rather than passing combining marks to native DrawString.
- **Transforms & Global Block Flipping**: Preserves `SpriteEffects.FlipHorizontally` and `FlipVertically` by reflecting per-run offsets using `TotalSize` and `run.Size` (`effectiveOffsetX = plan.TotalSize.X - run.Offset.X - run.Size.X`). Preserves explicit `Vector2.Zero` scale.
- **GPU Asset Caching & Bounded Diagnostics**: Caches unavailable secondary font results for initialized sources to prevent per-frame disk scans. Emits clear diagnostics for missing supported-locale fonts. Bounds warning dictionary keys by glyph and locale.

---

## 3. Covered Surfaces
1. **Item Tooltips (`IClickableMenu.drawToolTip`)**: Bilingual title rendered with `dialogueFont` using fallback runs; registered in `BilingualStringTracker`.
2. **Skills Page (`SkillsPage.performHoverAction`)**: Profession titles (levels 5 & 10) and skill titles registered for scoped fallback drawing.
3. **Powers Tab (`PowersTab.performHoverAction`)**: Special items and powers titles; `Game1.parseText` wrapped in `BilingualRenderScope` for run-aware description wrapping.
4. **Collections Page (`CollectionsPage.createDescription`)**: Merged items, achievements, and secret notes registered; descriptions preserve stats, counts, and concealment (`???`).
5. **Collections Mail & Side Tabs (`CollectionsPageHoverPatch`)**: Wrapped mail titles (`Data/mail`) and side tabs (`0..7`) parsed in scope with `Game1.smallFont`.
6. **Secret Note Image Previews (`CollectionsPageDrawPatch`)**: Image note layout calculation and header rendering scoped; header and image box remain strictly disjoint.
7. **Level-Up Menu (`LevelUpMenuPatch`)**: Main profession title (`dialogueFont`) and subtitle (`smallFont`) rendered in scope; supports both primary-first and `SecondaryFirst` order.

---

## 4. Automated Verification & Regression Suite
- **Command**: `dotnet run --project tests/UntranslatedStardew.Tests/UntranslatedStardew.Tests.csproj`
- **Result**: **706 Passed, 0 Failed** (all 605 baseline regressions preserved + 101 font, scope, transform, and acceptance tests).
- **Key Test Areas**:
  - `XnbFontMetadataParser`: Headless binary parsing of uncompressed XNB, LZX compressed XNB (flags `0x01` and `0x81`), HiDef profile, character table extraction (`en`: 266 chars, `vi`: 259 chars, `ja-JP`: 2749 chars, `ru-RU`: Cyrillic, `zh-CN`: Hanzi).
  - `Headless SpriteFont`: Measurement of Vietnamese, Japanese, Russian, and English strings without GPU initialization.
  - `BilingualRunPlanner`: Same-font fast path, NFD to NFC normalization, PrimaryFirst & SecondaryFirst line offsets and total heights, mixed-line segmentation with contiguous offsets, per-line maximum height calculation.
  - `Nested Harmony Fixture`: Real patched nested targets verifying child Postfix+Finalizer exits exactly once without closing parent scope; child exception recovery in parent; idempotent `ScopeCookie.Dispose()`.
  - `Recursion Guards & Unsupported Glyphs`: Glyphs missing in both fonts measured safely without recursion; single warning emission.
  - `NFC Fast-Path`: Decomposed NFD strings with primary precomposed glyphs produce normalized runs; precomposed strings pass through untouched.
  - `Transforms & Block Flipping`: Offset reflection under `FlipHorizontally` and `FlipVertically`; explicit `scale: Vector2.Zero` preservation.
  - `Diagnostics & Lifecycle`: Caching unavailable font assets, clear missing supported-locale font warnings, cache clearing on `Invalidate()`, resource disposal.

---

## 5. Manual Verification Matrix
| Pair | Primary | Secondary | Expected Behavior |
| :--- | :--- | :--- | :--- |
| **`en` -> `vi`** | English (`en`) | Vietnamese (`vi`) | English titles draw with vanilla font; Vietnamese titles draw with `SmallFont_vi`/`SpriteFont1_vi`. Full diacritics displayed without `*`. |
| **`en` -> `ja`** | English (`en`) | Japanese (`ja`) | English primary draws with vanilla font; Japanese secondary draws with `SmallFont.ja-JP`/`SpriteFont1.ja-JP` (Kanji & Kana). |
| **`vi` -> `ru`** | Vietnamese (`vi`) | Russian (`ru`) | Vietnamese primary draws with mod font; Russian secondary draws with `SmallFont.ru-RU`/`SpriteFont1.ru-RU` (Cyrillic). |
| **`ja` -> `en`** | Japanese (`ja`) | English (`en`) | Japanese primary draws with Japanese font; English secondary draws with ASCII glyphs. |
| **Hot Switch** | Any | Switched in GMCM | `Invalidate()` flushes plan cache and secondary fonts; new language assets load immediately without restart. |

---

## 6. Strict Scope Boundaries & Limitations
- **No Ruby Layouts**: Ruby annotations and furigana layouts are excluded.
- **No Dialogue Expansion**: Game dialogue (`SpriteText`/BMFont) is outside current scope and remains untouched.
- **No Independent Size/Color Settings**: Mod does not add user configuration for font size or layer colors.
- **Unsupported External Scripts**: Scripts not present in game content or mod font packs remain logged limitations.
