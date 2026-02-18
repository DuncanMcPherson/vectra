using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.AST.Models.Statements;
using VectraCompiler.Bind.Bodies;
using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Bind;

public sealed class BinderService(DeclarationBindResult declarations, DiagnosticBag diagnostics)
{
    private static readonly BoundBinaryOperator[] Ops =
    [
        // int/int
        new(BoundBinaryOperatorKind.Add, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number),
        new(BoundBinaryOperatorKind.Subtract, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number),
        new(BoundBinaryOperatorKind.Multiply, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number),
        new(BoundBinaryOperatorKind.Divide, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number),

        new(BoundBinaryOperatorKind.Equals, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.NotEquals, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.Less, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.LessOrEqual, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.Greater, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.GreaterOrEqual, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Bool),

        // bool/bool
        new(BoundBinaryOperatorKind.Equals, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.NotEquals, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.LogicalAnd, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.LogicalOr, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool, BuiltInTypeSymbol.Bool),

        // string/string
        new(BoundBinaryOperatorKind.Add, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String),
        new(BoundBinaryOperatorKind.Equals, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.NotEquals, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.Less, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.LessOrEqual, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.Greater, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String, BuiltInTypeSymbol.Bool),
        new(BoundBinaryOperatorKind.GreaterOrEqual, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String, BuiltInTypeSymbol.Bool),

        // string/number or number/string (for string interpolation)
        new (BoundBinaryOperatorKind.Add, BuiltInTypeSymbol.String, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.String),
        new (BoundBinaryOperatorKind.Add, BuiltInTypeSymbol.Number, BuiltInTypeSymbol.String, BuiltInTypeSymbol.String)
    ];

    public BoundBlockStatement BindConstructorBody(
        ConstructorSymbol ctor,
        NamedTypeSymbol containingType,
        BlockStatementNode body)
    {
        declarations.TypeMemberScopes.TryGetValue(containingType, out var memberScope);
        var localScope = new Scope(memberScope);
        var slotAllocator = new SlotAllocator();

        foreach (var parameter in ctor.Parameters)
        {
            parameter.SlotIndex = slotAllocator.Allocate();
            if (!localScope.TryDeclare(parameter))
            {
                diagnostics.Error(ErrorCode.DuplicateSymbol, $"Parameter '{parameter.Name}' is already declared.");
            }
        }

        var ctx = new BindContext
        {
            Declarations = declarations,
            Diagnostics = diagnostics,
            Scope = localScope,
            ContainingType = containingType,
            ContainingCallable = ctor,
            ExpectedType = containingType,
            IsLValueTarget = false,
            SlotAllocator = slotAllocator
        };
        return BindBlock(body, ctx);
    }

    public BoundBlockStatement BindMethodBody(
        MethodSymbol method,
        NamedTypeSymbol containingType,
        BlockStatementNode body)
    {
        declarations.TypeMemberScopes.TryGetValue(containingType, out var memberScope);
        var localScope = new Scope(memberScope);
        var slotAllocator = new SlotAllocator();

        foreach (var parameter in method.Parameters)
        {
            parameter.SlotIndex = slotAllocator.Allocate();
            if (!localScope.TryDeclare(parameter))
            {
                diagnostics.Error(ErrorCode.DuplicateSymbol, $"Parameter '{parameter.Name}' is already declared.");
            }
        }

        var ctx = new BindContext
        {
            Declarations = declarations,
            Diagnostics = diagnostics,
            Scope = localScope,
            ContainingType = containingType,
            ContainingCallable = method,
            ExpectedType = method.ReturnType,
            IsLValueTarget = false,
            SlotAllocator = slotAllocator
        };
        return BindBlock(body, ctx);
    }

    private BoundBlockStatement BindBlock(BlockStatementNode node, BindContext ctx)
    {
        var blockCtx = ctx.PushScope();
        var boundStatements = new List<BoundStatement>(node.Statements.Count);
        foreach (var stmt in node.Statements)
            boundStatements.Add(BindStatement(stmt, blockCtx));
        return new BoundBlockStatement(node.Span, boundStatements);
    }

    private BoundStatement BindStatement(IStatementNode node, BindContext ctx)
    {
        return node switch
        {
            BlockStatementNode b => BindBlock(b, ctx),
            ExpressionStatementNode e => new BoundExpressionStatement(e.Span, BindExpression(e.Expression, ctx)),
            ReturnStatementNode r => BindReturnStatement(r, ctx),
            VariableDeclarationStatementNode v => BindVariableDeclarationStatement(v, ctx),
            _ => BindBadStatement(node, ctx)
        };
    }

    private BoundStatement BindBadStatement(IStatementNode node, BindContext ctx)
    {
        ctx.Diagnostics.Error(
            ErrorCode.UnsupportedNode,
            $"Unsupported statement kind: {node.GetType().Name}");
        var errorTypes = ctx.Scope.Lookup("<error>");
        var errorTypeSymbol = (TypeSymbol)errorTypes.FirstOrDefault()!;
        return new BoundExpressionStatement(node.Span,
            new BoundErrorExpression(node.Span, errorTypeSymbol));
    }

    private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementNode node, BindContext ctx)
    {
        if (!node.ExplicitType!.IsNullOrWhiteSpace())
        {
            var declaredType = ResolveType(node.ExplicitType!, ctx);
            BoundExpression? init = null;

            if (node.Initializer is not null)
            {
                init = BindExpression(node.Initializer, ctx.WithExpectedType(declaredType));
                init = ConvertIfNeeded(init, declaredType, ctx, node.Span);
            }

            var local = new LocalSymbol(node.Name, declaredType)
            {
                DeclarationSpan = node.Span with { FilePath = ctx.ContainingCallable?.SourceFilePath },
                SlotIndex = ctx.SlotAllocator.Allocate()
            };
            DeclareLocal(local, ctx);
            return new BoundVariableDeclarationStatement(node.Span, local, init);
        }
        else
        {
            var init = BindExpression(node.Initializer!, ctx);
            var inferredType = init.Type;
            if (inferredType == BuiltInTypeSymbol.Unknown || inferredType == BuiltInTypeSymbol.Error)
            {
                ctx.Diagnostics.Warning(ErrorCode.UnableToInferType, $"Cannot infer type of {node.Name}");
            } else if (IsNullLiteral(init))
            {
                ctx.Diagnostics.Error(ErrorCode.UnableToInferType,
                    $"Cannot infer type of '{node.Name}' from 'null'. Add an explicit type.");
            }

            var local = new LocalSymbol(node.Name, inferredType)
            {
                DeclarationSpan = node.Span with { FilePath = ctx.ContainingCallable?.SourceFilePath },
                SlotIndex = ctx.SlotAllocator.Allocate()
            };
            DeclareLocal(local, ctx);
            return new BoundVariableDeclarationStatement(node.Span, local, init);
        }
    }

    private void DeclareLocal(LocalSymbol symbol, BindContext ctx)
    {
        if (!ctx.Scope.TryDeclare(symbol))
        {
            ctx.Diagnostics.Error(ErrorCode.VariableAlreadyDeclared, $"Variable '{symbol.Name}' already declared.");
        }
    }

    private BoundStatement BindReturnStatement(ReturnStatementNode node, BindContext ctx)
    {
        var expected = ctx.ContainingCallable?.ReturnType;
        if (expected is null)
        {
            ctx.Diagnostics.Error(ErrorCode.IllegalStatement, "Return statement outside of function");
            return new BoundReturnStatement(node.Span, null);
        }

        if (node.Value is null)
        {
            if (expected != BuiltInTypeSymbol.Void)
            {
                ctx.Diagnostics.Error(ErrorCode.IllegalStatement,
                    "Cannot return void from a method with non-void return type");
            }
            return new BoundReturnStatement(node.Span, null);
        }

        if (expected == BuiltInTypeSymbol.Void)
        {
            if (node.Value is not null)
            {
                ctx.Diagnostics.Error(ErrorCode.IllegalStatement, "Cannot return a value from a void method");
                _ = BindExpression(node.Value, ctx);
                return new BoundReturnStatement(node.Span, null);
            }
        }

        var expr = BindExpression(node.Value!, ctx.WithExpectedType(expected));

        if (!ReferenceEquals(expr.Type, BuiltInTypeSymbol.Error) && !ReferenceEquals(expr.Type, expected))
        {
            ctx.Diagnostics.Error(ErrorCode.TypeMismatch,
                $"'{ctx.ContainingCallable!.Name}': cannot return '{expr.Type.Name}' from a method with return type '{expected.Name}'.");
        }
        return new BoundReturnStatement(node.Span, expr);
    }

    private BoundExpression BindExpression(IExpressionNode node, BindContext ctx)
    {
        return node switch
        {
            AssignmentExpressionNode n => BindAssignment(n, ctx),
            BinaryExpressionNode n => BindBinary(n, ctx),
            CallExpressionNode n => BindCall(n, ctx),
            IdentifierExpressionNode n => BindIdentifier(n, ctx),
            LiteralExpressionNode n => BindLiteral(n, ctx),
            MemberAccessExpressionNode n => BindMemberAccess(n, ctx),
            NewExpressionNode n => BindNewExpression(n, ctx),
            _ => BindBadExpression(node, ctx)
        };
    }

    private BoundExpression BindBadExpression(IExpressionNode node, BindContext ctx)
    {
        ctx.Diagnostics.Error(ErrorCode.IllegalExpression, $"Unsupported expression kind: {node.GetType().Name}");
        return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
    }

    private BoundExpression BindLiteral(LiteralExpressionNode node, BindContext ctx)
    {
        var (value, type) = InferLiteral(node, ctx);
        return new BoundLiteralExpression(node.Span, value, type);
    }

    private BoundExpression BindIdentifier(IdentifierExpressionNode node, BindContext ctx)
    {
        var candidates = ctx.Scope.Lookup(node.Name);
        if (candidates.Count == 0)
        {
            ctx.Diagnostics.Error(ErrorCode.IdentifierNotFound, $"Unknown identifier '{node.Name}'.");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }

        var local = candidates.OfType<VariableSymbol>().FirstOrDefault();
        if (local is null)
        {
            ctx.Diagnostics.Error(ErrorCode.IdentifierNotFound, $"'{node.Name}' was not found");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }

        return new BoundLocalExpression(node.Span, local);
    }

    private BoundExpression BindAssignment(AssignmentExpressionNode node, BindContext ctx)
    {
        var target = BindExpression(node.Target, ctx.WithLValueTarget(true));
        var value = BindExpression(node.Right, ctx.WithExpectedType(target.Type));

        // TODO: Validate that target can be assigned to
        return new BoundAssignmentExpression(node.Span, target, value);
    }

    private BoundExpression BindBinary(BinaryExpressionNode node, BindContext ctx)
    {
        var left = BindExpression(node.Left, ctx);
        var right = BindExpression(node.Right, ctx);

        var op = ResolveBinaryOperator(node.Operator, left.Type, right.Type, ctx);
        return op is null
            ? new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error)
            : new BoundBinaryExpression(node.Span, left, op, right);
    }

    private BoundExpression BindCall(CallExpressionNode node, BindContext ctx)
    {
        var args = node.Arguments.Select(a => BindExpression(a, ctx)).ToArray();
        var memberAccess = BindExpression(node.Target, ctx);
        if (memberAccess is not BoundMethodGroupExpression boundAccess)
        {
            ctx.Diagnostics.Error(ErrorCode.TargetNotCallable, $"Cannot call method on non-member access expression.");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }
        var functions = boundAccess.Candidates;
        if (functions.Length == 0)
        {
            ctx.Diagnostics.Error(ErrorCode.IdentifierNotFound, $"No functions found on type '{boundAccess.Receiver.Type.Name}'");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }
        var best = functions.FirstOrDefault(f => f.Arity == args.Length + 1) ?? functions.First();
        var expectedParams = best.Parameters.Skip(1).ToArray();
        for (var i = 0; i < args.Length && i < expectedParams.Length; i++)
        {
            var argType = args[i].Type;
            var paramType = expectedParams[i].Type;
            if (!ReferenceEquals(argType, BuiltInTypeSymbol.Error) &&
                !ReferenceEquals(argType, paramType))
            {
                ctx.Diagnostics.Error(ErrorCode.TypeMismatch,
                    $"Argument {i + 1} of '{best.Name}' has type '{argType.Name}', but expected '{paramType.Name}'.", args[i].Span);
            }
        }
        return new BoundCallExpression(node.Span, best, boundAccess.Receiver, args);
    }

    private BoundExpression BindMemberAccess(MemberAccessExpressionNode node, BindContext ctx)
    {
        var receiver = BindExpression(node.Object, ctx);
        var receiverType = receiver.Type;

        if (receiverType is not NamedTypeSymbol namedType)
        {
            ctx.Diagnostics.Error(ErrorCode.IllegalAccess, $"Cannot access member '{node.TargetName}' of non-type '{receiverType.Name}'");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }

        if (!ctx.TryGetMemberScope(namedType, out var memberScope))
        {
            ctx.Diagnostics.Error(ErrorCode.TypeNotFound, $"Cannot find member scope for type '{namedType.Name}'");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }

        var candidates = memberScope.Lookup(node.TargetName);

        if (candidates.Count == 0)
        {
            ctx.Diagnostics.Error(ErrorCode.UnknownMember,
                $"Type '{namedType.Name}' has no member named '{node.TargetName}'");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }

        var methods = candidates.OfType<MethodSymbol>().ToArray();
        if (methods.Length > 0)
        {
            return new BoundMethodGroupExpression(node.Span, receiver, [..methods]);
        }

        var property = candidates.OfType<PropertySymbol>().FirstOrDefault();
        if (property is not null)
            return new BoundMemberAccessExpressionReceiver(node.Span, receiver, property);
        var field = candidates.OfType<FieldSymbol>().FirstOrDefault();
        if (field is not null)
            return new BoundMemberAccessExpressionReceiver(node.Span, receiver, field);
        ctx.Diagnostics.Error(ErrorCode.UnknownMember,
            $"Member '{node.TargetName}' on type '{namedType.Name}' is not a supported member kind yet");
        return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
    }

    private BoundExpression BindNewExpression(NewExpressionNode node, BindContext ctx)
    {
        var type = ResolveType(node.TypeName, ctx);
        if (type is not NamedTypeSymbol namedType)
        {
            ctx.Diagnostics.Error(ErrorCode.TypeNotConstructable, $"Type '{node.TypeName}' is not constructable");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }

        var args = node.Arguments.Select(a => BindExpression(a, ctx)).ToArray();
        if (!ctx.TryGetMemberScope(namedType, out var ctorScope))
        {
            ctx.Diagnostics.Error(ErrorCode.TypeNotFound, $"Cannot find constructor scope for type '{namedType.Name}'");
            return new BoundErrorExpression(node.Span, BuiltInTypeSymbol.Error);
        }
        
        var ctors = ctorScope.Lookup(namedType.Name).OfType<ConstructorSymbol>().ToArray();
        if (ctors.Length == 0)
        {
            ctx.Diagnostics.Error(ErrorCode.CannotFindConstructor, $"Type '{type.Name}' has no constructors");
            return new BoundErrorExpression(node.Span, type);
        }

        var requestedArity = args.Length + 1; // add 1 for 'this' parameter
        var ctor = ctors.FirstOrDefault(c => c.Arity == requestedArity);
        if (ctor is null)
        {
            ctx.Diagnostics.Error(ErrorCode.CannotFindConstructor, $"Cannot find constructor for '{type.Name}' with {requestedArity - 1} arguments");
            return new BoundErrorExpression(node.Span, type);
        }
        return new BoundNewExpression(node.Span, namedType, ctor, args);
    }

    private TypeSymbol ResolveType(string typeName, BindContext ctx)
    {
        var candidates = ctx.Scope.Lookup(typeName);
        if (candidates.Count == 0)
        {
            ctx.Diagnostics.Error(ErrorCode.TypeNotFound, $"Unknown type '{typeName}'.");
            return BuiltInTypeSymbol.Error;
        }
        var type = candidates.OfType<TypeSymbol>().FirstOrDefault();
        if (type is null)
        {
            ctx.Diagnostics.Error(ErrorCode.TypeNotFound, $"'{typeName}' is not a type.");
            return BuiltInTypeSymbol.Error;
        }
        return type;
    }

    private BoundBinaryOperator? ResolveBinaryOperator(string opToken, TypeSymbol left, TypeSymbol right,
        BindContext ctx)
    {
        var kind = ResolveOperatorKind(opToken);
        if (kind is null)
        {
            ctx.Diagnostics.Error(ErrorCode.InvalidOperator,
                $"Unknown binary operator '{opToken}'");
            return null;
        }

        if (IsNamedType(left) || IsNamedType(right))
        {
            if (kind is not (BoundBinaryOperatorKind.NotEquals or BoundBinaryOperatorKind.Equals))
            {
                ctx.Diagnostics.Error(ErrorCode.InvalidOperator,
                    $"Operator '{opToken}' is not valid for types '{left.Name}' and '{right.Name}'. Only equality operators are allowed");
                return null;
            }

            if (!ReferenceEquals(left, right))
            {
                ctx.Diagnostics.Error(
                    ErrorCode.InvalidOperator,
                    $"Cannot compare '{left.Name}' and '{right.Name}' with '{opToken}'. Types must match");
                return null;
            }
            // Comparison between named types is always bool
            return new BoundBinaryOperator(kind.Value, left, right, BuiltInTypeSymbol.Bool);
        }

        foreach (var op in Ops)
        {
            if (op.Kind == kind.Value &&
                ReferenceEquals(op.LeftType, left) &&
                ReferenceEquals(op.RightType, right))
            {
                return op;
            }
        }

        ctx.Diagnostics.Error(ErrorCode.InvalidOperator, $"Operator '{opToken}' is not defined for '{left.Name}' and '{right.Name}'.");
        
        return null;
    }

    private static BoundBinaryOperatorKind? ResolveOperatorKind(string token)
    {
        return token switch
        {
            "+" => BoundBinaryOperatorKind.Add,
            "-" => BoundBinaryOperatorKind.Subtract,
            "*" => BoundBinaryOperatorKind.Multiply,
            "/" => BoundBinaryOperatorKind.Divide,
            "==" => BoundBinaryOperatorKind.Equals,
            "!=" => BoundBinaryOperatorKind.NotEquals,
            "<" => BoundBinaryOperatorKind.Less,
            "<=" => BoundBinaryOperatorKind.LessOrEqual,
            ">" => BoundBinaryOperatorKind.Greater,
            ">=" => BoundBinaryOperatorKind.GreaterOrEqual,
            "&&" => BoundBinaryOperatorKind.LogicalAnd,
            "||" => BoundBinaryOperatorKind.LogicalOr,
            _ => null
        };
    }

    private static bool IsNamedType(TypeSymbol s) => s is NamedTypeSymbol;

    private static bool IsNullLiteral(BoundExpression expr) => expr is BoundLiteralExpression { Value: null };

    private (object? value, TypeSymbol type) InferLiteral(LiteralExpressionNode node, BindContext ctx)
    {
        var v = node.Value;
        switch (v)
        {
            case null:
                return (null, BuiltInTypeSymbol.Null);
            case bool:
                return (v, BuiltInTypeSymbol.Bool);
            case int or double:
                return (v, BuiltInTypeSymbol.Number);
            case string:
                return (v, BuiltInTypeSymbol.String);
            default:
                ctx.Diagnostics.Error(ErrorCode.UnsupportedLiteral, $"Unsupported literal type: {v.GetType().Name}");
                return (v, BuiltInTypeSymbol.Unknown);
        }
    }

    private BoundExpression ConvertIfNeeded(BoundExpression expr, TypeSymbol targetType, BindContext ctx, SourceSpan span)
    {
        if (ReferenceEquals(expr.Type, targetType))
            return expr;

        ctx.Diagnostics.Error(
            ErrorCode.TypeMismatch,
            $"Cannot convert expression of type '{expr.Type.Name}' to '{targetType.Name}'.");

        return new BoundErrorExpression(span, BuiltInTypeSymbol.Error);
    }
}