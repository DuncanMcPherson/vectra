namespace VectraCompiler.Core;

public static class Extensions
{
    extension(string value)
    {
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(value);
        }

        public bool IsNullOrWhiteSpace()
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}