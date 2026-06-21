"""JiApp EC2 Starter Lambda — wakes up the EC2 instance on demand."""
import json
import os

import boto3
from botocore.exceptions import ClientError

ec2 = boto3.client("ec2")


def _json_response(status_code, body):
    return {
        "statusCode": status_code,
        "headers": {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
        },
        "body": json.dumps(body),
    }


def _resolve_instance():
    """Return (instance_id, state) for the JiApp server, or (None, None)."""
    env_id = os.environ.get("EC2_INSTANCE_ID")

    # 1) Try the explicit env-var id first, then fall through to tag discovery.
    if env_id:
        try:
            resp = ec2.describe_instances(InstanceIds=[env_id])
            inst = resp["Reservations"][0]["Instances"][0]
            return inst["InstanceId"], inst["State"]["Name"]
        except ClientError:
            pass  # stale / invalid id — try tag discovery below

    # 2) Tag-based discovery.
    resp = ec2.describe_instances(
        Filters=[
            {"Name": "tag:Name", "Values": ["JiApp"]},
            {
                "Name": "instance-state-name",
                "Values": ["pending", "running", "stopping", "stopped"],
            },
        ]
    )
    for reservation in resp.get("Reservations", []):
        for inst in reservation.get("Instances", []):
            return inst["InstanceId"], inst["State"]["Name"]

    return None, None


def handler(event, context):
    try:
        instance_id, state = _resolve_instance()
    except ClientError as exc:
        print(f"Failed to query server state: {exc}")
        return _json_response(503, {"state": "error", "message": "Could not query server state"})

    if instance_id is None:
        return _json_response(503, {"state": "error", "message": "Server instance not found"})

    if state == "running":
        return _json_response(200, {"state": "running", "message": "Server already running"})

    if state == "pending":
        return _json_response(200, {"state": "pending", "message": "Server is starting"})

    try:
        ec2.start_instances(InstanceIds=[instance_id])
    except ClientError as exc:
        print(f"Failed to start server: {exc}")
        return _json_response(503, {"state": "error", "message": "Failed to start server"})

    return _json_response(
        200,
        {"state": "pending", "estimatedWait": 60, "message": "Server is starting"},
    )
