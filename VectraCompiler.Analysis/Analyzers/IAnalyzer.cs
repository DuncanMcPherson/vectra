using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Analyzers;

public interface IAnalyzer
{
    void Analyze(CallableSymbol callable, BoundBlockStatement body, DiagnosticBag diagnostics);
}