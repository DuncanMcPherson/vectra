using VectraCompiler.AST;
using VectraCompiler.AST.Lexing;
using VectraCompiler.Package;

namespace VectraCompiler;

internal static class Program
{
    private static void Main(string[] args)
    {
        string? input = null;
        if (args.Length > 0)
            input = args[0];
        var startTime = DateTime.Now;
        var packageData = PackageReader.Read(input);
        var moduleData = packageData.Modules.Select(ModuleReader.Read).ToList();
        foreach (var module in moduleData)
        {
            var files = SourceFileDiscoverer.Discover(module);
            if (files.Count == 0)
                continue;
            Console.WriteLine($"Compiling {files[0]}...");
            var sourceString = File.ReadAllText(files[0].Trim());
            var lexer = new Lexer();
            var tokens = lexer.ReadTokens(sourceString);
            var parser = new Parser(tokens, module);
            var moduleAst = parser.Parse();
            for (var i = 1; i < files.Count; i++)
            {
                var file = files[i];
                sourceString =  File.ReadAllText(file);
                tokens = lexer.ReadTokens(sourceString);
                parser = new Parser(tokens, module);
                var fileAst = parser.Parse();
                moduleAst.InsertSpace(fileAst.Space);
            }
        }
        var endTime = DateTime.Now;
        Console.WriteLine($"Build complete in: {(endTime - startTime).TotalMilliseconds} ms");
    }
}