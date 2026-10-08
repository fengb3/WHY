using SuperCli;
using SuperCli.Exceptions;

namespace WHY.Cli.Commands;

public class CommandExceptionFilter : ICommandFilter
{
    public async Task HandleAsync(CommandExecuteContext context, Func<CommandExecuteContext, Task> next)
    {
        try
        {
            await next(context);
        }
        catch (CommandExitException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new CommandExitException(1, $"Unable to reach WHY API: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new CommandExitException(1, $"Unexpected error: {ex.Message}");
        }
    }
}
