# Language source example

Template for pack authors, not a complete language pack. Rename the UniqueID/locale and provide your own assets under assets/Content. Asset names have no .xnb extension in the descriptor. Include Fonts/SmallFont_xx.xnb and Fonts/SpriteFont1_xx.xnb of type SpriteFont. BMFont files are not substitutes. No game or third-party font binaries are included.

The example Strings values demonstrate lookup even before all translated XNB dictionaries are supplied. Without the declared font files the mod logs a missing-font diagnostic and uses its font fallback. Select xx-example as secondary language to inspect those demo names. Do not distribute unchanged as a translation pack.

See ../../docs/LOCALE-ROLE-FONTS-1.3.0.md for the C# API and AdditionalLanguages CustomFields contract.

## Aligned dialogue blocks

A Content Patcher pack can use `EditData` on `Mods/GiaHung.UntranslatedStardew/DialoguePairs` to declare matching blocks for existing NPC dialogue. For example, this entry aligns the two parts of Evelyn's Monday line:

```json
{
  "Action": "EditData",
  "Target": "Mods/GiaHung.UntranslatedStardew/DialoguePairs",
  "Entries": {
    "Characters/Dialogue/Evelyn:Mon": [
      ["Oh my!", "What a lovely day."],
      ["Ôi chao!", "Một ngày đẹp trời."]
    ]
  }
}
```

Each array describes one translation of an existing dialogue segment. List corresponding blocks in the same order. Include both language variants in the entry at the same time. Joining the blocks with one space must reproduce the full segment, with `@` allowed for the player's name. Keep portrait commands, branch commands, and page markers in the original dialogue asset. When a segment changes, update its pairing entry. The mod falls back to the whole translated segment when there is no unique exact match or the separate pairs do not fit.
