
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    public static class Hash128Utilities
    {
        public static unsafe void ComputeHash128<T>(T value, ref Hash128 hashInOut)
        {
            var reference = __makeref(value);
            void* message = &reference;
            var size = Marshal.SizeOf(typeof(T));

            ComputeHash128(message, size, ref hashInOut);
        }

        public static unsafe void ComputeHash128(void* message, int size, ref Hash128 hashInOut)
        {
            fixed (void* hashData = &hashInOut)
            {
                var ptr = (ulong*)hashData;
                SpookyHashV2.Hash128(message, size, ptr, ptr + 1);
            }
        }

        public static unsafe Hash128 ComputeHash128<T>(T value)
        {
            var reference = __makeref(value);
            void* message = &reference;
            var size = Marshal.SizeOf(typeof(T));

            return ComputeHash128(message, size);
        }

        public static unsafe Hash128 ComputeHash128(void* message, int size)
        {
            var result = new Hash128();
            var ptr = (ulong*)&result;
            SpookyHashV2.Hash128(message, size, ptr, ptr + 1);
            return result;
        }

        public static void AppendHash(Hash128 toAdd, ref Hash128 hashInOut)
        {
            ComputeHash128(toAdd, ref hashInOut);
        }

        public static Hash128 CombineHashes(Hash128 a, Hash128 b)
        {
            var result = a;
            ComputeHash128(b, ref result);
            return result;
        }

        public static Hash128 CombineHashes(Hash128 a, Hash128 b, Hash128 c)
        {
            var result = a;
            ComputeHash128(b, ref result);
            ComputeHash128(c, ref result);
            return result;
        }

        public static Hash128 CombineHashes(Hash128 a, Hash128 b, Hash128 c, Hash128 d)
        {
            var result = a;
            ComputeHash128(b, ref result);
            ComputeHash128(c, ref result);
            ComputeHash128(d, ref result);
            return result;
        }

        public static Hash128 CombineHashes(Hash128 a, Hash128 b, Hash128 c, Hash128 d, Hash128 e)
        {
            var result = a;
            ComputeHash128(b, ref result);
            ComputeHash128(c, ref result);
            ComputeHash128(d, ref result);
            ComputeHash128(e, ref result);
            return result;
        }

        public static Hash128 CombineHashes(Hash128 a, Hash128 b, Hash128 c, Hash128 d, Hash128 e, Hash128 f)
        {
            var result = a;
            ComputeHash128(b, ref result);
            ComputeHash128(c, ref result);
            ComputeHash128(d, ref result);
            ComputeHash128(e, ref result);
            ComputeHash128(f, ref result);
            return result;
        }

        public static unsafe Hash128 QuantizedHash(Matrix4x4 matrix)
        {
            var quantisedMatrix = stackalloc int[16];
            for (var i = 0; i < 16; ++i)
                quantisedMatrix[i] = (int)((matrix[i] * 1000) + .5f);

            return ComputeHash128(&quantisedMatrix, 16 * sizeof(int));
        }

        public static unsafe Hash128 QuantizedHash(Vector3 vector)
        {
            var quantisedVector = stackalloc int[3];
            for (var i = 0; i < 3; ++i)
                quantisedVector[i] = (int)((vector[i] * 1000) + .5f);

            return ComputeHash128(&quantisedVector, 3 * sizeof(int));
        }
    }
}
