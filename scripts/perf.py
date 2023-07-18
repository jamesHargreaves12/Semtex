#!/usr/bin/env python3.9

import os
from pathlib import Path
from utils import read_summaries
import argparse


def write_snapshot_files(left_path: Path, right_path: Path):
    left_commits = read_summaries(left_path)
    right_commits = read_summaries(right_path)
    left_lookup = {x['CommitHash']:x for x in left_commits}
    right_lookup = {x['CommitHash']:x for x in right_commits}

    common = set(left_lookup.keys()).intersection(right_lookup.keys())
    print(f"Left has {len(left_commits)} commits and {len(common)} of these are common with right")

    left_elapsed = [left_lookup[sha]["ElapsedMilliseconds"] / 1000 for sha in common]
    right_elapsed = [right_lookup[sha]["ElapsedMilliseconds"] / 1000 for sha in common]

    def printStat(name, fn):
        l = fn(left_elapsed)
        r = fn(right_elapsed)
        pct = l/r* 100
        left_str = f"{l:.0f}"
        pct_str = f"{pct:.0f}"
        right_str = f"{r:.0f}"
        col = '\033[92m' if l < r else '\033[91m'
        end_col = '\033[0m'
        print(f"{col} {name:<5} :  {left_str:<5} {right_str:<5} ({pct_str:<3}%){end_col}")

    print(f"Stats ({len(common):.0f} commits) /s")
    printStat("Max", max)
    printStat("Mean", lambda xs: sum(xs)/len(xs))
    printStat("Total", sum)





if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('left')
    parser.add_argument('right')
    args = parser.parse_args()
    write_snapshot_files(Path(os.path.join(os.getcwd(), args.left)), Path(os.path.join(os.getcwd(), args.right)))
