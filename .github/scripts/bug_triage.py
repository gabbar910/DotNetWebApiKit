import os
import requests
from openai import OpenAI

# --- Environment variables ---
GITHUB_TOKEN = os.getenv("GITHUB_TOKEN")
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
REPO = os.getenv("GITHUB_REPO")
EVENT_NAME = os.getenv("EVENT_NAME")
ISSUE_NUMBER = os.getenv("ISSUE_NUMBER")

# --- OpenAI client ---
client = OpenAI(api_key=OPENAI_API_KEY)

# --- GitHub API helpers ---
def github_headers():
    return {"Authorization": f"token {GITHUB_TOKEN}", "Accept": "application/vnd.github.v3+json"}

def get_issues():
    """Fetch issues to triage, skipping already labeled ones."""
    if EVENT_NAME == "issues" and ISSUE_NUMBER:
        return [int(ISSUE_NUMBER)]
    else:
        url = f"https://api.github.com/repos/{REPO}/issues"
        params = {"state": "open", "labels": ""}
        response = requests.get(url, headers=github_headers(), params=params)
        response.raise_for_status()
        return [
            issue["number"]
            for issue in response.json()
            if "pull_request" not in issue and not any(
                lbl["name"].startswith(("type/", "priority/", "severity/")) for lbl in issue["labels"]
            )
        ]

def fetch_issue_details(issue_number):
    url = f"https://api.github.com/repos/{REPO}/issues/{issue_number}"
    response = requests.get(url, headers=github_headers())
    response.raise_for_status()
    return response.json()

def ai_triage(issue):
    prompt = f"""
You are an expert bug triage assistant for a software project.
Analyze the following GitHub issue and return your analysis in JSON format with keys:
- type: Bug, Feature, Question, Documentation
- priority: Critical, High, Medium, Low
- severity: Blocker, Major, Minor
- assignee: GitHub username or "" if none
- summary: One-line summary

Title: {issue['title']}
Body: {issue.get('body', '')}
"""
    resp = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.2
    )

    # Expecting structured JSON
    import json
    try:
        data = json.loads(resp.choices[0].message.content.strip())
    except json.JSONDecodeError:
        raise ValueError("AI did not return valid JSON.")
    return data

def add_labels(issue_number, labels):
    """Add labels to the issue."""
    url = f"https://api.github.com/repos/{REPO}/issues/{issue_number}/labels"
    response = requests.post(url, headers=github_headers(), json={"labels": labels})
    response.raise_for_status()

def assign_issue(issue_number, assignee):
    if assignee:
        url = f"https://api.github.com/repos/{REPO}/issues/{issue_number}"
        response = requests.patch(url, headers=github_headers(), json={"assignees": [assignee]})
        response.raise_for_status()

def post_comment(issue_number, comment):
    url = f"https://api.github.com/repos/{REPO}/issues/{issue_number}/comments"
    response = requests.post(url, headers=github_headers(), json={"body": comment})
    response.raise_for_status()

# --- Main execution ---
if __name__ == "__main__":
    issues = get_issues()
    for issue_num in issues:
        details = fetch_issue_details(issue_num)
        triage = ai_triage(details)

        # Add labels
        labels = [
            f"type/{triage['type'].lower()}",
            f"priority/{triage['priority'].lower()}",
            f"severity/{triage['severity'].lower()}"
        ]
        add_labels(issue_num, labels)

        # Assign if available
        if triage["assignee"]:
            assign_issue(issue_num, triage["assignee"])

        # Post AI triage summary
        comment = (
            f"**AI Triage Suggestion**\n\n"
            f"- **Type:** {triage['type']}\n"
            f"- **Priority:** {triage['priority']}\n"
            f"- **Severity:** {triage['severity']}\n"
            f"- **Assignee:** {triage['assignee'] or 'Unassigned'}\n"
            f"- **Summary:** {triage['summary']}\n\n"
            f"*(Automated via GitHub Actions)*"
        )
        post_comment(issue_num, comment)

