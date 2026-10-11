#!/usr/bin/env python3
"""Build, test, and package independently versioned Stardew mods."""

import argparse
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

ROOT = Path(__file__).resolve().parents[1]
VERSION_PATTERN = r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?"


def load_packages():
    packages = json.loads((ROOT / "packages.json").read_text(encoding="utf-8"))
    registered = {package["path"] for package in packages.values()}
    discovered = {
        str(path.parent.relative_to(ROOT))
        for area in ("mods", "patches")
        for path in (ROOT / area).glob("*/manifest.json")
    }
    if registered != discovered:
        raise ValueError(f"packages.json does not match mod directories: {registered ^ discovered}")
    return packages


def metadata(package_id, package):
    source = ROOT / package["path"]
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    version = manifest["Version"]
    if "project" in package:
        project = ET.parse(source / package["project"])
        version = project.findtext(".//Version")
        if manifest["Version"] != "%ProjectVersion%":
            raise ValueError(f"{package_id}: manifest must use %ProjectVersion%")
        if not manifest.get("EntryDll"):
            raise ValueError(f"{package_id}: compiled mod must declare EntryDll")
    elif manifest.get("ContentPackFor", {}).get("UniqueID") != "Pathoschild.ContentPatcher":
        raise ValueError(f"{package_id}: expected a Content Patcher pack")
    if not version or not re.fullmatch(VERSION_PATTERN, version):
        raise ValueError(f"{package_id}: invalid release version {version!r}")
    return {
        "version": version,
        "tag": f"{package_id}/v{version}",
        "archive": f"dist/{package['folder']}-{version}.zip",
    }


def validate(package_id, package):
    info = metadata(package_id, package)
    source = ROOT / package["path"]
    for relative in package["files"]:
        if not (source / relative).exists():
            raise ValueError(f"{package_id}: missing package input {relative}")
    if "tests" in package and not (source / package["tests"]).is_file():
        raise ValueError(f"{package_id}: missing test project")
    if "project" not in package and package["path"].startswith("mods/"):
        content = json.loads((source / "content.json").read_text(encoding="utf-8"))
        if package_id == "immersive-dialogue-ui":
            manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
            if content["Changes"][0]["Entries"]["default"]["packName"] != manifest["UniqueID"]:
                raise ValueError("Immersive Dialogue UI layout must match its manifest ID")
            if not any(
                dependency["UniqueID"] == "Mangupix.DialogueDisplayFrameworkContinued"
                and dependency.get("IsRequired") is True
                for dependency in manifest["Dependencies"]
            ):
                raise ValueError("Immersive Dialogue UI requires DDFC")
    # Patched upstream content uses Content Patcher's JSON-with-comments syntax.
    # Preserve those files verbatim rather than parse them as strict JSON.
    return info


def package_mod(package_id, package):
    info = validate(package_id, package)
    source = ROOT / package["path"]
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    manifest["Version"] = info["version"]
    inputs = {}
    for relative in package["files"]:
        path = source / relative
        files = sorted(path.rglob("*")) if path.is_dir() else [path]
        for file in files:
            if file.is_file() and file.name != ".DS_Store":
                inputs[str(file.relative_to(source))] = file
    if "project" in package:
        framework = ET.parse(source / package["project"]).findtext(".//TargetFramework")
        dll = source / "bin" / "Release" / framework / manifest["EntryDll"]
        if not dll.is_file():
            raise ValueError(f"{package_id}: missing build output {dll}")
        inputs[manifest["EntryDll"]] = dll

    archive = ROOT / info["archive"]
    archive.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(archive, "w", compression=ZIP_DEFLATED) as output:
        output.writestr(
            f"{package['folder']}/manifest.json",
            json.dumps(manifest, indent=2, ensure_ascii=False) + "\n",
        )
        for relative, file in sorted(inputs.items()):
            output.write(file, f"{package['folder']}/{relative}")
    return archive


def build(package_id, package, game_path):
    validate(package_id, package)
    if "project" in package:
        properties = ["-p:EnableModDeploy=false", "-p:EnableModZip=false"]
        if game_path:
            properties.append(f"-p:GamePath={Path(game_path).resolve()}")
        source = ROOT / package["path"]
        subprocess.run(
            ["dotnet", "build", str(source / package["project"]), "-c", "Release", *properties],
            cwd=ROOT,
            check=True,
        )
        subprocess.run(
            ["dotnet", "test", str(source / package["tests"]), "-c", "Release", *properties],
            cwd=ROOT,
            check=True,
        )
    archive = package_mod(package_id, package)
    print(archive.relative_to(ROOT))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("matrix", "validate", "build"))
    parser.add_argument("package", nargs="?", help="package ID; omit to select all")
    parser.add_argument("--game-path", help="game or reference assembly directory")
    args = parser.parse_args()
    packages = load_packages()
    if args.package:
        if args.package not in packages:
            parser.error(f"unknown package: {args.package}")
        packages = {args.package: packages[args.package]}
    for package_id, package in packages.items():
        validate(package_id, package)
    if args.command == "matrix":
        print(json.dumps({"include": [
            {"id": package_id, "compiled": "project" in package}
            for package_id, package in packages.items()
        ]}))
    elif args.command == "build":
        for package_id, package in packages.items():
            build(package_id, package, args.game_path)
    else:
        print(f"Validated {len(packages)} packages")


if __name__ == "__main__":
    main()
