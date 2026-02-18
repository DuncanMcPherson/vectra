namespace VectraCompiler.Bind.Models;

public sealed class SlotAllocator
{
    private int _nextSlot = 0;

    public int Allocate()
    {
        return _nextSlot++;
    }
}