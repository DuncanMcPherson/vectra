using VectraCompiler.Emit.Models;

namespace VectraCompiler.Emit.Services;

public sealed class InstructionBuffer
{
    private readonly List<byte> _bytes = [];
    public IReadOnlyList<byte> Bytes => _bytes;
    
    public int Position => _bytes.Count;
    
    public void Emit(Opcode opcode)
        => _bytes.Add((byte)opcode);

    public void Emit(Opcode opcode, ushort operand)
    {
        _bytes.Add((byte)opcode);
        EmitUInt16(operand);
    }

    public void Emit(Opcode opcode, ushort operand1, ushort operand2)
    {
        _bytes.Add((byte)opcode);
        EmitUInt16(operand1);
        EmitUInt16(operand2);
    }

    public void EmitUInt16(ushort value)
    {
        _bytes.Add((byte)(value & 0xFF));
        _bytes.Add((byte)(value >> 8));
    }

    public int EmitJump(Opcode opcode)
    {
        _bytes.Add((byte)opcode);
        var patchPosition = _bytes.Count;
        _bytes.Add(0x00);
        _bytes.Add(0x00);
        return patchPosition;
    }

    public void PatchJump(int patchPosition)
    {
        var target = (ushort)_bytes.Count;
        _bytes[patchPosition] = (byte)(target & 0xFF);
        _bytes[patchPosition + 1] = (byte)(target >> 8);
    }
}