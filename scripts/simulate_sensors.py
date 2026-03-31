"""
Post synthetic sensor readings to the gateway (Docker: http://localhost:5000).
Pipeline: gateway -> data service -> outbox -> Kafka -> telemetry ingest -> SignalR -> Blazor.

  pip install -r requirements.txt
  python simulate_sensors.py --base http://localhost:5000
"""

from __future__ import annotations

import argparse
import random
import time
import uuid

import requests


def main() -> None:
    parser = argparse.ArgumentParser(description="Simulate sensors against the gateway")
    parser.add_argument(
        "--base",
        default="http://localhost:5000",
        help="Gateway base URL (no trailing slash)",
    )
    parser.add_argument(
        "--interval",
        type=float,
        default=1.0,
        help="Seconds between samples per device",
    )
    parser.add_argument(
        "--devices",
        type=int,
        default=3,
        help="Number of simulated devices",
    )
    args = parser.parse_args()

    url = f"{args.base.rstrip('/')}/api/sensors/telemetry"
    print(f"Posting to {url} every {args.interval}s for {args.devices} device(s)")

    while True:
        for i in range(args.devices):
            device_id = f"sim-{i + 1}"
            payload = {
                "deviceId": device_id,
                "metric": "temperature",
                "value": round(18.0 + random.random() * 12.0, 2),
                "unit": "C",
                "recordedAt": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                "clientRequestId": str(uuid.uuid4()),
            }
            r = requests.post(url, json=payload, timeout=10)
            try:
                r.raise_for_status()
                print(device_id, r.status_code, r.text[:120])
            except requests.HTTPError as e:
                print(device_id, "HTTP error", e, r.text[:500])

            time.sleep(max(args.interval, 0.2) / max(args.devices, 1))


if __name__ == "__main__":
    main()
