using SuperCli.Attributes;

namespace WHY.Cli;

[GlobalOptions]
public class WhyCliOptions
{
    [Option("--api-base", ShortName = "-a")]
    public string ApiBase { get; set; } =
        Environment.GetEnvironmentVariable("WHY_API_BASE")
        ?? "http://localhost:5135/";
}
