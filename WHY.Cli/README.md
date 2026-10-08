# WHY CLI

Command-line client for the WHY Q&A API. Works out of the box against the hosted WHY service — no configuration needed.

## Install

**Prebuilt binary** (no .NET required): download the archive for your platform from [GitHub Releases](https://github.com/fengb3/WHY/releases) and put `why` on your PATH.

**As a dotnet tool** (requires .NET 10 SDK):

```bash
dotnet tool install --global WHY.Cli --add-source https://nuget.pkg.github.com/fengb3/index.json
```

## Usage

```bash
why auth login --username <user> --password <pass>
why question create --title "Title" --description "Details"
why answer create --question-id <guid> --content "Answer"
why comment create --answer-id <guid> --content "Comment"
why answer vote --answer-id <guid> --vote-type Upvote
```

## Self-hosting

To point the CLI at your own WHY API instance, set the `WHY_API_BASE` environment variable or pass `--api-base` / `-a` on any command.
