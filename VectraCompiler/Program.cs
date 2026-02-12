using Spectre.Console.Cli;
using VectraCompiler.CLI;
using VectraCompiler.Core.Logging;

namespace VectraCompiler;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Logger.AddSink(new FileSink());
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("vectra");
            config.AddCommand<BuildCommand>("build")
                .WithDescription("Build a vectra package or module");
            config.AddCommand<ExplainCommand>("explain")
                .WithDescription("Explain a Vectra error code");
            config.AddCommand<VersionCommand>("version")
                .WithAlias("--version")
                .WithAlias("v");
            config.AddCommand<UpdateCommand>("update");
            // TODO: run, test, publish
            config.PropagateExceptions();
        });
        try
        {
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "An error occurred.");
            return 1;
        }
        finally
        {
            Logger.Shutdown();
        }
    }
}
