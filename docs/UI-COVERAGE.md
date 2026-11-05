# UI Expansion & Coverage Analysis: Untranslated Stardew

- **Document Version**: 1.1.0
- **Game Version Analyzed**: Stardew Valley 1.6.15.24356 (Decompiled source)
- **Target Menus**: `SkillsPage`, `LevelUpMenu`, `PowersTab`, `CollectionsPage`
- **Scope**: Names-only bilingual display for skill names, profession titles, powers/special items, collection entries, and achievements. Descriptions, dialogue, letters, secret notes, numbers, prices, and concealments (`???`) remain untouched in the primary game language.

---

## 1. SMAPI `default` vs `en` Provider Semantics

### SMAPI Fallback Mechanics & Scope Boundaries
- In SMAPI, `TranslationHelper` resolves translation keys by inspecting the active locale file (e.g. `vi.json`), and if not found, falls back to `default.json`.
- `default.json` is the author's primary/fallback translation file in SMAPI—it is **not** universally English for custom mods.
- In vanilla Stardew Valley XNB content, unsuffixed assets (e.g. `Strings/UI.xnb`, `Data/Powers.xnb`) contain the original unlocalized base game content, which happens to be English for Stardew Valley.

### Untranslated Stardew Implementation
1. **Explicit `default` selection**:
   - `SecondaryLanguage: "default"` maps to suffix `null` (pointing directly to base unsuffixed `.xnb` files).
   - `RequestedLanguage` is retained as `"default"`, and `EffectiveLanguage` is `"default"`.
   - No unsupported fallback warning is logged when selecting `"default"`.
   - For mod's own UI strings (GMCM options and log messages), Untranslated Stardew uses SMAPI's native `TranslationHelper.Get(...)`, allowing SMAPI to fall back to `i18n/default.json` naturally.
   - **Scope boundary**: This provides clean base content access for vanilla game tables and Untranslated Stardew's own strings; it does **not** claim or promise that arbitrary third-party mods' `default.json` or custom assets are automatically translated or supported.
2. **Explicit `en` selection**:
   - `SecondaryLanguage: "en"` maps to suffix `null` (loading base vanilla English `.xnb` files).
   - `RequestedLanguage` is `"en"`, and `EffectiveLanguage` is `"en"`.
   - Preserves full backward compatibility with existing user configuration files.
3. **Third-Party / Vietnamese Mod Fallback**:
   - For Vietnamese (`vi`), the mod queries `Kou.ViethoaSV` files (e.g. `Strings/Objects_vi.xnb`). If missing, it logs a one-time message and falls back to `"default"`.
   - No speculative third-party translation injection is performed beyond verified providers.

---

## 2. Rendering & Data Call Chain Investigation

### A. Skills Page (`StardewValley.Menus.SkillsPage`)

#### Source Call Chain & Layout
- **Construction (`SkillsPage` constructor)**:
  - Generates `skillBars` list with 5 vanilla skills: Farming (`0`), Fishing (`1`), Foraging (`2`), Mining (`3`), Combat (`4`), and optionally Luck (`5` if enabled).
  - Calculates fixed vertical slots: each skill row is partitioned into small 68-pixel height rows (`skillBars.Add(new Rectangle(...))`).
  - Sets up `skillAreas` bounding boxes for hover detection (`Rectangle(xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + (i == 4 ? 60 : 0), yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)(i * 56), (i == 4 ? 380 : 320), 56)`).
  - Sets up `professionImageLocations` for unlocked profession icons.
- **Hover Detection (`performHoverAction(int x, int y)`)**:
  - Checks `skillAreas`: when hovering over a skill row, sets:
    - `hoverTitle = parseSkillNumber(i)` (primary skill name).
    - `hoverText = ...` (skill level summary and bonus stats).
  - Checks `professionImageLocations`: when hovering over an unlocked profession icon (level 5 or level 10), sets:
    - `hoverTitle = LevelUpMenu.getProfessionTitle(profession)`
    - `hoverText = LevelUpMenu.getProfessionDescription(profession)`
- **Draw (`draw(SpriteBatch b)`)**:
  - Draws skill names onto the menu surface using `SpriteText.drawString(b, parseSkillNumber(i), ...)` inside fixed 68px row boundaries.
  - At the end of `draw`, delegates to `drawHoverText(b, hoverText, hoverTitle)`.

#### Layout Constraint & Decision
- **On-Page Text Slot**: The on-page skill labels are rendered using `SpriteText` in fixed 68px vertical rows. Forcing two-line bilingual labels (`"Nông nghiệp / Farming"`) causes text overflow, vertical clipping, and overlapping with neighboring skill progress bars on 720p / 1080p resolutions.
- **Safe Solution**: Scoped bilingual hover display. In `performHoverAction`, `___hoverTitle` is intercepted via Harmony postfix and formatted as `Primary (Secondary)` or `Secondary (Primary)` based on `SecondaryFirst`.
- **Primary Body Preservation**: `hoverText` (stat bonuses, skill level, and description) remains entirely in the primary language.

---

### B. Level Up Menu (`StardewValley.Menus.LevelUpMenu`)

#### Source Call Chain & Geometry Evidence
- **Decompiled Source Call Chain (`LevelUpMenu.cs:723-736`)**:
  - Title position: `new Vector2(xPositionOnScreen + spaceToClearSideBorder + 32, yPositionOnScreen + spaceToClearTopBorder + 160)` rendered with `Game1.dialogueFont`.
  - Profession icon: `new Vector2(xPositionOnScreen + spaceToClearSideBorder + width / 2 - 112, yPositionOnScreen + spaceToClearTopBorder + 160 - 16)` with dimensions 64x64px (Y range: `top+144` to `top+208`).
  - First bullet item (i=1): `new Vector2(-4 + xPositionOnScreen + spaceToClearSideBorder + 32, yPositionOnScreen + spaceToClearTopBorder + 128 + 8 + 64 * 2) = top+264`.
  - **Available Vertical Budget**: Exactly `264 - 160 = 104` pixels between the title origin and the first bullet item.
- **Fixed Layout & Scoped Rendering**:
  - Inserting a newline `\n` into `leftProfessionDescription[0]` rendered with `dialogueFont` (~38-42px line height) would overflow the 104px vertical budget and collide directly into the first bullet item.
  - Furthermore, mutating `leftProfessionDescription[0]` permanently leaks bilingual strings across frames into click/hover logic and prevents dynamic config updates.
- **Scoped Solution**:
  1. **Main Title**: Drawn in `Game1.dialogueFont` at `top+160` (vanilla one-line title).
  2. **Subtitle**: Rendered inside the measured gap at `top+208` using `Game1.smallFont` (~24px height).
     - Vertical clearance to first bullet: `264 - 232 = 32px`.
     - Vertical clearance to title bottom: `208 - 198 = 10px`.
  3. **Horizontal Clamping & Truncation**:
     - Left column bounded to `width / 2 - 144` (336px on standard 960px width) before the icon.
     - Right column bounded to `width / 2 - 128` (352px on standard 960px width) before the icon.
     - Pure helper `LevelUpLayoutHelper.TruncateToWidth` clamps text and adds ellipsis `...` if long modded titles exceed column bounds.
  4. **State Management & Exception Safety**:
     - `LevelUpMenuState` stores original list references in `Prefix`.
     - If `SecondaryFirst == true`: index 0 is temporarily swapped with the secondary title for `dialogueFont` rendering, and the primary title is set as the subtitle.
     - If `SecondaryFirst == false`: index 0 is completely untouched throughout `draw`, and the secondary title is set as the subtitle.
     - In `Postfix`, subtitles are drawn with matching hover colors (`leftProfessionColor` / `rightProfessionColor`). In `finally`, `state.Restore()` runs.
     - `Finalizer` guarantees `state.Restore()` runs even if an unhandled exception occurs during `draw`.
     - Zero string leakage, zero list corruption across repeated frames.

---

### C. Powers Tab (`StardewValley.Menus.PowersTab`)

#### Source Call Chain & Layout
- **Data Source**:
  - Stardew Valley 1.6 stores special items and powers in `Data/Powers.xnb`.
  - Each entry has an ID key (e.g. `"SkullKey"`, `"BearPaw"`, `"ForestMagic"`), `DisplayName` (formatted as `[LocalizedText Strings\1_6_Strings:SkullKey_Name]`), and `Description`.
  - `PowersTab` populates `public List<List<ClickableTextureComponent>> powers`.
- **Hover Detection (`performHoverAction(int x, int y)`)**:
  - When mouse is over a power icon:
    - If unlocked: sets `hoverText = power.DisplayName`, `descriptionText = power.Description`.
    - If locked: sets `hoverText = "???"`, `descriptionText = "???"`.
  - Calls `descriptionText = Game1.parseText(descriptionText, Game1.smallFont, width)`.
- **Draw (`draw(SpriteBatch b)`)**:
  - Displays `hoverText` as bold title and `descriptionText` as body in the hover tooltip box.

#### Patch Target & Execution
- **Patch Target**: Harmony postfix on `PowersTab.performHoverAction(int x, int y)`.
- Intercepts `__instance.hoverText`, `__instance.powers`, and `__instance.descriptionText`.
- If `__instance.hoverText == "???"`, the power is locked: **strictly retains `???` without spoiler**.
- Resolves the hovered power key from `c.name`, fetches secondary display name from `IPowerDataProvider`, and formats `__instance.hoverText` bilingually.
- Re-wraps `__instance.descriptionText` with `Game1.parseText` matching tooltip width.
- Description body remains 100% in the primary language.

---

### D. Collections Page (`StardewValley.Menus.CollectionsPage`)

#### Source Call Chain & Layout
- **Tabs**:
  - Tab 0: Shipped Items / Gathering (`0`)
  - Tab 1: Fish (`1`)
  - Tab 2: Artifacts (`2`)
  - Tab 3: Minerals (`3`)
  - Tab 4: Cooking (`4`)
  - Tab 5: Achievements (`5`)
  - Tabs 6-7: Secret Notes and Letters (`6`, `7`) — **strictly out of scope**.
- **Description Creation (`createDescription(string id)`)**:
  - Constructs a combined string:
    ```csharp
    return displayName + Environment.NewLine + Environment.NewLine 
           + Game1.parseText(description, Game1.smallFont, 256) 
           + Environment.NewLine + Environment.NewLine 
           + stats;
    ```
  - For unachieved or undiscovered items, `displayName` is `"???"` and description hints are concealed.
  - For achievements: `displayName` is achievement title; `description` is achievement criteria.
- **Draw (`draw(SpriteBatch b)`)**:
  - Directly calls `drawHoverText(b, hoverText, null)` passing the combined string as `hoverText` (with `hoverTitle = null`).

#### Patch Target & Execution
- **Patch Target**: Harmony postfix on `CollectionsPage.createDescription(string id)`.
- **Delimitation**: Splits result by `Environment.NewLine + Environment.NewLine`.
  - Part 0: Entry Title (`displayName`).
  - Part 1+: Description and statistics (counts, prices, recipes).
- If Part 0 is `"???"` or unearned achievement title is masked: **strictly preserved**.
- Secondary name is resolved:
  - For Tab 5: Resolved via `Data/Achievements` using integer key `id`.
  - For Tabs 0-4: Resolved via `ItemNameResolver.GetSecondaryCollectionItemTitle(id)` or `GetSecondaryNameById(id)`.
- Assembled back with the original `Environment.NewLine + Environment.NewLine` delimiter, keeping primary descriptions, counts, and prices intact.
- Letters and secret notes tabs bypass item formatting entirely.

---

## 3. Harmony Patch Boundary & Signature/Binding Validation

All Harmony patches validate method signatures and private/public target field bindings via reflection during `ModEntry.Entry`:

| Target Class | Member Name | Member Type | Expected Type | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `IClickableMenu` | `drawToolTip` | Method | `(SpriteBatch, string, string, Item, ...)` | Item tooltip bilingual title |
| `SkillsPage` | `performHoverAction` | Method | `(int, int)` | Skill names & profession titles on hover |
| `SkillsPage` | `hoverTitle` | Field (private) | `string` | Hover title buffer for skill tooltips |
| `LevelUpMenu` | `draw` | Method | `(SpriteBatch)` | Profession chooser choices 5/10 |
| `LevelUpMenu` | `leftProfessionDescription` | Field (private) | `List<string>` | Left profession descriptions list |
| `LevelUpMenu` | `rightProfessionDescription` | Field (private) | `List<string>` | Right profession descriptions list |
| `LevelUpMenu` | `professionsToChoose` | Field (private) | `List<int>` | Profession ID choices |
| `LevelUpMenu` | `leftProfessionColor` | Field (private) | `Color` | Hover color binding for left subtitle |
| `LevelUpMenu` | `rightProfessionColor` | Field (private) | `Color` | Hover color binding for right subtitle |
| `PowersTab` | `performHoverAction` | Method | `(int, int)` | Special items & powers bilingual title |
| `PowersTab` | `hoverText` | Field (public) | `string` | Title text buffer for powers |
| `PowersTab` | `descriptionText` | Field (public) | `string` | Description body buffer for powers |
| `PowersTab` | `powers` | Field (public) | `List<List<ClickableTextureComponent>>` | Powers page grid components |
| `CollectionsPage` | `createDescription` | Method | `(string)` | Collection items & achievement titles |

If any method or field changes in a future Stardew Valley update, `ModEntry.ValidatePatchSignatures()` catches the mismatch cleanly, logs an error via `I18n.Log_PatchFailed`, and disables only the affected patch without crashing the game.

---

## 4. Visual & Manual Verification Checklist

### A. Skill Panel (`SkillsPage`)
- [ ] Open game menu (default `E` or `Esc`) and navigate to the Skills tab (farmer icon).
- [ ] Hover over Farming, Mining, Foraging, Fishing, Combat skill rows:
  - Hover title shows `Trồng trọt (Farming)` (or `Farming (Trồng trọt)` when `SecondaryFirst: true`).
  - Hover description shows primary stat bonuses (e.g. `+1 Rìu, +1 Cuốc`).
- [ ] Hover over unlocked level 5 / 10 profession badges:
  - Title shows bilingual profession name (e.g. `Quản trại (Rancher)`).
  - Description shows primary perk text.

### B. Level Up Chooser (`LevelUpMenu`)
- [ ] Trigger level 5 or level 10 overnight level up in any skill.
- [ ] Chooser screen appears with two profession cards:
  - Main title is single-line `dialogueFont` without colliding into bullets.
  - Subtitle is rendered at `top+208` in `smallFont` with 32px clearance before the first bullet.
  - Subtitle color matches title hover transitions (`Game1.textColor` -> `Color.Green`).
  - Long titles clamp smoothly before icon boundaries (336px / 352px) with ellipsis `...`.
  - Closing or clicking either card selects the profession normally without list corruption.

### C. Special Items & Powers (`PowersTab`)
- [ ] Open game menu and navigate to Special Items & Powers tab (star icon).
- [ ] Hover over unlocked powers (e.g. Skull Key, Bear's Knowledge, Club Card):
  - Title shows `Chìa khóa Đầu lâu (Skull Key)`.
  - Body shows primary description with automatic text re-wrapping.
- [ ] Hover over locked powers:
  - Displays `???` without revealing the secondary title or spoiler description.

### D. Collections Tab (`CollectionsPage`)
- [ ] Open Collections menu across tabs:
  - **Shipped**: Hover discovered crops/items: title shows bilingual (e.g. `Củ cải (Parsnip)`), count shipped and price remain primary.
  - **Fish**: Discovered fish show bilingual title (e.g. `Cá Nóc (Pufferfish)`). Undiscovered fish show `???`.
  - **Artifacts & Minerals**: Discovered entries show bilingual title, descriptions remain primary.
  - **Cooking**: Learned recipes show bilingual title; unlearned show `???`.
  - **Achievements**: Completed achievements show bilingual title (e.g. `Lính Mới (15ng) (Greenhorn (15k))`). Criteria remain primary.
  - **Letters & Secret Notes**: Letter and note bodies are unchanged.

### E. Low Resolution & Vietnamese Font Compatibility
- [ ] **Small Screen / Low DPI (720p & 800x600)**:
  - Tooltips expand dynamically using `Game1.dialogueFont.MeasureString`.
  - Tooltip boxes clamp to screen boundaries without overflowing edges.
- [ ] **Vietnamese Diacritics**:
  - Fonts with full Vietnamese glyphs (`Kou.ViethoaSV`) render tones and special characters (`đ`, `ư`, `ơ`, `ắ`, `ệ`) cleanly without square-box missing glyph errors.
