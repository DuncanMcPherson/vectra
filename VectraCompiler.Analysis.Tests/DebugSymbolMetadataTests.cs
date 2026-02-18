using VectraCompiler.AST;
using VectraCompiler.Bind;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Analysis.Tests;

[TestFixture]
public class DebugSymbolMetadataTests
{
    private static readonly string TestFilePath = "test.vec";
    private static readonly string TestModuleName = "TestModule";

    [Test]
    public async Task Binding_Populates_DeclarationSpan_And_FilePath()
    {
        // Setup a simple program
        var source = @"
space Test;
class Program {
    number x;
    void Main(string args) {
        let y = 1;
    }
}";
        var package = await TestHelper.ParsePackage(source, TestFilePath, TestModuleName);
        var bindResult = await BindPhaseRunner.RunInitialBindingAsync(package);
        
        Assert.That(bindResult.Ok, Is.True, "Binding should succeed");
        
        var symbols = bindResult.Value.Declarations.SymbolsByNode.Values.Distinct().ToList();
        foreach (var scope in bindResult.Value.Declarations.TypeMemberScopes.Values)
        {
            symbols.AddRange(scope.AllSymbols());
        }
        symbols = symbols.Distinct().ToList();
        
        // Check Program class
        var programClass = symbols.OfType<NamedTypeSymbol>().First(s => s.Name == "Program");
        Assert.That(programClass.DeclarationSpan, Is.Not.Null);
        Assert.That(programClass.SourceFilePath, Is.EqualTo(TestFilePath));
        
        // Check x field
        var xField = symbols.OfType<FieldSymbol>().FirstOrDefault(s => s.Name == "x");
        if (xField == null)
        {
            var allSymbols = string.Join(", ", symbols.Select(s => $"{s.Kind} {s.Name}"));
            Assert.Fail($"Field 'x' not found. Available symbols: {allSymbols}");
        }
        Assert.That(xField.DeclarationSpan, Is.Not.Null);
        Assert.That(xField.SourceFilePath, Is.EqualTo(TestFilePath));
        
        // Check Main method
        var mainMethod = symbols.OfType<MethodSymbol>().First(s => s.Name == "Main");
        Assert.That(mainMethod.DeclarationSpan, Is.Not.Null);
        Assert.That(mainMethod.SourceFilePath, Is.EqualTo(TestFilePath));
        
        // Check args parameter
        var argsParam = mainMethod.Parameters.FirstOrDefault(p => p.Name == "args");
        if (argsParam == null)
        {
            var allParams = string.Join(", ", mainMethod.Parameters.Select(p => p.Name));
            Assert.Fail($"Parameter 'args' not found in Main. Available parameters: {allParams}");
        }
        Assert.That(argsParam.DeclarationSpan, Is.Not.Null);
        Assert.That(argsParam.SourceFilePath, Is.EqualTo(TestFilePath));
        
        // Check y local variable in body
        var mainBody = bindResult.Value.BodiesByMember[mainMethod];
        var yDecl = mainBody.Statements.OfType<BoundVariableDeclarationStatement>().First();
        var yLocal = yDecl.Local;
        Assert.That(yLocal.Name, Is.EqualTo("y"));
        Assert.That(yLocal.DeclarationSpan, Is.Not.Null);
        Assert.That(yLocal.SourceFilePath, Is.EqualTo(TestFilePath));
    }

    [Test]
    public async Task Binding_Assigns_Sequential_SlotIndices()
    {
        var source = @"
space Test;
class Program {
    void Main(number a, number b) {
        let x = 0;
        let y = 1;
    }
}";
        var package = await TestHelper.ParsePackage(source, TestFilePath, TestModuleName);
        var bindResult = await BindPhaseRunner.RunInitialBindingAsync(package);
        
        Assert.That(bindResult.Ok, Is.True, "Binding should succeed");
        
        var mainMethod = bindResult.Value.BodiesByMember.Keys.OfType<MethodSymbol>().First(s => s.Name == "Main");
        
        // Parameters: this (0), a (1), b (2)
        var thisParam = mainMethod.Parameters.First(p => p.Name == "this");
        var aParam = mainMethod.Parameters.First(p => p.Name == "a");
        var bParam = mainMethod.Parameters.First(p => p.Name == "b");
        
        Assert.That(thisParam.SlotIndex, Is.EqualTo(0));
        Assert.That(aParam.SlotIndex, Is.EqualTo(1));
        Assert.That(bParam.SlotIndex, Is.EqualTo(2));
        
        var mainBody = bindResult.Value.BodiesByMember[mainMethod];
        var locals = mainBody.Statements.OfType<BoundVariableDeclarationStatement>().Select(s => s.Local).ToList();
        
        var xLocal = locals.First(l => l.Name == "x");
        var yLocal = locals.First(l => l.Name == "y");
        
        Assert.That(xLocal.SlotIndex, Is.EqualTo(3));
        Assert.That(yLocal.SlotIndex, Is.EqualTo(4));
    }
}

public static class TestHelper
{
    public static async Task<VectraAstPackage> ParsePackage(string source, string filePath, string moduleName)
    {
        var lexer = new VectraCompiler.AST.Lexing.Lexer();
        var tokens = lexer.ReadTokens(source);
        var parser = new VectraCompiler.AST.Parser(tokens, filePath);
        var parseResult = parser.Parse();
        
        var module = new VectraAstModule { ModuleName = moduleName };
        module.Files.Add(parseResult);
        
        var package = new VectraAstPackage { Name = "TestPackage" };
        package.AddModule(module);
        
        return package;
    }
}