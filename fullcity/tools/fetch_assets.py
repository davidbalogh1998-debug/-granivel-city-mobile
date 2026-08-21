#!/usr/bin/env python3
"""Download and assemble the CC0 asset library for Granivel City.

This script intentionally keeps large third-party binaries out of git history.
It downloads only sources listed in asset-manifest.json and writes provenance
beside the downloaded files. Poly Haven assets are fetched through the public
API with an explicit user-agent and the required 'Powered by Poly Haven' credit
is preserved in the generated provenance file.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "asset-manifest.json"
OUT = ROOT / "vendor-assets"
UA = "GranivelCityAssetBuilder/1.0 (+https://github.com/davidbalogh1998-debug/-granivel-city-mobile)"


def request_json(url: str):
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.load(r)


def download(url: str, dest: Path) -> Path:
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists() and dest.stat().st_size > 0:
        return dest
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=300) as r, open(dest, "wb") as f:
        shutil.copyfileobj(r, f, length=1024 * 1024)
    return dest


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def safe_name(name: str) -> str:
    return "".join(c.lower() if c.isalnum() else "-" for c in name).strip("-")


def extract_zip(z: Path, dest: Path):
    marker = dest / ".extracted"
    if marker.exists():
        return
    dest.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(z) as archive:
        archive.extractall(dest)
    marker.write_text("ok\n")


def walk_files_tree(node, path=()):
    if isinstance(node, dict):
        if "url" in node and "size" in node:
            yield path, node
        for key, value in node.items():
            if isinstance(value, (dict, list)):
                yield from walk_files_tree(value, path + (str(key),))
    elif isinstance(node, list):
        for i, value in enumerate(node):
            yield from walk_files_tree(value, path + (str(i),))


def choose_poly_files(tree, preferred: str, fallback: str):
    wanted_maps = {"diffuse", "nor_gl", "normal", "rough", "displacement", "ao", "arm"}
    candidates = []
    for path, rec in walk_files_tree(tree):
        low = [p.lower() for p in path]
        joined = "/".join(low)
        if not any(m in low or m in joined for m in wanted_maps):
            continue
        if preferred in low:
            score = 0
        elif fallback in low:
            score = 1
        else:
            continue
        url = rec.get("url", "")
        if not url.lower().endswith((".jpg", ".jpeg", ".png", ".exr", ".hdr")):
            continue
        candidates.append((score, path, rec))

    picked = {}
    for score, path, rec in sorted(candidates, key=lambda x: x[0]):
        low = [p.lower() for p in path]
        map_name = next((m for m in wanted_maps if m in low or m in "/".join(low)), None)
        if map_name and map_name not in picked:
            picked[map_name] = (path, rec)
    return list(picked.values())


def fetch_kenney(manifest, provenance):
    base = OUT / "kenney"
    for item in manifest["kenney"]:
        slug = safe_name(item["name"])
        z = download(item["url"], base / "downloads" / f"{slug}.zip")
        extract_zip(z, base / slug)
        provenance.append({
            "name": item["name"], "source": item["source"], "license": item["license"],
            "archive_sha256": sha256(z), "bytes": z.stat().st_size
        })


def ensure_gdown():
    try:
        import gdown  # noqa: F401
        return
    except Exception:
        subprocess.check_call([sys.executable, "-m", "pip", "install", "--quiet", "gdown>=5.2,<6"])


def fetch_quaternius(manifest, provenance):
    ensure_gdown()
    base = OUT / "quaternius"
    base.mkdir(parents=True, exist_ok=True)
    for item in manifest["quaternius"]:
        dest = base / safe_name(item["name"])
        marker = dest / ".downloaded"
        if not marker.exists():
            dest.mkdir(parents=True, exist_ok=True)
            subprocess.check_call([
                sys.executable, "-m", "gdown", "--folder", "--remaining-ok",
                "--output", str(dest), item["folder"]
            ])
            marker.write_text("ok\n")
        total = sum(p.stat().st_size for p in dest.rglob("*") if p.is_file())
        provenance.append({
            "name": item["name"], "source": item["source"], "license": item["license"],
            "bytes": total
        })


def fetch_polyhaven(manifest, provenance, resolution: str):
    ph = manifest["polyHaven"]
    base = OUT / "polyhaven"
    for asset in ph["assets"]:
        tree = request_json(f"{ph['api']}/files/{asset}")
        chosen = choose_poly_files(tree, resolution, ph["fallbackResolution"])
        if not chosen:
            # HDRIs don't use texture map keys. Grab a 4k/8k HDR when available.
            all_files = list(walk_files_tree(tree))
            hdrs = [(p, r) for p, r in all_files if resolution in [x.lower() for x in p] and r.get("url", "").lower().endswith(".hdr")]
            if not hdrs:
                hdrs = [(p, r) for p, r in all_files if ph["fallbackResolution"] in [x.lower() for x in p] and r.get("url", "").lower().endswith(".hdr")]
            chosen = hdrs[:1]
        asset_dir = base / asset
        records = []
        for path, rec in chosen:
            filename = rec["url"].split("/")[-1].split("?")[0]
            fp = download(rec["url"], asset_dir / filename)
            records.append({"file": str(fp.relative_to(ROOT)), "bytes": fp.stat().st_size, "md5": rec.get("md5")})
        provenance.append({
            "name": asset, "source": f"https://polyhaven.com/a/{asset}", "license": "CC0",
            "credit": ph["credit"], "files": records, "bytes": sum(x["bytes"] for x in records)
        })


def write_provenance(provenance):
    OUT.mkdir(parents=True, exist_ok=True)
    total = sum(int(x.get("bytes", 0)) for x in provenance)
    data = {"generated_by": UA, "total_bytes": total, "assets": provenance}
    (OUT / "PROVENANCE.json").write_text(json.dumps(data, indent=2), encoding="utf-8")
    (OUT / "README.md").write_text(
        "# Vendor assets\n\nAll downloaded packs are CC0. Poly Haven assets are obtained using their public API. "
        "**Powered by Poly Haven.**\n\nTotal downloaded bytes: %s\n" % total,
        encoding="utf-8",
    )
    print(f"Asset library ready: {total / (1024**2):.1f} MiB")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--resolution", default="8k", choices=["1k", "2k", "4k", "8k"])
    ap.add_argument("--skip-quaternius", action="store_true")
    ap.add_argument("--clean", action="store_true")
    args = ap.parse_args()
    if args.clean and OUT.exists():
        shutil.rmtree(OUT)
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    provenance = []
    fetch_kenney(manifest, provenance)
    if not args.skip_quaternius:
        fetch_quaternius(manifest, provenance)
    fetch_polyhaven(manifest, provenance, args.resolution)
    write_provenance(provenance)


if __name__ == "__main__":
    main()
