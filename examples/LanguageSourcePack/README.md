# Language source example

Template for pack authors, not a complete language pack. Rename the UniqueID/locale and provide your own assets under assets/Content. Asset names have no .xnb extension in the descriptor. Include Fonts/SmallFont_xx.xnb and Fonts/SpriteFont1_xx.xnb of type SpriteFont. BMFont files are not substitutes. No game or third-party font binaries are included.

The example Strings values demonstrate lookup even before all translated XNB dictionaries are supplied. Without the declared font files the mod logs a missing-font diagnostic and uses its font fallback. Select xx-example as secondary language to inspect those demo names. Do not distribute unchanged as a translation pack.

See ../../docs/LOCALE-ROLE-FONTS-1.3.0.md for the C# API and AdditionalLanguages CustomFields contract.
