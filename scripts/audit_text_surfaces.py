#!/usr/bin/env python3
"""
scripts/audit_text_surfaces.py

Reproducible lexical callsite inventory scanner for Stardew Valley decompiled C# source.
Scans call families associated with text rendering, dialogue, tooltips, questions,
and font usage across the decompiled codebase.

No external dependencies (Python 3 standard library only).
"""

import os
import sys
import re
import json
import time
import argparse
from pathlib import Path


# Regex patterns for C# syntax structure
RE_CLASS = re.compile(r"\b(?:class|struct|interface|record)\s+([A-Za-z0-9_]+)")
RE_METHOD = re.compile(
    r"^\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|async|unsafe|new)\s+)+[\w<>\[\], ?]+\s+([A-Za-z0-9_]+)\s*\([^;]*$"
)

# Text source / asset key extraction patterns
RE_LOAD_STRING = re.compile(
    r'(?:Game1\.content\.)?LoadString(?:ReturnNullIfNotFound)?\s*\(\s*"([^"]+)"'
)
RE_ASSET_KEY = re.compile(r'"((?:Strings|Data|Characters)[\\/][^"]+)"')
RE_LITERAL_STRING = re.compile(r'"([^"\\]*(?:\\.[^"\\]*)*)"')

# Callsite families to inventory
PATTERNS = {
    "SpriteText.draw": re.compile(
        r"\bSpriteText\.(drawString|drawStringHorizontallyCenteredAt|drawStringWithScrollCenteredAt|drawStringWithScrollBackground|drawSmallTextBubble|drawWithOffset)\s*\("
    ),
    "SpriteBatch.DrawString": re.compile(
        r"(?:\b([a-zA-Z0-9_]+(?:\.[a-zA-Z0-9_]+)?)\.)?DrawString\s*\("
    ),
    "Utility.drawText": re.compile(
        r"\bUtility\.(drawTextWithShadow|drawBoldText|drawTextWithColoredShadow|drawTextWithMinimapBackground)\s*\("
    ),
    "IClickableMenu.hover_tooltip": re.compile(
        r"(?:IClickableMenu\.)?(drawHoverText|drawToolTip)\s*\("
    ),
    "Game1.dialogue": re.compile(
        r"\bGame1\.(drawObjectDialogue|multipleDialogues|drawDialogueNoTyping|drawDialogueBox|drawDialogue|drawObjectQuestionDialogue)\s*\("
    ),
    "GameLocation.createQuestionDialogue": re.compile(
        r"(?:\w+\.)?(createQuestionDialogue|createQuestionDialogueWithCustomWidth)\s*\("
    ),
    "Constructor.Dialogue": re.compile(
        r"\bnew\s+(Dialogue)\s*\("
    ),
    "Constructor.DialogueBox": re.compile(
        r"\bnew\s+(DialogueBox)\s*\("
    ),
    "Constructor.HUDMessage": re.compile(
        r"\bnew\s+(HUDMessage)\s*\("
    ),
    "Character.showTextAboveHead": re.compile(
        r"(?:\w+\.)?(showTextAboveHead)\s*\("
    ),
    "HUDMessage.draw": re.compile(
        r"(?:(?:Game1\.)?hudMessages\[\w+\]\.(draw)|(?:\bmessage|\bmsg|\bhudMessage)\.(draw)\s*\(|HUDMessage\.(draw|drawSubject)|public\s+virtual\s+void\s+(draw)\s*\(\s*SpriteBatch\s+\w+,\s*int\s+\w+,\s*ref\s+int\b)"
    ),
    "Font_family_reference": re.compile(
        r"\bGame1\.(dialogueFont|smallFont|tinyFont|mouseClickPositionFont)\b"
    ),
    "Texture_text_candidate": re.compile(
        r'(?:LooseSprites[\\/](Billboard|font_bold|junimoKart|PrairieKing|titleButtons|Cursors)|Minigames[\\/]|minigames[\\/])',
        re.IGNORECASE
    ),
}


def extract_source_expression(line: str) -> tuple[str | None, str | None]:
    """Extract asset key candidate and primary string argument expression."""
    asset_key = None
    m_load = RE_LOAD_STRING.search(line)
    if m_load:
        asset_key = m_load.group(1).replace("/", "\\")
    else:
        m_asset = RE_ASSET_KEY.search(line)
        if m_asset:
            asset_key = m_asset.group(1).replace("/", "\\")

    text_expr = None
    if asset_key:
        text_expr = f'LoadString("{asset_key}")'
    else:
        m_str = RE_LITERAL_STRING.search(line)
        if m_str and len(m_str.group(1)) > 1:
            text_expr = f'"{m_str.group(1)[:60]}"'
    return asset_key, text_expr


def scan_file(file_path: Path, base_dir: Path) -> list[dict]:
    """Scan a single C# source file and return detected callsites."""
    rel_path = file_path.relative_to(base_dir).as_posix()
    call_sites = []

    current_class = None
    current_method = None
    brace_depth = 0
    class_brace_depth = 0
    method_brace_depth = 0

    try:
        with open(file_path, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
    except Exception as e:
        sys.stderr.write(f"Error reading {file_path}: {e}\n")
        return []

    for line_idx, line in enumerate(lines, 1):
        stripped = line.strip()

        # Update brace depth
        open_braces = stripped.count("{")
        close_braces = stripped.count("}")

        # Check for class declaration
        m_class = RE_CLASS.search(stripped)
        if m_class and not stripped.startswith("//") and not stripped.startswith("*"):
            current_class = m_class.group(1)
            class_brace_depth = brace_depth + (1 if "{" in stripped else 0)

        # Check for method declaration
        m_method = RE_METHOD.search(stripped)
        if m_method and not stripped.startswith("//") and not stripped.startswith("*"):
            current_method = m_method.group(1)
            method_brace_depth = brace_depth + (1 if "{" in stripped else 0)

        # Test against patterns
        for family, pattern in PATTERNS.items():
            match = pattern.search(line)
            if match:
                symbol = match.group(1) if match.lastindex and match.group(1) else match.group(0)
                asset_key, text_expr = extract_source_expression(line)
                snippet = stripped[:140]

                call_sites.append({
                    "family": family,
                    "file": rel_path,
                    "line": line_idx,
                    "class": current_class or "<global>",
                    "method": current_method or "<file_scope>",
                    "symbol": symbol.strip() if symbol else "",
                    "snippet": snippet,
                    "asset_key": asset_key,
                    "text_source_expression": text_expr,
                })

        brace_depth += open_braces - close_braces
        if current_method and brace_depth < method_brace_depth:
            current_method = None
        if current_class and brace_depth < class_brace_depth:
            current_class = None

    return call_sites


def run_audit(decompiled_dir: str, output_file: str, print_summary: bool = True) -> dict:
    """Run audit over all C# files and generate JSON report."""
    base_path = Path(decompiled_dir).resolve()
    if not base_path.exists():
        raise FileNotFoundError(f"Decompiled directory not found: {base_path}")

    t0 = time.time()
    cs_files = sorted(base_path.glob("**/*.cs"))
    total_files = len(cs_files)

    all_sites = []
    family_counts = {fam: 0 for fam in PATTERNS}

    for idx, cs_file in enumerate(cs_files, 1):
        sites = scan_file(cs_file, base_path)
        for s in sites:
            s["kind"] = ("font_reference" if s["family"] == "Font_family_reference" else
                         "texture_reference" if s["family"] == "Texture_text_candidate" else "call_or_declaration_candidate")
            s["id"] = len(all_sites) + 1
            all_sites.append(s)
            family_counts[s["family"]] += 1

    elapsed = time.time() - t0

    result = {
        "metadata": {
            "title": "Stardew Valley 1.6.15 Lexical Text-Surface Callsite Inventory",
            "target_version": "1.6.15",
            "build_number": 24356,
            "scan_timestamp": time.strftime("%Y-%m-%d %H:%M:%S UTC", time.gmtime()),
            "decompiled_root": str(base_path).replace("\\", "/"),
            "total_cs_files_scanned": total_files,
            "total_call_sites_identified": len(all_sites),  # legacy field: lexical matches, not proven invocations
            "unique_source_lines": len({(site["file"], site["line"]) for site in all_sites}),
            "files_with_candidates": len({site["file"] for site in all_sites}),
            "counting_unit": "One match per family per source line; families may overlap; includes declarations and font/texture references.",
            "elapsed_seconds": round(elapsed, 2),
            "family_counts": family_counts,
            "scan_patterns": {k: p.pattern for k, p in PATTERNS.items()},
            "exclusions_and_limitations": [
                "Lexical regex match candidate inventory; does not prove runtime call frequency or reachability.",
                "Third-party libraries (Force.DeepCloner, Ionic.Zlib, LWJGL, Sickhead) are included in scan but rarely contain game text.",
                "Class/method context is heuristic nearest-declaration/brace tracking, not a C# parser; braces in literals and nested types can misattribute context. Verify the source before patching.",
                "Asset/text hints come from the first matching literal on the same line, not resolved argument/data flow; dynamic formats and multiline calls require manual provenance analysis.",
                "Texture candidates are asset references only: fonts and generic atlases do not prove that a texture contains a displayed word. Comments/declarations/string literals can match these lexical patterns."
            ]
        },
        "call_sites": all_sites
    }

    out_path = Path(output_file).resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as fp:
        json.dump(result, fp, indent=2, ensure_ascii=False)

    if print_summary:
        print(f"=== Stardew Valley 1.6.15 Text-Surface Inventory ===")
        print(f"Scanned files : {total_files}")
        print(f"Total sites   : {len(all_sites)}")
        print(f"Time taken    : {elapsed:.2f}s")
        print(f"Output saved  : {out_path}")
        print("\nBreakdown by Family:")
        for fam, count in family_counts.items():
            print(f"  {fam:<36} : {count:>5}")

    return result


def main():
    parser = argparse.ArgumentParser(
        description="Audit visible text surfaces and call sites in decompiled Stardew Valley 1.6.15 source."
    )
    parser.add_argument(
        "--decompiled-dir",
        default="D:/Downloads/Stardew_decompilers",
        help="Path to decompiled Stardew Valley source directory (default: D:/Downloads/Stardew_decompilers)"
    )
    parser.add_argument(
        "--output",
        default="docs/research/text-call-sites-1.6.15.json",
        help="Path to output JSON inventory file (default: docs/research/text-call-sites-1.6.15.json)"
    )
    parser.add_argument(
        "--no-summary",
        action="store_true",
        help="Suppress console summary output"
    )

    args = parser.parse_args()
    run_audit(args.decompiled_dir, args.output, print_summary=not args.no_summary)


if __name__ == "__main__":
    main()
