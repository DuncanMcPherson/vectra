using VectraCompiler.Bind.Bodies;
using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Emit.Models;

namespace VectraCompiler.Emit.Services;

public sealed class MethodBodyEmitter
{
    private readonly ConstantPool _pool;
    private readonly InstructionBuffer _buffer;
    private readonly CallableSymbol _callable;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MethodBodyEmitter(CallableSymbol callable, ConstantPool pool)
    {
        _callable = callable;
        _pool = pool;
        _buffer = new InstructionBuffer();
    }

    public IReadOnlyList<byte> Emit(BoundStatement body)
    {
        EmitStatement(body);

        if (_callable is MethodSymbol m && m.ReturnType == BuiltInTypeSymbol.Void)
            _buffer.Emit(Opcode.LOAD_NULL);
        else if (_callable is ConstructorSymbol)
            _buffer.Emit(Opcode.LOAD_NULL);

        _buffer.Emit(Opcode.RET);
        return _buffer.Bytes;
    }

    private void EmitStatement(BoundStatement node)
    {
        switch (node)
        {
            case BoundBlockStatement block:
                foreach (var stmt in block.Statements)
                    EmitStatement(stmt);
                break;
            case BoundExpressionStatement expr:
                EmitExpression(expr.Expression);
                _buffer.Emit(Opcode.POP);
                break;
            case BoundVariableDeclarationStatement varDecl:
                EmitVariableDeclaration(varDecl);
                break;
            case BoundReturnStatement ret:
                EmitReturn(ret);
                break;
            case BoundObjectAllocationStatement objAlloc:
                var typeIndex = _pool.AddType(objAlloc.Type);
                _buffer.Emit(Opcode.NEW_OBJ, typeIndex);
                _buffer.Emit(Opcode.STORE_LOCAL, (ushort)objAlloc.Target.SlotIndex);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported statement kind in emit: {node.GetType().Name}");
        }
    }

    private void EmitVariableDeclaration(BoundVariableDeclarationStatement node)
    {
        if (node.Initializer is null) return;
        EmitExpression(node.Initializer);
        _buffer.Emit(Opcode.STORE_LOCAL, (ushort)node.Local.SlotIndex);
    }

    private void EmitReturn(BoundReturnStatement node)
    {
        if (node.Expression is not null)
            EmitExpression(node.Expression);
        else
        {
            _buffer.Emit(Opcode.LOAD_NULL);
        }

        _buffer.Emit(Opcode.RET);
    }


    private void EmitExpression(BoundExpression node)
    {
        switch (node)
        {
            case BoundLiteralExpression literal:
                EmitLiteral(literal);
                break;
            case BoundLocalExpression local:
                int index;
                if (local.Local is ParameterSymbol p)
                    index = p.SlotIndex;
                else if (local.Local is LocalSymbol l)
                    index = l.SlotIndex;
                else throw new InvalidOperationException("Invalid local expression.");
                _buffer.Emit(Opcode.LOAD_LOCAL, (ushort)index);
                break;
            case BoundBinaryExpression binary:
                EmitBinary(binary);
                break;
            case BoundAssignmentExpression assignment:
                EmitAssignment(assignment);
                break;
            case BoundCallExpression call:
                EmitCall(call);
                break;
            case BoundNewExpression:
                throw new InvalidOperationException(
                    "BoundNewExpression should have been lowered before emit.");
            case BoundMemberAccessExpressionReceiver member:
                EmitMemberAccess(member);
                break;
            case BoundNativeFunctionCallExpression nativeCall:
                EmitNativeCall(nativeCall);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported expression kind in emit: {node.GetType().Name}");
        }
    }
    
    private void EmitLiteral(BoundLiteralExpression node)
    {
        switch (node.Value)
        {
            case int i:
            {
                var numIndex = _pool.AddNumber(i);
                _buffer.Emit(Opcode.LOAD_CONST, numIndex);
                break;
            }
            case double d:
            {
                var numIndex = _pool.AddNumber(d);
                _buffer.Emit(Opcode.LOAD_CONST, numIndex);
                break;
            }
            case string s:
                var strIndex = _pool.AddString(s);
                _buffer.Emit(Opcode.LOAD_CONST, strIndex);
                break;
            case bool b:
                _buffer.Emit(b ? Opcode.LOAD_TRUE : Opcode.LOAD_FALSE);
                break;
            case null:
                _buffer.Emit(Opcode.LOAD_NULL);
                break;
        }
    }
    
    private void EmitBinary(BoundBinaryExpression node)
    {
        EmitExpression(node.Left);
        EmitExpression(node.Right);
        var opcode = node.Operator.Kind switch
        {
            BoundBinaryOperatorKind.Add           => Opcode.ADD,
            BoundBinaryOperatorKind.Subtract      => Opcode.SUB,
            BoundBinaryOperatorKind.Multiply      => Opcode.MUL,
            BoundBinaryOperatorKind.Divide        => Opcode.DIV,
            // BoundBinaryOperatorKind.Modulo        => Opcode.MOD,
            BoundBinaryOperatorKind.Equals        => Opcode.CEQ,
            BoundBinaryOperatorKind.NotEquals     => Opcode.CNE,
            BoundBinaryOperatorKind.Less          => Opcode.CLT,
            BoundBinaryOperatorKind.LessOrEqual   => Opcode.CLE,
            BoundBinaryOperatorKind.Greater       => Opcode.CGT,
            BoundBinaryOperatorKind.GreaterOrEqual => Opcode.CGE,
            _ => throw new InvalidOperationException(
                $"Unsupported binary operator: {node.Operator.Kind}")
        };
        _buffer.Emit(opcode);
    }
    
    private void EmitAssignment(BoundAssignmentExpression node)
    {
        EmitExpression(node.Value);
        switch (node.Target)
        {
            case BoundLocalExpression local:
                _buffer.Emit(Opcode.DUP); // keep value on stack (assignment is an expression)
                int index;
                if (local.Local is ParameterSymbol p)
                    index = p.SlotIndex;
                else if (local.Local is LocalSymbol l)
                    index = l.SlotIndex;
                else throw new InvalidOperationException("Invalid local expression.");
                _buffer.Emit(Opcode.LOAD_LOCAL, (ushort)index);
                break;
            case BoundMemberAccessExpressionReceiver member:
                EmitExpression(member.Receiver);
                var memberIndex = member.Member is FieldSymbol f
                    ? _pool.AddField(f)
                    : _pool.AddProperty((PropertySymbol)member.Member);
                _buffer.Emit(Opcode.STORE_MEMBER, memberIndex);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported assignment target: {node.Target.GetType().Name}");
        }
    }
    
    private void EmitCall(BoundCallExpression node)
    {
        // Push all arguments (this is already arg 0 for instance calls)
        foreach (var arg in node.Arguments)
            EmitExpression(arg);

        var argCount = (ushort)node.Arguments.Count;

        switch (node.Method)
        {
            case ConstructorSymbol ctor:
                var ctorIndex = _pool.AddConstructor(ctor);
                _buffer.Emit(Opcode.CALL_CTOR, ctorIndex, argCount);
                break;
            case MethodSymbol method:
                var methodIndex = _pool.AddMethod(method);
                _buffer.Emit(Opcode.CALL, methodIndex, argCount);
                break;
        }
    }

    private void EmitMemberAccess(BoundMemberAccessExpressionReceiver node)
    {
        EmitExpression(node.Receiver);
        var memberIndex = node.Member is FieldSymbol f
            ? _pool.AddField(f)
            : _pool.AddProperty((PropertySymbol)node.Member);
        _buffer.Emit(Opcode.LOAD_MEMBER, memberIndex);
    }

    private void EmitNativeCall(BoundNativeFunctionCallExpression node)
    {
        foreach (var arg in node.Arguments)
            EmitExpression(arg);
        _buffer.Emit(Opcode.CALL_NATIVE,
            (ushort)node.NativeFunction.NativeIndex,
            (ushort)node.Arguments.Length);
    }
}