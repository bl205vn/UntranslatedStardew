# Untranslated Stardew - Debug & Implementation Manual (v1.2.1)

**Date**: 2026-11-05  
**Version**: 1.2.1  
**Scope**: Bilingual note/journal image layout overlap fix, safe-area bounds clamping, and dynamic font scaling in LevelUpMenu.

---

## 1. Issue Analysis: Note Image Preview Overlap (Screenshot Case)

### 1.1 User Bug Report
- **Observed Behavior**: In the Collections Page (Tab 6: Secret Notes & Journal Scraps), hovering over Journal Scrap #4 (or image-only Secret Notes like #19, #20, #21, #22, #23, #25) displays a bilingual hover title (Line 1 Vietnamese: *"Mảnh nhật ký vụn #4"*, Line 2 English: *"Journal Scrap #4"*). However, the 288x288 image texture box and the 256x256 secret note preview image are rendered directly over Line 2 ("Journal Scrap #4").
- **Screenshots & Context**: The user reported a screenshot showing the picture frame obscuring the secondary title.

### 1.2 Decompiled Stardew Valley 1.6 Source & IL Analysis
Inspection of `StardewValley.Menus.CollectionsPage.draw` (lines ~868-877) in decompiled source and compiled assembly IL:

```csharp
else if (!this.hoverText.Equals(""))
{
    IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont, 0, 0, this.value);
    if (this.secretNoteImage != -1)
    {
        IClickableMenu.drawTextureBox(b, Game1.getOldMouseX(), Game1.getOldMouseY() + 64 + 32, 288, 288, Color.White);
        b.Draw(this.secretNoteImageTexture, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 64 + 32 + 16), new Rectangle(this.secretNoteImage * 64 % this.secretNoteImageTexture.Width, this.secretNoteImage * 64 / this.secretNoteImageTexture.Width * 64, 64, 64), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.865f);
    }
}
```

#### Compiled IL Instructions:
```cil
IL_0310: call Void drawHoverText(...)
IL_0311: ldarg.0 
IL_0312: ldfld Int32 secretNoteImage
IL_0313: ldc.i4.m1 
IL_0314: beq ...
IL_0315: ldarg.1 
IL_0316: call Int32 getOldMouseX()
IL_0317: call Int32 getOldMouseY()
IL_0318: ldc.i4.s 64
IL_0319: add 
IL_0320: ldc.i4.s 32
IL_0321: add 
IL_0322: ldc.i4 288
IL_0323: ldc.i4 288
IL_0324: call Color get_White()
IL_0325: call Void drawTextureBox(...)
IL_0326: ldarg.1 
IL_0327: ldarg.0 
IL_0328: ldfld Texture2D secretNoteImageTexture
IL_0329: call Int32 getOldMouseX()
IL_0330: ldc.i4.s 16
IL_0331: add 
...
IL_0334: ldc.i4.s 64
IL_0335: add 
IL_0336: ldc.i4.s 32
IL_0337: add 
IL_0338: ldc.i4.s 16
IL_0339: add 
...
```

#### Why the bug occurred:
1. `IClickableMenu.drawHoverText` draws a tooltip box starting at `mouseY + 32`. Its height is `Math.Max(60, (int)font.MeasureString(hoverText).Y + 32)`.
2. In vanilla unilingual mode, `hoverText` is a single line, so `font.MeasureString` height is ~28..32px, giving a tooltip box height of 64px (`32 + 32 = 64`). The bottom of the tooltip is at `(mouseY + 32) + 64 = mouseY + 96`.
3. Vanilla hardcoded the texture box at `mouseY + 64 + 32 = mouseY + 96` and the image at `mouseY + 64 + 32 + 16 = mouseY + 112`, assuming the tooltip would never exceed 64px.
4. When Untranslated Stardew injected the secondary title on a second line (`hoverText = "Mảnh nhật ký vụn #4\nJournal Scrap #4"`), `font.MeasureString` height expanded to 64px, and tooltip height grew to `64 + 32 = 96px` (spanning `mouseY + 32` to `mouseY + 128`).
5. Vanilla drew the texture box at `mouseY + 96` and the image at `mouseY + 112`—directly covering the second line of text (which renders between `mouseY + 68` and `mouseY + 104`).
6. Furthermore, vanilla had zero screen boundary clamping on the 288x288 image box; hovering notes near screen bottom or right sent the image off-screen.

---

## 2. Image Layout Fix Architecture

### 2.1 Pure Geometry Helper: `CollectionsNoteImageLayoutHelper`
The calculation is decoupled from MonoGame graphics pipelines into pure mathematics:
- Inputs: `mouseX`, `mouseY`, `hoverText`, `measureString` delegate, `safeArea` rectangle.
- Measures `hoverText` height dynamically:
  ```csharp
  int headerWidth = (int)Math.Ceiling(textWidth) + 36;
  int headerHeight = Math.Max(60, (int)Math.Ceiling(textHeight) + 32);
  int extraHeight = Math.Max(0, headerHeight - VanillaBaselineTooltipHeight);
  int headerX = mouseX + 32;
  int headerY = mouseY + 32;
  int boxX = mouseX;
  int boxY = Math.Max(mouseY + VanillaBaselineBoxYOffset, headerY + headerHeight);
  ```
- **Joint Coordinated Group Clamping**:
  - Treats header and image box as a coordinated group rather than clamping independently. Independent clamping near screen bottom pushed the image box upward while the tooltip remained at cursor position, recreating collisions.
  - When the combined height (`headerHeight + boxHeight <= safeArea.Height`) fits vertically:
    `boxY` and `headerY` shift upward together near screen bottom (`safeArea.Bottom`), ensuring `headerBottom <= boxY` (strictly disjoint).
  - Near right screen edge (`safeArea.Right`), both `headerX` and `boxX` shift left together.
  - If total group height exceeds available safe area height, activates fallback: side-by-side placement or proportional image scaling down to available vertical space.
- Anchors the scaled image inside the box with uniform 16px padding (`imageX = boxX + (boxW - imgW) / 2`, `imageY = boxY + (boxH - imgH) / 2`).
- When `extraHeight == 0` (unilingual / single-line title), coordinates match vanilla geometry (396 / 412) with 0px displacement.

### 2.2 Scoped Replacement Patch: `CollectionsPageDrawPatch`
- **Prefix**: Checks if `currentTab == secretNotesTab`, `letterviewerSubMenu == null`, `secretNoteImage != -1`, and `hoverText` is not empty or `"???"`. If valid, stores `CollectionsNoteImageState` into `__state`, sets `__instance.secretNoteImage = -1;`, and suppresses `___hoverText = "";`. This cleanly suppresses vanilla's hardcoded drawing and uncoordinated `drawHoverText`.
- **Postfix**: Renders the hover header via native `IClickableMenu.drawHoverText` using calculated `overrideX` and `overrideY`, renders `drawTextureBox`, and draws the scaled image. In `finally`, restores both `secretNoteImage` and `hoverText` safely.
- **Finalizer**: In case of drawing exceptions, restores `secretNoteImage` and `hoverText` immediately without swallowing exceptions, preserving game menu state across frames.
- **Scope Restriction**: Only targets note/journal scrap image previews in `CollectionsPage`. Leaves `LetterViewerMenu` (which centers images on the letter paper without titles), story bodies, and regular item tooltips completely untouched.

---

## 3. Dynamic Font Safety in `LevelUpMenu`

### 3.1 Prior Limitation
Previously, `LevelUpLayoutHelper` assumed hardcoded vertical offsets:
- Main title at `top + 160` (`Game1.dialogueFont`, assumed ~48px height)
- Subtitle at `top + 208` (`Game1.smallFont`, assumed ~24px height)
- Bullet 1 at `top + 264` (`Game1.smallFont`)
- Available gap: `264 - 160 = 104px`

When Vietnamese font packs or custom font mods (e.g. `FontSettings`) increased `dialogueFont` height (e.g. 56px), the title extended past `top + 208`, causing the subtitle to overlap the title. If `smallFont` was also enlarged (e.g. 36px+), subtitle text collided with bullet 1.

### 3.2 Dynamic Font Solution
1. **Frame-by-Frame Measurement**:
   In `LevelUpMenuState.DrawSubtitles`, actual heights are queried from `Game1.dialogueFont` and `Game1.smallFont` on each frame:
   ```csharp
   leftTitleH = Game1.dialogueFont.MeasureString(LeftList[0]).Y;
   leftSubH = Game1.smallFont.MeasureString(SubtitleLeft).Y;
   ```
2. **Dynamic Subtitle Y**:
   Subtitle top coordinate is dynamically anchored to clear the main title:
   ```csharp
   int minSubtitleY = (int)Math.Ceiling(titleY + titleHeight);
   int subtitleY = Math.Max(defaultSubtitleY, minSubtitleY);
   ```
   If `titleHeight <= 48px`, `subtitleY` remains at vanilla baseline `top + 208`. If title is taller (e.g. 56px), `subtitleY` moves down to `top + 216`, leaving zero overlap.
3. **Bounded Scaling with Explicit Readability Limit**:
   When fonts are too large to fit in the remaining gap before bullet 1 (`top + 264`), the subtitle font is scaled down to fit.
   If available space is mathematically impossible (< 12px or required scale < 0.5f), the subtitle overlay is safely omitted (`LeftVisible = false`) rather than rendering unreadable or colliding text.
4. **Column Independence**:
   Left and right columns are calculated independently, ensuring that a longer title on one side does not unnecessarily push or scale the opposite column.
5. **Exception Guarding**:
   Wrapped in `try/catch` with scoped state restoration (`LevelUpMenuState.Restore()`) in `finally` and `Finalizer`, preserving input and draw states even under unexpected rendering errors.

---

## 4. Verification & Testing Evidence

### 4.1 Automated Regression Suite
Run command:
```pwsh
dotnet run --project tests/UntranslatedStardew.Tests/UntranslatedStardew.Tests.csproj
```
Results: **605 Passed, 0 Failed**.
- **All 497 prior tests pass** without modification.
- **108 new assertions** verify:
  1. Reflection & IL match of vanilla `CollectionsPage.draw` fixed offsets.
  2. Unchanged single-title geometry and baseline coordinates.
  3. Multiline bilingual titles (en/vi) and enlarged fonts moving box and image by identical delta.
  4. Coordinated group safe area edge clamping (`Right`, `Bottom`, `Top`, `Left`), guaranteeing header and box remain strictly disjoint (`headerBottom <= boxY`).
  5. Reduced available height fallback (side-by-side or proportional image scale).
  6. Hidden `"???"`, other tabs, open letter submenu, and no-image guards remaining untouched.
  7. Scoped state suppression, idempotent restoration, and finalizer exception pass-through.
  8. Dynamic font scaling, clearance, impossible space overlay omission (< 12px / < 0.5f), and independent columns.

### 4.2 Packaging & Distribution
Offline release build:
```pwsh
dotnet build -c Release /p:EnableModDeploy=false /p:EnableModZip=true
```
Generates clean ZIP package: `bin/Release/net6.0/UntranslatedStardew 1.2.1.zip` containing strictly:
- `manifest.json` (version 1.2.1)
- `UntranslatedStardew.dll`
- `i18n/default.json` and `i18n/vi.json`
- Zero test binaries, example packs, or unnecessary assemblies.
