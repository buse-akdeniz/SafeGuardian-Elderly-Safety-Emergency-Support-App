import json
import time
import urllib.request
import urllib.error
import ssl

BASE = "https://safeguardian-elderly-safety-emergency-support-ap-production.up.railway.app"
EMAIL = f"railway.test.{int(time.time())}@example.com"
PASSWORD = "Test123!"
SSL_CONTEXT = ssl._create_unverified_context()


def post(path, payload, token=None):
    data = json.dumps(payload).encode("utf-8")
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(BASE + path, data=data, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=20, context=SSL_CONTEXT) as r:
            return r.getcode(), r.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8")


def get(path, token=None):
    headers = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(BASE + path, headers=headers, method="GET")
    try:
        with urllib.request.urlopen(req, timeout=20, context=SSL_CONTEXT) as r:
            return r.getcode(), r.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8")


print("HEALTH:", *get("/health/live"))

enroll_code, enroll_body = post(
    "/api/elderly-self-enroll",
    {
        "deviceId": f"ios-sim-{int(time.time())}",
        "fullName": "Railway Test User",
        "phone": "+905551234567",
        "email": EMAIL,
        "birthDate": "1948-05-10",
        "bloodType": "A+",
        "password": PASSWORD,
        "emergencyContact1Name": "Test Aile",
        "emergencyContact1Phone": "+905551234568",
    },
)
print("ENROLL:", enroll_code, enroll_body)

login_code, login_body = post(
    "/api/elderly/login",
    {"email": EMAIL, "password": PASSWORD},
)
print("LOGIN:", login_code, login_body)

token = ""
try:
    token = json.loads(login_body).get("token", "")
except Exception:
    token = ""

print("TOKEN_PRESENT:", bool(token))

if token:
    print("SUBSCRIPTION:", *get("/api/subscription", token=token))
    print(
        "EMERGENCY:",
        *post(
            "/api/emergency-alert",
            {
                "location": "41.0082,28.9784",
                "coords": {"latitude": 41.0082, "longitude": 28.9784},
            },
            token=token,
        ),
    )
