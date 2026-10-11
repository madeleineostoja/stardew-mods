#!/usr/bin/env python3
"""Publish one built package without republishing an existing version."""

import argparse
import json
import subprocess
from urllib.parse import quote

from scripts.mods import ROOT, load_packages, metadata


def release_exists(repository, tag):
    result = subprocess.run(
        ["gh", "api", f"repos/{repository}/releases/tags/{quote(tag, safe='')}"],
        capture_output=True,
        text=True,
    )
    if result.returncode == 0:
        release = json.loads(result.stdout)
        if release["draft"]:
            raise ValueError(f"{tag}: a draft release exists; publish or delete it before retrying")
        return True
    if "HTTP 404" in result.stderr:
        return False
    raise RuntimeError(f"Could not check release {tag}: {result.stderr.strip()}")


def publish(package_id, repository, target):
    package = load_packages()[package_id]
    info = metadata(package_id, package)
    if release_exists(repository, info["tag"]):
        print(f"{info['tag']} is already released; leaving it unchanged")
        return
    tag_commit = subprocess.run(
        ["git", "rev-parse", "--verify", f"refs/tags/{info['tag']}^{{commit}}"],
        cwd=ROOT,
        capture_output=True,
        text=True,
    )
    if tag_commit.returncode == 0 and tag_commit.stdout.strip() != target:
        raise ValueError(f"{info['tag']} points at another commit; bump the package version")
    archive = ROOT / info["archive"]
    if not archive.is_file():
        raise ValueError(f"Missing package: {archive}")
    subprocess.run(
        [
            "gh", "release", "create", info["tag"], str(archive),
            "--repo", repository, "--target", target,
            "--title", f"{package['folder']} {info['version']}",
            "--notes", f"Extract {archive.name} into Stardew Valley/Mods. "
            "This archive contains only this mod; install its manifest-listed dependencies separately.",
        ],
        cwd=ROOT,
        check=True,
    )


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package")
    parser.add_argument("--repo", required=True)
    parser.add_argument("--target", required=True)
    args = parser.parse_args()
    publish(args.package, args.repo, args.target)


if __name__ == "__main__":
    main()
