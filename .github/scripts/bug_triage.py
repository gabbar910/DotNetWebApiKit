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

def get_issues():
    """Fetch issues to triage."""
    if EVENT_NAME == "issues" and ISSUE_NUMBER:
        # Real-time mode: single issue
        return [int(ISSUE_NUMBER)]
    else:
        # Daily batch mode: all open, unlabeled issues
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

def ai_triage(issue):
    prompt = f"""
You are an expert bug triage assistant for a software project.
Analyze the following GitHub issue and provide:
1. Issue type: Bug, Feature, Question, Documentation
2. Priority: Critical, High, Medium, Low
3. Severity: Blocker, Major, Minor
4. Suggested Assignee (if identifiable from context or leave blank)
5. One-line summary
---
Title: {issue['title']}
Body: {issue.get('body', '')}
"""
    # New API style
    resp = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.2
    )

    return resp.choices[0].message.content.strip()

def post_comment(issue_number, comment):
    url = f"https://api.github.com/repos/{REPO}/issues/{issue_number}/comments"
    headers = {"Authorization": f"token {GITHUB_TOKEN}"}
    response = requests.post(url, headers=headers, json={"body": comment})
    response.raise_for_status()

if __name__ == "__main__":
    issues = get_issues()
    for issue_num in issues:
        details = fetch_issue_details(issue_num)
        analysis = ai_triage(details)
        comment = f" **AI Triage Suggestion**\n\n{analysis}\n\n*(Automated via GitHub Actions)*"
        post_comment(issue_num, comment)
