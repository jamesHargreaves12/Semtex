#!/usr/bin/env python3.9

import json
import os
from pathlib import Path
from utils import read_summaries
import argparse


def write_snapshot_files(left_path: Path, right_path: Path):
    print(f"{left_path=} {right_path=}")
    left_commits = read_summaries(left_path)
    right_commits = read_summaries(right_path)
    left_shas = set(x['CommitHash'] for x in left_commits)
    common = left_shas.intersection(x['CommitHash'] for x in right_commits)
    print(f"Right has {len(right_commits)} commits and {len(common)} of these are common with left")

    if not common:
        print(f"⚠️ No commits in common so not doing snapshot")
        return

    # assuming that the order of commits on left and right are the same and so we don't need to reorder them to match.
    snapshot_folder = left_path.parent.parent / "Out"/"ForSnapshotting"
    snapshot_folder.mkdir(parents=True, exist_ok=True)

    left_snapshot_path = snapshot_folder / left_path.name
    print(f"writing left result to {left_snapshot_path}")
    with open(left_snapshot_path, "w+") as left_out:
        for commit in left_commits:
            if commit["CommitHash"] in common:
                del commit["ElapsedMilliseconds"]
                json.dump(commit, left_out)
                left_out.write("\n")

    right_snapshot_path = snapshot_folder / right_path.name
    print(f"writing right result to {right_snapshot_path}")
    with open(right_snapshot_path, "w+") as right_out:
        for commit in right_commits:
            if commit["CommitHash"] in common:
                del commit["ElapsedMilliseconds"]
                json.dump(commit, right_out)
                right_out.write("\n")

    if open(left_snapshot_path).read() == open(right_snapshot_path).read():
        lines_checked = len(open(left_snapshot_path).readlines())
        print(f"✅  Success the snapshots match ({lines_checked} commits) ✅  ")
    else:
        print("⚠️  Failure the snapshots do not match  ⚠️  ")  # TODO command to open BCompare for the two files
        for l,r in zip(open(left_snapshot_path).readlines(),open(right_snapshot_path).readlines()):
            if l != r:
                l = json.loads(l)
                print(f"{l['CommitHash']}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('left')
    parser.add_argument('right')
    args = parser.parse_args()
    write_snapshot_files(Path(os.path.join(os.getcwd(), args.left)), Path(os.path.join(os.getcwd(), args.right)))
