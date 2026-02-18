using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Lower.Transformers;

public abstract class BoundTreeRewriter
{
    public virtual BoundStatement RewriteStatement(BoundStatement node)
    {
        return node switch
        {
            BoundBlockStatement block => RewriteBlockStatement(block),
            BoundExpressionStatement expr => RewriteExpressionStatement(expr),
            BoundVariableDeclarationStatement varDecl => RewriteVariableDeclarationStatement(varDecl),
            BoundReturnStatement ret => RewriteReturnStatement(ret),
            _ => throw new System.ArgumentException($"Unsupported statement type: {node.GetType().Name}")
        };
    }

    public virtual BoundBlockStatement RewriteBlockStatement(BoundBlockStatement node)
    {
        var statements = node.Statements;
        List<BoundStatement>? newStatements = null;

        for (int i = 0; i < statements.Count; i++)
        {
            var oldStatement = statements[i];
            var newStatement = RewriteStatement(oldStatement);

            if (newStatement != oldStatement)
            {
                if (newStatements == null)
                {
                    newStatements = new List<BoundStatement>(statements.Count);
                    for (int j = 0; j < i; j++)
                        newStatements.Add(statements[j]);
                }
            }

            if (newStatements != null)
                newStatements.Add(newStatement);
        }

        return newStatements == null ? node : new BoundBlockStatement(node.Span, newStatements);
    }

    public virtual BoundExpressionStatement RewriteExpressionStatement(BoundExpressionStatement node)
    {
        var expression = RewriteExpression(node.Expression);
        return expression == node.Expression ? node : new BoundExpressionStatement(node.Span, expression);
    }

    public virtual BoundVariableDeclarationStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
    {
        var initializer = node.Initializer != null ? RewriteExpression(node.Initializer) : null;
        return initializer == node.Initializer ? node : new BoundVariableDeclarationStatement(node.Span, node.Local, initializer);
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
            BoundBinaryExpression binary => RewriteBinaryExpression(binary),
            BoundLiteralExpression literal => RewriteLiteralExpression(literal),
            BoundLocalExpression local => RewriteLocalExpression(local),
            BoundAssignmentExpression assignment => RewriteAssignmentExpression(assignment),
            BoundCallExpression call => RewriteCallExpression(call),
            BoundNewExpression @new => RewriteNewExpression(@new),
            BoundMemberAccessExpressionReceiver member => RewriteMemberAccessExpression(member),
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

        if (receiver == node.Receiver && newArguments == null)
            return node;

        return new BoundCallExpression(node.Span, node.Method, receiver, newArguments ?? arguments);
    }

    public virtual BoundExpression RewriteNewExpression(BoundNewExpression node)
    {
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

        if (newArguments == null)
            return node;

        return new BoundNewExpression(node.Span, (NamedTypeSymbol)node.Type, node.Constructor, newArguments);
    }

    public virtual BoundExpression RewriteMemberAccessExpression(BoundMemberAccessExpressionReceiver node)
    {
        var expression = RewriteExpression(node.Receiver);
        if (expression == node.Receiver)
            return node;

        return new BoundMemberAccessExpressionReceiver(node.Span, expression, node.Member);
    }
}
