"""Assert the production health response against a real PostgreSQL dependency."""

import argparse
import json
import time
import urllib.error
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument("url")
parser.add_argument("status", choices=["Healthy", "Unhealthy"])
parser.add_argument("--wait", type=int, default=0)
args = parser.parse_args()
deadline = time.monotonic() + args.wait

while True:
    try:
        try:
            response = urllib.request.urlopen(args.url, timeout=15)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            body = json.load(response)
            expected_code = 200 if args.status == "Healthy" else 503
            assert response.code == expected_code, (response.code, body)
            assert body["status"] == args.status, body
            assert body["checkedAtUtc"] and body["durationMs"] >= 0, body
            checks = body["checks"]
            assert any(check["name"] == "self" for check in checks), body
            dependencies = [check for check in checks if check["name"] != "self"]
            assert dependencies, "No database health check registered"
            assert all(check["status"] == "Healthy" for check in checks if check["name"] == "self"), body
            if args.status == "Healthy":
                assert all(check["status"] == "Healthy" for check in checks), body
            else:
                assert any(check["status"] == "Unhealthy" for check in dependencies), body
            assert all(set(check) == {"name", "status", "durationMs"} for check in checks), body
        print(json.dumps(body))
        break
    except (AssertionError, OSError, ValueError):
        if time.monotonic() >= deadline:
            raise
        time.sleep(1)
