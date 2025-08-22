import os
import re
import requests
import json
from typing import List

# --- Environment variables ---
GITHUB_TOKEN = os.getenv("GITHUB_TOKEN")
SAIA_API_KEY = os.getenv("GEAI_API_KEY")
REPO = os.getenv("GITHUB_REPO")
EVENT_NAME = os.getenv("EVENT_NAME")
ISSUE_NUMBER = os.getenv("ISSUE_NUMBER")

# --- SAIA API configuration ---
SAIA_API_URL = "https://saiapi.corp.globant.com/chat"

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
    # Prepare content with just title and body
    content = f"Title: {issue.get('title', '')}\nBody: {issue.get('body', '')}"
    
    # SAIA API headers
    headers = {
        "Authorization": f"Bearer {SAIA_API_KEY}",
        "Content-Type": "application/json"
    }
    
    # SAIA API payload
    payload = {
        "model": "saia:assistant:Product-Roadmap-Assistant",
        "messages": [{
            "role": "user",
            "content": content
        }],
        "stream": False
    }
    
    # Make request to SAIA API with error handling
    try:
        response = requests.post(SAIA_API_URL, headers=headers, json=payload, timeout=30)
        response.raise_for_status()
        
        # Extract response content
        response_data = response.json()
        txt = response_data['choices'][0]['message']['content'].strip()
        
    except requests.exceptions.RequestException as e:
        print(f"SAIA API Error: {e}")
        # Return defaults on API failure
        return {
            "issue_type": "Bug",
            "priority": "Medium",
            "severity": "Minor",
            "assignee": "",
            "confidence": 0.5,
            "summary": (issue.get('title') or "")[:140]
        }
    except (KeyError, IndexError) as e:
        print(f"SAIA API Response parsing error: {e}")
        # Return defaults on response parsing failure
        return {
            "issue_type": "Bug",
            "priority": "Medium",
            "severity": "Minor",
            "assignee": "",
            "confidence": 0.5,
            "summary": (issue.get('title') or "")[:140]
        }
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

    # Apply defaults if missing and map SAIA response format to expected format
    defaults = {
        "issue_type": "Bug",
        "priority": "Medium", 
        "severity": "Minor",
        "assignee": "",
        "confidence": 0.5,
        "summary": (issue.get('title') or "")[:140]
    }
    
    # Map SAIA API response format to expected format
    mapped_response = {}
    
    # Map issuetype to issue_type
    mapped_response["issue_type"] = parsed.get("issuetype", defaults["issue_type"])
    
    # Map priority (P0->Critical, P1->High, P2->Medium, P3->Low)
    priority_mapping = {
        "P0": "Critical",
        "P1": "High", 
        "P2": "Medium",
        "P3": "Low"
    }
    saia_priority = parsed.get("priority", "P2")
    mapped_response["priority"] = priority_mapping.get(saia_priority, defaults["priority"])
    
    # Map severity (keep as is)
    mapped_response["severity"] = parsed.get("severity", defaults["severity"])
    
    # Map confidence
    mapped_response["confidence"] = parsed.get("confidence", defaults["confidence"])
    
    # Map summary
    mapped_response["summary"] = parsed.get("summary", defaults["summary"])[:140]
    
    # Keep assignee as empty (not provided by SAIA API)
    mapped_response["assignee"] = defaults["assignee"]
    
    # Store additional SAIA fields for potential future use
    mapped_response["labels"] = parsed.get("labels", [])
    mapped_response["steps"] = parsed.get("steps", [])

    return mapped_response

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
