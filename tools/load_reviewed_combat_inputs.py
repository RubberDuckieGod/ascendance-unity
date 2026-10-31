#!/usr/bin/env python3
"""Load one hash-pinned native combat test descriptor; never publish or deploy."""
from __future__ import annotations

import argparse
import hashlib
import hmac
import json
import os
from pathlib import Path
import re
import stat

FIELDS = {
    "release_tag": ("RELEASE_TAG", r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}"),
    "source_revision": ("SOURCE_REVISION", r"[0-9a-f]{40}"),
    "server_archive_sha256": ("SERVER_ARCHIVE_SHA256", r"[0-9a-f]{64}"),
    "server_manifest_sha256": ("SERVER_MANIFEST_SHA256", r"[0-9a-f]{64}"),
    "client_archive_sha256": ("CLIENT_ARCHIVE_SHA256", r"[0-9a-f]{64}"),
    "client_manifest_sha256": ("CLIENT_MANIFEST_SHA256", r"[0-9a-f]{64}"),
}


def unique_fields(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate reviewed combat descriptor field")
        result[key] = value
    return result


def load_reviewed_inputs(path: Path, expected_sha256: str) -> dict[str, str]:
    if not isinstance(expected_sha256, str) or not re.fullmatch(r"[0-9a-f]{64}", expected_sha256):
        raise ValueError("Set the reviewed combat descriptor SHA-256 before running native tests")
    mode = path.lstat().st_mode
    if not stat.S_ISREG(mode) or path.is_symlink() or not 0 < path.stat().st_size <= 16384:
        raise ValueError("Reviewed combat descriptor must be a small regular file")
    content = path.read_bytes()
    if not hmac.compare_digest(hashlib.sha256(content).hexdigest(), expected_sha256):
        raise ValueError("Reviewed combat descriptor SHA-256 differs")
    document = json.loads(content.decode("utf-8"), object_pairs_hook=unique_fields)
    if not isinstance(document, dict) or set(document) != set(FIELDS):
        raise ValueError("Reviewed combat descriptor must contain exactly the six allowlisted test identifiers")
    result = {}
    for field, (variable, pattern) in FIELDS.items():
        value = document[field]
        if not isinstance(value, str) or not re.fullmatch(pattern, value):
            raise ValueError("Invalid reviewed combat identifier: " + field)
        result[variable] = value
    return result


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    args = parser.parse_args(argv)
    try:
        identifiers = load_reviewed_inputs(args.input, args.sha256)
        environment_file = os.environ.get("GITHUB_ENV")
        if not environment_file:
            raise ValueError("GitHub Actions environment output is required")
        # Every value has passed a restrictive single-line identifier regex.
        # Field names and the output path cannot be supplied by the descriptor.
        with Path(environment_file).open("a", encoding="utf-8", newline="\n") as output:
            output.writelines(name + "=" + value + "\n" for name, value in identifiers.items())
    except (OSError, ValueError, UnicodeError) as error:
        parser.exit(1, "Reviewed combat inputs refused: " + str(error) + "\n")
    print("Loaded six verified native combat test identifiers; no release or deployment action requested.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
