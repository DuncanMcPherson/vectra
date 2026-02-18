namespace VectraCompiler.Bind.Models;

public sealed class SlotAllocator
{
    private int _nextSlot = 0;
    public int NextSlot => _nextSlot;

    public int Allocate()
    {
        return _nextSlot++;
    }
}