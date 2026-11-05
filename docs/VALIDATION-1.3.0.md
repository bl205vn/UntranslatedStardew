# Validation — Untranslated Stardew 1.3.0

- Date: 2026-11-06.
- Release build: succeeded, offline (--no-restore), EnableModDeploy=false, EnableModZip=true.
- Regression executable: 885 Passed, 0 Failed. Includes existing regressions, real XNB reads, headless Harmony draw/measure boundaries, role/NFC/wrapping and custom-source lifecycle checks.
- Build warning: NETSDK1138 for the net6.0 test target; no compiler warnings from mod source in the final build.
- git diff --check: clean.
- ZIP: bin/Release/net6.0/UntranslatedStardew 1.3.0.zip.
- ZIP manifest version: 1.3.0. Contains only manifest.json, i18n/default.json, i18n/vi.json, UntranslatedStardew.dll.
- SHA256: BC97E92B5A219AD55B7E82C6F8A67C7BE77AE0CCCB2DA9D98B5BA99C2A48EED5
- No game deployment, installation, commit, push, tag or publication performed.
- AGY supplied research and initial implementation. Follow-up failed with Windows socket bind access errors; Codex completed and independently verified the remaining fixes.
- In-game rendering, GPU font loading and visual coexistence with third-party UI/font mods remain manual acceptance steps. See LOCALE-ROLE-FONTS-1.3.0.md.
