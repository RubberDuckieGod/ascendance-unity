#!/usr/bin/env python3
"""Audit an Ascendance source handoff without executing project or Unity code.

PASS means only that the selected static intake gate passed. Unity compilation,
builds, gameplay, and live multiplayer are never certified by this program.
"""

from __future__ import annotations

import argparse
import ast
from collections import Counter
from datetime import datetime, timezone
import json
from pathlib import Path, PurePosixPath
import re
import sys


EXPECTED_SCENES = (
    ("Assets/Scenes/MainMenu.unity", "7a1e1c2f1b2e4a49a86759007a000701"),
    ("Assets/Scenes/Stonebrook.unity", "7a1e1c2f1b2e4a49a86759007a000702"),
)
REQUIRED_SCRIPTS = (
    "Assets/Scripts/Core/AscendanceGameManager.cs",
    "Assets/Scripts/Online/AscendanceRoomAuthority0100.cs",
    "Assets/Scripts/Online/AscendanceClientCommandRouter0101.cs",
    "Assets/Scripts/Online/AscendanceTcpServer0102.cs",
    "Assets/Scripts/Online/AscendanceTcpCommandTransport0102.cs",
    "Assets/Scripts/Online/AscendanceNetworkProtocol0102.cs",
    "Assets/Editor/AscendanceDedicatedServerBuilder0100.cs",
    "Assets/Editor/AscendanceStandaloneBuilder.cs",
    "Assets/Editor/MasterSaveLoadRegressionTestTools0862.cs",
    "Assets/Editor/DedicatedServerFoundationTestTools0100.cs",
    "Assets/Editor/ClientCommandRoutingTestTools0101.cs",
    "Assets/Editor/NetworkTransportTestTools0102.cs",
)
REQUIRED_DATA = (
    "Assets/Resources/Ascendance/Data/hero_roster.json",
    "Assets/Resources/Ascendance/Data/hero_cards.json",
    "Assets/Resources/Ascendance/Data/full_content_0850.json",
    "Assets/Resources/Ascendance/Data/status_effects.json",
)
DATA_COUNTS = {
    "hero_roster.json": {"heroes": 16},
    "hero_cards.json": {"cards": 323},
    "full_content_0850.json": {
        "equipment": 144, "loot": 190, "searchCards": 100, "encounters": 105,
        "enemies": 125, "locations": 236, "craftingRecipes": 200, "quests": 72, "worldEvents": 24,
    },
    "status_effects.json": {"statuses": 102},
}
REQUIRED_DEPLOYMENT = (
    "Deploy/DigitalOcean/README.md",
    "Deploy/DigitalOcean/run-ascendance.sh",
    "Deploy/DigitalOcean/ascendance.service.example",
    "Deploy/DigitalOcean/release.py",
)
EDITOR_VERSION_PATTERN = r"\d+\.\d+\.\d+[abfp]\d+"
GUID_PATTERN = r"[0-9a-f]{32}"


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def _json(text):
    def reject_constant(value):
        raise ValueError(f"nonstandard JSON constant: {value}")

    return json.loads(text, object_pairs_hook=_unique_object, parse_constant=reject_constant)


def _field(text, name):
    """Read one scalar in these Unity-generated settings, not general YAML."""
    values = re.findall(rf"^[ \t]*{re.escape(name)}:[ \t]*(.*?)[ \t]*$", text, re.M)
    if len(values) != 1 or not values[0]:
        raise ValueError(f"expected exactly one nonempty {name} scalar")
    return values[0]


class Audit:
    def __init__(self, root: Path):
        self.root = root
        self.checks = {"metadata": [], "source": [], "deployment": []}
        self.editor_version = None
        self.bundle_version = None
        self.configured_scenes = []
        self.legacy_verifiers = []

    def record(self, section, name, status, detail, path=None):
        check = {"id": name, "status": status, "detail": detail}
        if path:
            check["path"] = path
        self.checks[section].append(check)

    def read(self, section, rel):
        path = self.root / rel
        if not path.is_file():
            self.record(section, f"file:{rel}", "FAIL" if section == "metadata" else "BLOCKED",
                        "Required file is missing.", rel)
            return None
        try:
            text = path.read_text(encoding="utf-8-sig")
            if not text.strip():
                raise ValueError("file is empty")
        except (OSError, UnicodeError, ValueError) as exc:
            self.record(section, f"file:{rel}", "FAIL", str(exc), rel)
            return None
        self.record(section, f"file:{rel}", "PASS", "Nonempty UTF-8 file is present.", rel)
        return text

    def metadata(self):
        version = self.read("metadata", "ProjectSettings/ProjectVersion.txt")
        if version is not None:
            try:
                self.editor_version = _field(version, "m_EditorVersion")
                revision = _field(version, "m_EditorVersionWithRevision")
                if not re.fullmatch(EDITOR_VERSION_PATTERN, self.editor_version):
                    raise ValueError("invalid Unity Editor version")
                if not re.fullmatch(re.escape(self.editor_version) + r" \([0-9a-f]+\)", revision):
                    raise ValueError("Editor version and revision disagree")
                self.record("metadata", "editor-version", "PASS", revision)
            except ValueError as exc:
                self.record("metadata", "editor-version", "FAIL", str(exc))

        player = self.read("metadata", "ProjectSettings/ProjectSettings.asset")
        if player is not None:
            try:
                if _field(player, "companyName") != "Ascendance" or _field(player, "productName") != "Ascendance":
                    raise ValueError("expected Ascendance company and product names")
                self.bundle_version = _field(player, "bundleVersion")
                if not re.fullmatch(r"\d+\.\d+\.\d+[a-z]?", self.bundle_version):
                    raise ValueError("unsupported or invalid bundleVersion")
                self.record("metadata", "player-identity", "PASS", f"Ascendance {self.bundle_version}")
            except ValueError as exc:
                self.record("metadata", "player-identity", "FAIL", str(exc))

        for rel, field, expected in (
            ("ProjectSettings/EditorSettings.asset", "m_SerializationMode", "2"),
            ("ProjectSettings/VersionControlSettings.asset", "m_Mode", "Visible Meta Files"),
        ):
            text = self.read("metadata", rel)
            if text is not None:
                try:
                    if _field(text, field) != expected:
                        raise ValueError(f"expected {field}: {expected}")
                    self.record("metadata", field, "PASS", expected, rel)
                except ValueError as exc:
                    self.record("metadata", field, "FAIL", str(exc), rel)

        self.build_settings()
        self.packages()
        self.verifiers()

    def build_settings(self):
        rel = "ProjectSettings/EditorBuildSettings.asset"
        text = self.read("metadata", rel)
        if text is None:
            return
        try:
            if text.count("m_Scenes:") != 1:
                raise ValueError("expected one m_Scenes list")
            blocks = re.split(r"^[ \t]*-[ \t]+enabled:[ \t]*", text, flags=re.M)[1:]
            for block in blocks:
                enabled = block.splitlines()[0].strip()
                path, guid = _field(block, "path"), _field(block, "guid")
                if enabled not in ("0", "1") or not re.fullmatch(GUID_PATTERN, guid):
                    raise ValueError("invalid scene enabled flag or GUID")
                if PurePosixPath(path).parts[:1] != ("Assets",) or ".." in PurePosixPath(path).parts:
                    raise ValueError("build scene path must stay inside Assets")
                self.configured_scenes.append({"path": path, "guid": guid, "enabled": enabled == "1"})
            expected = [{"path": path, "guid": guid, "enabled": True} for path, guid in EXPECTED_SCENES]
            if self.configured_scenes != expected:
                raise ValueError("expected enabled MainMenu then Stonebrook with their original scene GUIDs")
            self.record("metadata", "build-scene-settings", "PASS",
                        "Two enabled scene references and original GUIDs match; assets checked separately.", rel)
        except ValueError as exc:
            self.record("metadata", "build-scene-settings", "FAIL", str(exc), rel)

    def packages(self):
        parsed = []
        for rel in ("Packages/manifest.json", "Packages/packages-lock.json"):
            text = self.read("metadata", rel)
            if text is None:
                parsed.append(None)
                continue
            try:
                document = _json(text)
                deps = document.get("dependencies") if isinstance(document, dict) else None
                if not isinstance(deps, dict) or not deps:
                    raise ValueError("dependencies must be a nonempty JSON object")
                parsed.append(deps)
                self.record("metadata", f"json:{rel}", "PASS", f"Parsed {len(deps)} dependencies.", rel)
            except (ValueError, TypeError) as exc:
                parsed.append(None)
                self.record("metadata", f"json:{rel}", "FAIL", str(exc), rel)
        manifest, locked = parsed
        if manifest is None or locked is None:
            return
        try:
            for name, spec in locked.items():
                if not isinstance(spec, dict) or not isinstance(spec.get("version"), str):
                    raise ValueError(f"invalid lock entry for {name}")
                if spec.get("source") not in {"builtin", "registry", "git", "local", "embedded"}:
                    raise ValueError(f"invalid or missing lock source for {name}")
                if type(spec.get("depth")) is not int or spec["depth"] < 0:
                    raise ValueError(f"invalid lock depth for {name}")
                if not isinstance(spec.get("dependencies"), dict):
                    raise ValueError(f"invalid lock dependencies for {name}")
            for name, version in manifest.items():
                if not isinstance(version, str) or not version:
                    raise ValueError(f"invalid manifest version for {name}")
                if name not in locked or locked[name]["version"] != version or locked[name]["depth"] != 0:
                    raise ValueError(f"direct package missing, version mismatch, or wrong lock depth: {name}")
            for name, spec in locked.items():
                for dep, version in spec["dependencies"].items():
                    if dep not in locked or locked[dep]["version"] != version:
                        raise ValueError(f"unresolved or mismatched locked dependency: {name} -> {dep}")
            self.record("metadata", "package-lock-consistency", "PASS",
                        f"{len(manifest)} direct dependencies match; {len(locked)} locked entries resolve. "
                        "No packages were installed or downloaded.")
        except (ValueError, TypeError) as exc:
            self.record("metadata", "package-lock-consistency", "FAIL", str(exc))

    def verifiers(self):
        self.legacy_verifiers = sorted((self.root / "tests").glob("verify_*.py"))
        if not self.legacy_verifiers:
            self.record("metadata", "legacy-verifier-syntax", "FAIL", "No historical verify_*.py files supplied.")
        for path in self.legacy_verifiers:
            rel = path.relative_to(self.root).as_posix()
            try:
                ast.parse(path.read_text(encoding="utf-8-sig"), filename=rel)
                self.record("metadata", f"syntax:{rel}", "PASS", "Python syntax parsed; verifier was not executed.", rel)
            except (OSError, UnicodeError, SyntaxError, ValueError) as exc:
                self.record("metadata", f"syntax:{rel}", "FAIL", str(exc), rel)

    def source(self):
        required_assets = tuple(path for path, _ in EXPECTED_SCENES) + REQUIRED_SCRIPTS + REQUIRED_DATA
        texts = {rel: self.read("source", rel) for rel in required_assets}
        guids = {}
        for rel in required_assets:
            meta = self.read("source", rel + ".meta")
            if meta is not None:
                try:
                    guid = _field(meta, "guid")
                    if not re.fullmatch(GUID_PATTERN, guid):
                        raise ValueError("invalid asset GUID")
                    if guid in guids:
                        raise ValueError(f"duplicate asset GUID also used by {guids[guid]}")
                    guids[guid] = rel
                    self.record("source", f"guid:{rel}", "PASS", guid, rel + ".meta")
                    expected_scene = dict(EXPECTED_SCENES).get(rel)
                    configured = next((s["guid"] for s in self.configured_scenes if s["path"] == rel), None)
                    if expected_scene is not None and (guid != expected_scene or guid != configured):
                        self.record("source", f"scene-guid:{rel}", "FAIL",
                                    "Scene .meta GUID does not match original GUID and EditorBuildSettings.", rel + ".meta")
                    elif expected_scene is not None:
                        self.record("source", f"scene-guid:{rel}", "PASS", "Scene GUID matches its build reference.", rel + ".meta")
                except ValueError as exc:
                    self.record("source", f"guid:{rel}", "FAIL", str(exc), rel + ".meta")

        gm = texts["Assets/Scripts/Core/AscendanceGameManager.cs"]
        if gm is not None:
            versions = re.findall(r'\bconst\s+string\s+BuildVersion\s*=\s*"([^"]+)"', gm)
            ok = len(versions) == 1 and versions[0] == self.bundle_version
            self.record("source", "source-build-version", "PASS" if ok else "FAIL",
                        "BuildVersion matches PlayerSettings." if ok else "BuildVersion is missing or differs from PlayerSettings.")
        for rel in REQUIRED_DATA:
            if texts[rel] is not None:
                try:
                    document = _json(texts[rel])
                    if not isinstance(document, dict):
                        raise ValueError("authored catalog must be a JSON object")
                    expected = DATA_COUNTS[PurePosixPath(rel).name]
                    for key, count in expected.items():
                        if not isinstance(document.get(key), list) or len(document[key]) != count:
                            raise ValueError(f"expected {count} entries in authored catalog {key}")
                    self.record("source", f"json:{rel}", "PASS",
                                "JSON and locked catalog totals match: " + ", ".join(f"{k}={n}" for k, n in expected.items())
                                + ". Gameplay semantics unverified.", rel)
                except (ValueError, TypeError) as exc:
                    self.record("source", f"json:{rel}", "FAIL", str(exc), rel)
        for rel in REQUIRED_DEPLOYMENT:
            self.read("deployment", rel)

    def report(self, mode):
        self.metadata()
        self.source()
        sections = {}
        for section, checks in self.checks.items():
            counts = dict(Counter(check["status"] for check in checks))
            status = "FAIL" if counts.get("FAIL") else "BLOCKED" if counts.get("BLOCKED") else "PASS"
            sections[section] = {"status": status, "counts": counts, "checks": checks}
        sections["source"]["scope"] = "Minimum required handoff files, data JSON, and asset GUIDs; not a complete Unity integrity check."
        sections["deployment"]["scope"] = "Deployment package presence only; excluded from the source gate. No remote checks or changes were made."
        sections["runtime"] = {
            "status": "BLOCKED",
            "reason": "Unity compilation, builds, Play Mode gates, and live multiplayer were not run by this static intake checker.",
            "executed": False,
        }
        passed = sections["metadata"]["status"] == "PASS" and (
            mode == "metadata-only" or sections["source"]["status"] == "PASS"
        )
        return {
            "schema_version": 1,
            "generated_at_utc": datetime.now(timezone.utc).isoformat(),
            "mode": mode,
            "gate_status": "PASS" if passed else "FAIL",
            "gate_scope": "Static metadata only." if mode == "metadata-only" else "Static metadata and minimum source handoff only.",
            "project": {"name": "Ascendance", "editor_version": self.editor_version, "bundle_version": self.bundle_version},
            "inventory": {
                "assets_directory_present": (self.root / "Assets").is_dir(),
                "csharp_files": sum(1 for _ in (self.root / "Assets").rglob("*.cs")),
                "scene_files": sum(1 for _ in (self.root / "Assets").rglob("*.unity")),
                "legacy_verifiers_found": len(self.legacy_verifiers),
                "legacy_verifiers_syntax_passed": sum(c["status"] == "PASS" for c in self.checks["metadata"] if c["id"].startswith("syntax:")),
                "legacy_verifiers_executed": 0,
            },
            "sections": sections,
            "project_code_executed": False,
        }


def inspect_project(root: Path, mode="require-source"):
    return Audit(root.resolve()).report(mode)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group(required=True)
    modes.add_argument("--metadata-only", action="store_true", help="Allow missing source; runtime stays BLOCKED.")
    modes.add_argument("--require-source", action="store_true", help="Fail when the minimum source handoff is incomplete.")
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--report", type=Path, default=Path("docs/INTAKE_STATUS.json"),
                        help="JSON output; relative paths are resolved against --project-root.")
    args = parser.parse_args(argv)
    root = args.project_root.resolve()
    mode = "metadata-only" if args.metadata_only else "require-source"
    report = inspect_project(root, mode)
    target = args.report if args.report.is_absolute() else root / args.report
    try:
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    except OSError as exc:
        parser.exit(2, f"Could not write intake report: {exc}\n")
    for name, section in report["sections"].items():
        print(f"{name.upper()}: {section['status']}")
    print(f"GATE ({mode}): {report['gate_status']}")
    print(f"Report: {target}")
    print("Unity/gameplay/live-network results remain unverified.")
    return 0 if report["gate_status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
