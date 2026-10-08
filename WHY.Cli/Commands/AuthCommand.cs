using SuperCli;
using SuperCli.Attributes;
using WHY.Shared.Dtos.Users;

namespace WHY.Cli.Commands;

[Command("auth")]
[CommandFilter<CommandExceptionFilter>]
internal partial class AuthCommand
{
    private readonly WhyApiClient _api;
    private readonly TokenStore _tokenStore;

    public AuthCommand(WhyApiClient api, TokenStore tokenStore)
    {
        _api = api;
        _tokenStore = tokenStore;
    }

    [Command("register")]
    public async Task<int> Register(
        string username,
        string password,
        string nickname = "",
        string bio = "")
    {
        var result = await _api.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Password = password,
            Nickname = nickname,
            Bio = bio,
        });

        if (result.StatusCode >= 400 || result.Data?.Token == null)
        {
            await Console.Error.WriteLineAsync($"Registration failed: {result.Message}");
            return 1;
        }

        _tokenStore.SaveToken(result.Data.Token, username);
        Console.WriteLine($"Registered and logged in as '{username}'.");
        return 0;
    }

    [Command("login")]
    public async Task<int> Login(string username, string password)
    {
        var result = await _api.LoginAsync(new LoginUserRequest
        {
            Username = username,
            Password = password,
        });

        if (result.StatusCode >= 400 || result.Data?.Token == null)
        {
            await Console.Error.WriteLineAsync($"Login failed: {result.Message}");
            return 1;
        }

        _tokenStore.SaveToken(result.Data.Token, username);
        Console.WriteLine($"Logged in as '{username}'.");
        return 0;
    }

    [Command("logout")]
    public int Logout()
    {
        _tokenStore.ClearToken();
        Console.WriteLine("Logged out.");
        return 0;
    }

    [Command("me")]
    public int Me()
    {
        if (!_tokenStore.IsLoggedIn)
        {
            Console.Error.WriteLine("Not logged in.");
            return 1;
        }

        Console.WriteLine($"Logged in as '{_tokenStore.GetUsername()}'.");
        return 0;
    }
}
