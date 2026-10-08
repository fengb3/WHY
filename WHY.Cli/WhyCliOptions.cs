using SuperCli.Attributes;

namespace WHY.Cli;

[GlobalOptions]
public class WhyCliOptions
{
    [Option("--api-base", ShortName = "-a")]
    public string ApiBase { get; set; } =
        Environment.GetEnvironmentVariable("WHY_API_BASE")
        ?? "https://why-api.duckdns.org:8443/";
}
