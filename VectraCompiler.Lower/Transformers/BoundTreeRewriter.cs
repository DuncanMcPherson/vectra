using VectraCompiler.Bind.Bodies;
using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Lower.Transformers;

public abstract class BoundTreeRewriter
{
    private readonly DiagnosticBag _diag;

    private SlotAllocator? _allocator;

    private readonly List<BoundStatement> _pendingStatements = new();

    protected BoundTreeRewriter(DiagnosticBag diag)
    {
        _diag = diag;
    }

    protected void EmitPending(BoundStatement statement) => _pendingStatements.Add(statement);

    private List<BoundStatement> FlushPending()
    {
        List<BoundStatement> flushed = [.._pendingStatements];
        _pendingStatements.Clear();
        return flushed;
    }

    public void SetAllocator(SlotAllocator allocator) => _allocator = allocator;

    public virtual BoundStatement RewriteStatement(BoundStatement node)
    {
        return node switch
        {
            BoundBlockStatement block => RewriteBlockStatement(block),
            BoundExpressionStatement expr => RewriteExpressionStatement(expr),
            BoundVariableDeclarationStatement varDecl => RewriteVariableDeclarationStatement(varDecl),
            BoundReturnStatement ret => RewriteReturnStatement(ret),
            _ => (BoundStatement)WriteErrorNode(node)
        };
    }

    protected virtual BoundNode WriteErrorNode(BoundNode node)
    {
        _diag.Error(ErrorCode.UnsupportedStatement, $"Unsupported statement type: {node.GetType().Name}", node.Span);
        return node;
    }

    public virtual BoundBlockStatement RewriteBlockStatement(BoundBlockStatement node)
    {
        var statements = node.Statements;
        List<BoundStatement>? newStatements = null;

        for (var i = 0; i < statements.Count; i++)
        {
            var newStatement = RewriteStatement(statements[i]);
            var pending = FlushPending();

            if (pending.Count > 0 || newStatement != statements[i])
            {
                if (newStatements == null)
                {
                    newStatements = new List<BoundStatement>(statements.Count);
                    for (var j = 0; j < i; j++)
                    {
                        newStatements.Add(statements[j]);
                    }
                }

                newStatements.AddRange(pending);
            }

            newStatements?.Add(newStatement);
        }

        return newStatements == null ? node : new BoundBlockStatement(node.Span, newStatements);
    }

    public virtual BoundExpressionStatement RewriteExpressionStatement(BoundExpressionStatement node)
    {
        var expression = RewriteExpression(node.Expression);
        return expression == node.Expression ? node : new BoundExpressionStatement(node.Span, expression);
    }

    public virtual BoundVariableDeclarationStatement RewriteVariableDeclarationStatement(
        BoundVariableDeclarationStatement node)
    {
        var initializer = node.Initializer != null ? RewriteExpression(node.Initializer) : null;
        return initializer == node.Initializer
            ? node
            : new BoundVariableDeclarationStatement(node.Span, node.Local, initializer);
    }

    public virtual BoundReturnStatement RewriteReturnStatement(BoundReturnStatement node)
    {
        var expression = node.Expression != null ? RewriteExpression(node.Expression) : null;
        return expression == node.Expression ? node : new BoundReturnStatement(node.Span, expression);
    }

    public virtual BoundExpression RewriteExpression(BoundExpression node)
    {
        return node switch
        {
            BoundAssignmentExpression assignment => RewriteAssignmentExpression(assignment),
            BoundBinaryExpression binary => RewriteBinaryExpression(binary),
            BoundCallExpression call => RewriteCallExpression(call),
            BoundLiteralExpression literal => RewriteLiteralExpression(literal),
            BoundLocalExpression local => RewriteLocalExpression(local),
            BoundMemberAccessExpressionReceiver member => RewriteMemberAccessExpression(member),
            BoundNewExpression @new => RewriteNewExpression(@new),
            BoundMethodGroupExpression mg => (BoundExpression)WriteErrorNode(mg),
            _ => node
        };
    }

    public virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
    {
        var left = RewriteExpression(node.Left);
        var right = RewriteExpression(node.Right);

        if (left == node.Left && right == node.Right)
            return node;

        return new BoundBinaryExpression(node.Span, left, node.Operator, right);
    }

    public virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node) => node;
    public virtual BoundExpression RewriteLocalExpression(BoundLocalExpression node) => node;

    public virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
    {
        var target = RewriteExpression(node.Target);
        var value = RewriteExpression(node.Value);

        if (target == node.Target && value == node.Value)
            return node;

        return new BoundAssignmentExpression(node.Span, target, value);
    }

    public virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
    {
        var receiver = node.Receiver != null ? RewriteExpression(node.Receiver) : null;
        var arguments = node.Arguments;
        List<BoundExpression>? newArguments = null;

        for (int i = 0; i < arguments.Count; i++)
        {
            var oldArgument = arguments[i];
            var newArgument = RewriteExpression(oldArgument);

            if (newArgument != oldArgument)
            {
                if (newArguments == null)
                {
                    newArguments = new List<BoundExpression>(arguments.Count);
                    for (int j = 0; j < i; j++)
                        newArguments.Add(arguments[j]);
                }
            }

            if (newArguments != null)
                newArguments.Add(newArgument);
        }
        var finalArgs = newArguments ?? arguments;
        if (receiver is not null)
            finalArgs = finalArgs.Prepend(receiver).ToArray();
        return Equals(finalArgs, arguments) ? node : new BoundCallExpression(node.Span, node.Method, null, finalArgs);
    }

    public virtual BoundExpression RewriteNewExpression(BoundNewExpression node)
    {
        if (_allocator == null)
        {
            _diag.Error(ErrorCode.InternalError, "Slot allocator not set for BoundTreeRewriter");
            return node;
        }

        var arguments = node.Arguments;
        List<BoundExpression>? newArguments = null;

        for (var i = 0; i < arguments.Count; i++)
        {
            var rewritten = RewriteExpression(arguments[i]);
            if (rewritten != arguments[i])
            {
                if (newArguments == null)
                {
                    newArguments = [];
                    for (var j = 0; j < i; j++)
                        newArguments.Add(arguments[j]);
                }
            }

            newArguments?.Add(rewritten);
        }

        var finalArgs = newArguments ?? arguments;
        var tempLocal = new LocalSymbol($"$tmp_{_allocator!.NextSlot}", (NamedTypeSymbol)node.Type)
        {
            SlotIndex = _allocator.Allocate()
        };

        EmitPending(new BoundVariableDeclarationStatement(node.Span, tempLocal, null));
        EmitPending(new BoundObjectAllocationStatement(node.Span, tempLocal, (NamedTypeSymbol)node.Type));
        var thisArg = new BoundLocalExpression(node.Span, tempLocal);
        var ctorArgs = finalArgs.Prepend(thisArg).ToArray();
        EmitPending(new BoundExpressionStatement(node.Span,
            new BoundCallExpression(node.Span, node.Constructor, null, ctorArgs)));
        return new BoundLocalExpression(node.Span, tempLocal);
    }

    public virtual BoundExpression RewriteMemberAccessExpression(BoundMemberAccessExpressionReceiver node)
    {
        var expression = RewriteExpression(node.Receiver);
        if (expression == node.Receiver)
            return node;

        return new BoundMemberAccessExpressionReceiver(node.Span, expression, node.Member);
    }
}