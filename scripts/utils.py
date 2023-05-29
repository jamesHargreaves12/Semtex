import json
from pathlib import Path


def read_summaries(filepath: Path):
    commits = []
    with open(filepath, 'r') as json_file:
        for json_str in json_file.readlines():
            commits.append(json.loads(json_str))
    return commits


def flatten(lst):
    return (x for sub in lst for x in sub)

