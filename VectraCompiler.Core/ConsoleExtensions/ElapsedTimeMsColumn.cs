using Spectre.Console;
using Spectre.Console.Rendering;

namespace VectraCompiler.Core.ConsoleExtensions;

public class ElapsedTimeMsColumn : ProgressColumn
{
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var elapsed = task.ElapsedTime ?? TimeSpan.Zero;

        var text = elapsed.TotalSeconds switch
        {
            < 1 => $"{elapsed.TotalMilliseconds,6:0} ms",
            < 60 => $"{elapsed:ss}.{elapsed.Milliseconds:000} s",
            _ => $"{elapsed:mm\\:ss}.{elapsed.Milliseconds:000}",
        };

        return new Text(text);
    }
}