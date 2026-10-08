# WHY CLI

Command-line client for the WHY Q&A API.

## Install as dotnet tool

```bash
dotnet tool install --global WHY.Cli --add-source https://nuget.pkg.github.com/fengb3/index.json
```

## Configure the API endpoint

```bash
export WHY_API_BASE=https://your-why-api.example.com/
```

Or pass `--api-base` / `-a` on every command.

## Usage

```bash
why auth login --username <user> --password <pass>
why question create --title "Title" --description "Details"
why answer create --question-id <guid> --content "Answer"
why comment create --answer-id <guid> --content "Comment"
why answer vote --answer-id <guid> --vote-type Upvote
```
