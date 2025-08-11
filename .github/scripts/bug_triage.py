import os
import re
import requests
import json
from openai import OpenAI
from typing import List

# --- Environment variables ---
GITHUB_TOKEN = os.getenv("GITHUB_TOKEN")
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
REPO = os.getenv("GITHUB_REPO")
EVENT_NAME = os.getenv("EVENT_NAME")
ISSUE_NUMBER = os.getenv("ISSUE_NUMBER")

# --- OpenAI client ---
client = OpenAI(api_key=OPENAI_API_KEY)

def get_issues():
    """Fetch issues to triage."""
    if EVENT_NAME == "issues" and ISSUE_NUMBER:
        return [int(ISSUE_NUMBER)]  # Single issue mode
    else:
        url = f"https://api.github.com/repos/{REPO}/issues"
        headers = {"Authorization": f"token {GITHUB_TOKEN}"}
        params = {"state": "open", "labels": ""}
        response = requests.get(url, headers=headers, params=params)
        response.raise_for_status()
        return [
            issue["number"]
            for issue in response.json()
            if "pull_request" not in issue
        ]

def fetch_issue_details(issue_number):
    url = f"https://api.github.com/repos/{REPO}/issues/{issue_number}"
    headers = {"Authorization": f"token {GITHUB_TOKEN}"}
    response = requests.get(url, headers=headers)
    response.raise_for_status()
    return response.json()

def ai_triage(issue) -> dict:
    prompt = f"""
You are an expert bug triage assistant for a software project.

Your task:
Analyze the GitHub issue below and return ONLY a valid JSON object.
Do not include explanations, comments, markdown, or text outside of the JSON.
The JSON must follow this structure exactly:

{{
  "issue_type": "Bug | Feature | Question | Documentation",
  "priority": "Critical | High | Medium | Low",
  "severity": "Blocker | Major | Minor",
  "assignee": "GitHub username or empty string",
  "confidence": 0.0 to 1.0,
  "summary": "One-line summary (max 140 characters)"
}}

---
Title: {issue.get('title', '')}
Body: {issue.get('body', '')}
---
"""

    resp = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.2
    )

    txt = resp.choices[0].message.content.strip()
    parsed = {}

    # Try direct JSON parse
    try:
        parsed = json.loads(txt)
    except json.JSONDecodeError:
        # Try extracting first JSON-like object with regex
        match = re.search(r"\{[\s\S]*\}", txt)
        if match:
            try:
                parsed = json.loads(match.group(0))
            except json.JSONDecodeError:
                parsed = {}
        else:
            parsed = {}

    # Apply defaults if missing
    defaults = {
        "issue_type": "Bug",
        "priority": "Medium",
        "severity": "Minor",
        "assignee": "",
        "confidence": 0.5,
        "summary": (issue.get('title') or "")[:140]
    }
    for key, val in defaults.items():
        if key not in parsed or parsed[key] in (None, ""):
            parsed[key] = val

    return parsed

HEADERS = {
    "Authorization": f"token {GITHUB_TOKEN}",
    "Accept": "application/vnd.github+json",
    "User-Agent": "ai-bug-triage-bot",
}

def github_api(path: str, method="get", json_payload=None, params=None):
    url = f"https://api.github.com/repos/{REPO}{path}"
    if method.lower() == "get":
        r = requests.get(url, headers=HEADERS, params=params)
    elif method.lower() == "post":
        r = requests.post(url, headers=HEADERS, json=json_payload, params=params)
    elif method.lower() == "patch":
        r = requests.patch(url, headers=HEADERS, json=json_payload, params=params)
    else:
        raise ValueError("unsupported method")
    if r.status_code >= 400:
        print("GH API error", r.status_code, r.text)
        r.raise_for_status()
    return r.json()

def fetch_issue(issue_number: int):
    return github_api(f"/issues/{issue_number}")

def add_labels(issue_number: int, labels: List[str]):
    existing = fetch_issue(issue_number).get("labels", [])
    existing_names = [l["name"] for l in existing]
    new = list(set(existing_names + labels))
    github_api(f"/issues/{issue_number}", method="patch", json_payload={"labels": new})

def post_comment(issue_number, comment):
    url = f"https://api.github.com/repos/{REPO}/issues/{issue_number}/comments"
    headers = {"Authorization": f"token {GITHUB_TOKEN}"}
    response = requests.post(url, headers=headers, json={"body": comment})
    response.raise_for_status()

if __name__ == "__main__":
    issues = get_issues()
    for issue_num in issues:
        details = fetch_issue_details(issue_num)
        triage = ai_triage(details)

        issue_type = triage.get("issue_type", "Bug")
        priority = triage.get("priority", "Medium")
        severity = triage.get("severity", "Minor")
        suggested_assignee = triage.get("assignee", "").lstrip("@")
        confidence = float(triage.get("confidence", 0.5))
        summary = triage.get("summary", (details.get("title") or "")[:140])

        comment = f"**AI Triage Suggestion**\n\n{summary}\n\n*(Automated via GitHub Actions)*"
        post_comment(issue_num, comment)
        add_labels(issue_num, [
            issue_type.lower(),
            priority.lower(),
            severity.lower(),
            "triaged",
        ])