# Untranslated Stardew - Codebase Audit & Resilience Report (v1.2.0)

**Date**: 2026-11-05  
**Scope**: Focused audit of UI lifetime, state isolation, data boundaries, cache invalidation, external translation lifecycle, thread safety, and modded content extensibility.

---

## 1. Concrete Findings and Resolved Deficiencies

### 1.1 Menu Field Lifetime Restoration on Prefix Failure
- **Finding**: In `LevelUpMenuPatch.Prefix`, `__state` was assigned only at the very end of the method after mutating `___leftProfessionDescription[0]`. If any exception occurred during the right profession resolution or subsequent processing, the catch block logged a warning and exited with `__state == null`. Consequently, neither `Postfix` nor `Finalizer` was able to restore `___leftProfessionDescription[0]`, leaving the game's internal menu state permanently modified across frames.
- **Fix**:
  1. Instantiated `LevelUpMenuState` and assigned `__state = state;` immediately before executing any mutations on internal menu collections.
  2. Added explicit `__state?.Restore(__instance)` in the `Prefix` `catch` block so that any mid-flight exception immediately restores original text before returning to the caller.
  3. Preserved existing idempotent `Restore()` in both `Postfix` and `Finalizer`.

### 1.2 Locked Entries and Spoiler Prevention
- **Finding**: Collections and Powers menus present unrevealed/locked entries (such as unviewed secret notes, uncollected letters, and locked powers) that must remain strictly displayed as `"???"` without leaking secondary or primary names.
- **Verification & Safeguards**:
  1. `PowersTabPatch`: Validates `c.drawShadow` and skips formatting whenever `hoverText == "???"` or `!c.drawShadow`.
  2. `CollectionsPagePatch`: Validates `id != "???"` and `__result != "???"`. For Secret Notes (tab 6), unrevealed notes are bypassed entirely because vanilla sets `hoverText = "???"` without calling `createDescription`.
  3. `CollectionsPageHoverPatch`: For Letters (tab 7), validates `data_split[1] == "True"`, confirms non-empty title, and verifies `primaryTitle != "???"`. Unreceived or concealed entries stay `"???"`.

### 1.3 Skill and Profession Key Mappings
- **Finding**: Audited `SkillProfessionData` mapping table against decompiled Stardew Valley 1.6 binaries (`Farmer.cs` and `SkillsPage.cs`).
- **Result**: Confirmed exact index alignment:
  - Skills: `0` (Farming: `Farmer.cs.1991`), `1` (Fishing: `Farmer.cs.1993`), `2` (Foraging: `Farmer.cs.1994`), `3` (Mining: `Farmer.cs.1992`), `4` (Combat: `Farmer.cs.1996`), `5` (Luck: `Farmer.cs.1995`).
  - Professions: Indices `0` through `29` map 1:1 to vanilla constants (`Rancher` through `Desperado`).
  - Special Powers: Verified 29 vanilla power sources (`SkullKey`, `BearPaw`, `Book_*`, etc.) against `Data/Powers` and `StringsFromCSFiles`/`Objects`/`1_6_Strings`.

### 1.4 Cache Invalidation Coverage
- **Finding**: Previously, `OnAssetsInvalidated` checked only item data and string dictionaries. Mutations or edits to `Data/mail`, `Data/SecretNotes`, `Strings/Locations`, or the new custom dictionary asset `Mods/GiaHung.UntranslatedStardew/Names` did not trigger resolver cache clearance, causing stale bilingual titles.
- **Fix**: Added `Mods/GiaHung.UntranslatedStardew/Names`, `Data/mail`, `Data/SecretNotes`, and `Strings/Locations` to the invalidation filter in `ModEntry.OnAssetsInvalidated`. Re-loads Content Patcher entries and invalidates name resolver caches immediately upon asset change.

### 1.5 Submenu Guard, Unwrapped Title Extraction & Long Title Wrapping
- **Finding**:
  1. In `CollectionsPage.performHoverAction`, hovering while `letterviewerSubMenu` was open could mutate the underlying page's `hoverText`.
  2. When hovering repeatedly over letters, vanilla `Game1.parseText` had already wrapped long primary titles into multiple lines containing `\n`. If `TooltipFormatter.FormatTitle` checked `hoverTitle.Contains('\n')`, long letter titles silently failed to display the secondary title.
- **Fix**:
  1. Added early guard: `if (__instance.letterviewerSubMenu != null) return;`.
  2. Extracted the actual unwrapped primary title directly from `c2.name` metadata (`c2.name.Substring(secondSpace + 1)`).
  3. Parsed primary and secondary titles independently through `Game1.parseText(..., 256)` before joining them with `\n`. Long wrapped primary titles cannot suppress the secondary title.
  4. In `CollectionsPagePatch`, newline delimiter detection dynamically supports both `\r\n\r\n` and `\n\n` across operating systems.

### 1.6 Reconciled SMAPI-Native Locale Fallback
- **Finding**: Previously, `GetCandidateLanguages` skipped `default` when `en` was requested to avoid equating default to English. However, SMAPI `Translator.GetRaw` falls back to `default` for ANY locale including `en`, because `default` is the base dictionary for the mod.
- **Resolution**: Reconciled to exact SMAPI semantics:
  - When requesting any locale (including `en`, `fr-FR`, `zh-CN`), fallback is: `exact locale -> parent locale -> default`.
  - The source base translation in `default` may be non-English (e.g. French or Chinese).
  - Explicit `@en` registrations take precedence over `@default` when `en` is requested.
  - When the user explicitly configures `SecondaryLanguage = "default"`, only `default` is queried; it does NOT fall back to `en`.
  - Non-English languages (e.g. `fr`) do NOT fall back to `en`; they fall back to `default`.

### 1.7 CJB Item Spawner Integration & Zero Global Locale Mutation
- **Finding**: Audited `CjbItemSpawnerIntegration` for isolation and version compatibility.
- **Result**: Reflection is used to detect `CJBItemSpawner.Framework.ItemMenu.SearchItems` dynamically without a compile-time reference. If the class shape differs, the mod logs a single warning and safely disables CJB hooking. No game global locale is ever switched (`LocalizedContentManager.CurrentLanguageCode` is never changed).

### 1.8 External Translation Lifecycle, Cache Synchronization & Thread Safety
- **Finding**:
  1. Token disposal on `RegisterTranslations` removed entries from the registry but left `ItemNameResolver._cache` populated, causing stale names.
  2. If a mod called `RegisterTranslations` multiple times, disposing an older token could wipe out the newer registration.
  3. Concurrent lookups and registrations could encounter race conditions when reading mutable dictionaries without synchronization.
  4. Multi-mod conflict priority was non-deterministic due to arbitrary `ConcurrentDictionary` enumeration.
- **Fix**:
  1. Subscribed `ItemNameResolver` to `ExternalTranslationRegistry.OnChanged`, ensuring token disposal or re-registration immediately purges cached item resolutions.
  2. Added monotonic generation tracking (`RegistrationToken`). Older tokens from prior registrations cannot unregister newer replacement registrations for the same `sourceModId`.
  3. Ordered C# mod registrations by generation descending (`OrderByDescending(e => e.Generation)`). Last registered mod wins conflicts deterministically.
  4. Implemented copy-on-write immutable registration snapshots under a single lock (`_syncLock`). Lookups read atomic snapshot references without locks, guaranteeing complete thread safety with zero lock contention.

### 1.9 API Language Honoring & Isolation
- **Finding**: `api.GetSecondaryName(category, id, language)` honored `language` for external entries, but fell back to the active configured-language resolver for vanilla game data, potentially returning French when Vietnamese was explicitly requested.
- **Fix**:
  1. Integrated dedicated language resolvers via `_getResolverForLanguage` in `UntranslatedStardewApi`.
  2. If an explicit language is requested and no matching resolver is available (e.g. Vietnamese requested without Vietnamese mod installed), it cleanly returns `null` rather than silently returning an unrelated language.
  3. Primary player configuration and active gameplay resolver remain 100% untouched.

---

## 2. Operational Boundaries and Limits

1. **Letter and Note Bodies**: By design, only menu-hover titles for letters, secret notes, journal scraps, and side tabs are translated into bilingual text. Letter and note narrative bodies remain untranslated to preserve authored content and formatting directives.
2. **Virtual Mod Content**: Virtual assets injected solely via runtime C# hooks or raw unmapped XNB files without Content Patcher `EditData` or C# API registration cannot be reverse-guessed. If no translation is registered or found, the mod cleanly falls back to the primary title without mislabeling.
3. **Third-Party Provider Fault Isolation**: Errors thrown by third-party translation providers registered via `IUntranslatedStardewApi` are caught, logged once per mod ID at warning level, and never interrupt game execution or crash menus.
