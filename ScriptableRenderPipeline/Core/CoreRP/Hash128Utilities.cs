
namespace UnityEngine.Experimental.Rendering
{
    public static class Hash128Utilities
    {
        public static unsafe void ComputeHash128<T>(T value, ref Hash128 hashInOut)
        {
            var size = sizeof(T);
            fixed (void* data = &hashInOut)
            fixed (void* message = &value)
            {
                var ptr = (ulong*)data;
                SpookyHashV2.Hash128(message, size, ptr, ptr + 1);
            }
        }
    }
}
