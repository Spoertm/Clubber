import json
import re
import sys


def flatten(node, prefix=""):
    if isinstance(node, dict):
        for k, v in node.items():
            new_key = f"{prefix}__{k}" if prefix else k
            yield from flatten(v, new_key)
    elif isinstance(node, list):
        for i, v in enumerate(node):
            new_key = f"{prefix}__{i}" if prefix else str(i)
            yield from flatten(v, new_key)
    else:
        value = str(node)
        if re.search(r'[\s"\']', value):
            value = json.dumps(value)
        yield (prefix, value)


if len(sys.argv) < 2:
    print("Usage: python flatten-appsettings.py <path-to-appsettings.json>")
    sys.exit(1)

with open(sys.argv[1], "r") as f:
    data = json.load(f)

for key, value in flatten(data):
    print(f"{key}={value}")
