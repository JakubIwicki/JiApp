"""JiApp EC2 Starter Lambda — wakes up the EC2 instance on demand."""
import boto3
import json
import os

ec2 = boto3.client("ec2")
INSTANCE_ID = os.environ["EC2_INSTANCE_ID"]


def handler(event, context):
    resp = ec2.describe_instances(InstanceIds=[INSTANCE_ID])
    state = resp["Reservations"][0]["Instances"][0]["State"]["Name"]

    if state == "running":
        return {
            "statusCode": 200,
            "headers": {"Content-Type": "application/json", "Access-Control-Allow-Origin": "*"},
            "body": json.dumps({"state": "running", "message": "Server already running"}),
        }

    if state == "pending":
        return {
            "statusCode": 200,
            "headers": {"Content-Type": "application/json", "Access-Control-Allow-Origin": "*"},
            "body": json.dumps({"state": "pending", "message": "Server is starting"}),
        }

    ec2.start_instances(InstanceIds=[INSTANCE_ID])

    return {
        "statusCode": 200,
        "headers": {"Content-Type": "application/json", "Access-Control-Allow-Origin": "*"},
        "body": json.dumps({"state": "pending", "estimatedWait": 60, "message": "Server is starting"}),
    }
