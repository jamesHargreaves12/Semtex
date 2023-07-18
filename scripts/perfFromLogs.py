#!/usr/bin/env python3.9
import argparse
import re
from collections import defaultdict
from pathlib import Path


def parse_log(log_path: Path):

    with open(log_path) as fp:
        pattern = r"Performance:\s+(\w+)\s+took\s+(\d+)ms"
        perf_info = defaultdict(list)
        for line in fp.readlines():
            match = re.search(pattern, line)
            if not match:
                continue
            perf_info[match.group(1)].append(int(match.group(2)))

        totals = [(sum(xs), k) for k,xs in perf_info.items()]
        all_up_total = sum(x for x,_ in totals)
        for tot, k in sorted(totals, reverse=True):
            print(f"{k} {int(tot/all_up_total*100)}% {tot}")



if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('log_file')
    args = parser.parse_args()
    parse_log(Path(args.log_file))

