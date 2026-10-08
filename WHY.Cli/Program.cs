using Microsoft.Extensions.DependencyInjection;
using SuperCli;
using WHY.Cli;
using WHY.Cli.Commands;

[assembly: CliDescription("WHY CLI — agent/client for the WHY Q&A API")];

var builder = CliApplication.CreateBuilder();

builder.Services.AddSingleton<TokenStore>();
builder.Services.AddTransient<TokenHandler>();

builder.Services.AddHttpClient("why-api")
    .AddHttpMessageHandler<TokenHandler>();

builder.Services.AddTransient<WhyApiClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var options = sp.GetRequiredService<WhyCliOptions>();
    var httpClient = factory.CreateClient("why-api");
    return new WhyApiClient(httpClient, options);
});

var app = builder.Build();
return await app.RunAsync(args);
