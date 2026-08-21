#!/usr/bin/env python3
"""Build the large CC0 asset library used by Granivel City.

Large third-party binaries are downloaded during CI instead of being committed
into Git history. Every source is recorded in PROVENANCE.json. A failed mirror
is reported but does not prevent the remaining legal CC0 sources from building.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "asset-manifest.json"
OUT = ROOT / "vendor-assets"
UA = "GranivelCityAssetBuilder/1.1 (+https://github.com/davidbalogh1998-debug/-granivel-city-mobile)"


def request(url: str):
    return urllib.request.Request(url, headers={"User-Agent": UA})


def request_json(url: str):
    with urllib.request.urlopen(request(url), timeout=120) as response:
        return json.load(response)


def download(url: str, dest: Path) -> Path:
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists() and dest.stat().st_size:
        return dest
    with urllib.request.urlopen(request(url), timeout=600) as response, open(dest, "wb") as handle:
        shutil.copyfileobj(response, handle, length=1024 * 1024)
    return dest


def tree_size(path: Path) -> int:
    return sum(p.stat().st_size for p in path.rglob("*") if p.is_file()) if path.exists() else 0


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def safe_name(value: str) -> str:
    return "".join(c.lower() if c.isalnum() else "-" for c in value).strip("-")


def extract_zip(archive_path: Path, dest: Path):
    marker = dest / ".extracted"
    if marker.exists():
        return
    dest.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive_path) as archive:
        archive.extractall(dest)
    marker.write_text("ok\n", encoding="utf-8")


def record_error(provenance, name, source, license_name, exc):
    message = f"{type(exc).__name__}: {exc}"
    print(f"::warning::{name} failed: {message}")
    provenance.append({"name": name, "source": source, "license": license_name, "status": "failed", "error": message, "bytes": 0})


def walk_file_tree(node, path=()):
    if isinstance(node, dict):
        if isinstance(node.get("url"), str):
            yield path, node
        for key, value in node.items():
            if isinstance(value, (dict, list)):
                yield from walk_file_tree(value, path + (str(key),))
    elif isinstance(node, list):
        for index, value in enumerate(node):
            yield from walk_file_tree(value, path + (str(index),))


def choose_poly_files(tree, preferred: str, fallback: str):
    map_aliases = {
        "diffuse": ("diffuse", "albedo"),
        "normal": ("nor_gl", "normal"),
        "rough": ("rough", "roughness"),
        "displacement": ("disp", "displacement"),
        "ao": ("ao", "ambient_occlusion"),
        "arm": ("arm",),
    }
    candidates = []
    for path, rec in walk_file_tree(tree):
        low = [part.lower() for part in path]
        joined = "/".join(low)
        url = rec.get("url", "")
        if not url.lower().split("?")[0].endswith((".jpg", ".jpeg", ".png", ".exr", ".hdr")):
            continue
        resolution = 0 if preferred in low or preferred in joined else 1 if fallback in low or fallback in joined else None
        if resolution is None:
            continue
        for canonical, aliases in map_aliases.items():
            if any(alias in low or alias in joined for alias in aliases):
                candidates.append((resolution, canonical, path, rec))
                break
    selected = {}
    for score, canonical, path, rec in sorted(candidates, key=lambda x: x[0]):
        selected.setdefault(canonical, (path, rec))
    return list(selected.values())


def fetch_kenney(manifest, provenance):
    base = OUT / "kenney"
    for item in manifest["kenney"]:
        slug = safe_name(item["name"])
        try:
            archive_path = download(item["url"], base / "downloads" / f"{slug}.zip")
            dest = base / slug
            extract_zip(archive_path, dest)
            provenance.append({
                "name": item["name"], "source": item["source"], "license": item["license"],
                "status": "ok", "archive_sha256": sha256(archive_path), "bytes": tree_size(dest)
            })
        except Exception as exc:
            record_error(provenance, item["name"], item["source"], item["license"], exc)


def ensure_gdown():
    try:
        import gdown  # noqa: F401
    except Exception:
        subprocess.check_call([sys.executable, "-m", "pip", "install", "--quiet", "gdown>=5.2,<6"])


def fetch_quaternius(manifest, provenance):
    try:
        ensure_gdown()
    except Exception as exc:
        record_error(provenance, "Quaternius downloader", "https://quaternius.com", "CC0-1.0", exc)
        return
    base = OUT / "quaternius"
    base.mkdir(parents=True, exist_ok=True)
    for item in manifest["quaternius"]:
        dest = base / safe_name(item["name"])
        try:
            marker = dest / ".downloaded"
            if not marker.exists():
                dest.mkdir(parents=True, exist_ok=True)
                subprocess.check_call([
                    sys.executable, "-m", "gdown", "--folder", "--remaining-ok",
                    "--output", str(dest), item["folder"]
                ])
                marker.write_text("ok\n", encoding="utf-8")
            provenance.append({
                "name": item["name"], "source": item["source"], "license": item["license"],
                "status": "ok", "bytes": tree_size(dest)
            })
        except Exception as exc:
            record_error(provenance, item["name"], item["source"], item["license"], exc)


def fetch_polyhaven(manifest, provenance, resolution: str):
    ph = manifest["polyHaven"]
    base = OUT / "polyhaven"
    for asset in ph["assets"]:
        source = f"https://polyhaven.com/a/{asset}"
        try:
            tree = request_json(f"{ph['api']}/files/{asset}")
            selected = choose_poly_files(tree, resolution, ph["fallbackResolution"])
            if not selected:
                hdrs = []
                for path, rec in walk_file_tree(tree):
                    low = [x.lower() for x in path]
                    url = rec.get("url", "")
                    if url.lower().split("?")[0].endswith(".hdr") and (resolution in low or ph["fallbackResolution"] in low):
                        hdrs.append((path, rec))
                selected = hdrs[:1]
            if not selected:
                raise RuntimeError("No matching texture/HDR files returned by Poly Haven API")
            dest = base / asset
            records = []
            for path, rec in selected:
                filename = rec["url"].split("/")[-1].split("?")[0]
                fp = download(rec["url"], dest / filename)
                records.append({"file": str(fp.relative_to(ROOT)), "bytes": fp.stat().st_size, "md5": rec.get("md5")})
            provenance.append({
                "name": asset, "source": source, "license": "CC0", "credit": ph["credit"],
                "status": "ok", "files": records, "bytes": tree_size(dest)
            })
        except Exception as exc:
            record_error(provenance, asset, source, "CC0", exc)


def write_provenance(provenance):
    OUT.mkdir(parents=True, exist_ok=True)
    disk_total = tree_size(OUT)
    successful = sum(1 for item in provenance if item.get("status") == "ok")
    failed = sum(1 for item in provenance if item.get("status") == "failed")
    payload = {
        "generated_by": UA, "disk_total_bytes": disk_total,
        "successful_sources": successful, "failed_sources": failed, "assets": provenance
    }
    (OUT / "PROVENANCE.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    (OUT / "README.md").write_text(
        "# Granivel City vendor assets\n\n"
        "All bundled third-party packs listed here are CC0/public-domain licensed. "
        "Poly Haven content is downloaded via its public API. **Powered by Poly Haven.**\n\n"
        f"Assembled library: {disk_total / (1024 ** 2):.1f} MiB ({successful} successful sources, {failed} failed).\n",
        encoding="utf-8",
    )
    print(f"GRANIVEL_ASSET_BYTES={disk_total}")
    print(f"Asset library ready: {disk_total / (1024 ** 2):.1f} MiB")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--resolution", default="8k", choices=["1k", "2k", "4k", "8k"])
    parser.add_argument("--skip-quaternius", action="store_true")
    parser.add_argument("--clean", action="store_true")
    args = parser.parse_args()
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
