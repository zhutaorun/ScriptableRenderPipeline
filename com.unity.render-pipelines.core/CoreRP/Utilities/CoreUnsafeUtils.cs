using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Rendering
{
    public static unsafe class CoreUnsafeUtils
    {
        public interface IKeyGetter<TValue, TKey>
        {
            TKey Get(ref TValue v);
        }

        internal struct DefaultKeyGetter<T> : IKeyGetter<T, T>
        { public T Get(ref T v) { return v; } }

        public static void CopyTo<T>(this List<T> list, void* dest, int count)
            where T : struct
        {
            var c = Mathf.Min(count, list.Count);
            for (int i = 0; i < c; ++i)
                UnsafeUtility.WriteArrayElement<T>(dest, i, list[i]);
        }

        public static void CopyTo<T>(this T[] list, void* dest, int count)
            where T : struct
        {
            var c = Mathf.Min(count, list.Length);
            for (int i = 0; i < c; ++i)
                UnsafeUtility.WriteArrayElement<T>(dest, i, list[i]);
        }

        public static void QuickSort<T>(int count, void* data)
            where T : struct, IComparable<T>
        {
            QuickSort<T, T, DefaultKeyGetter<T>>(data, 0, count - 1);
        }

        public static void QuickSort<TValue, TKey, TGetter>(int count, void* data)
            where TKey : struct, IComparable<TKey>
            where TValue : struct
            where TGetter : struct, IKeyGetter<TValue, TKey>
        {
            QuickSort<TValue, TKey, TGetter>(data, 0, count - 1);
        }

        public static void QuickSort<TValue, TKey, TGetter>(void* data, int left, int right)
            where TKey : struct, IComparable<TKey>
            where TValue : struct
            where TGetter : struct, IKeyGetter<TValue, TKey>
        {
            // For Recursion
            if (left < right)
            {
                int pivot = Partition<TValue, TKey, TGetter>(data, left, right);

                if (pivot > 1)
                    QuickSort<TValue, TKey, TGetter>(data, left, pivot);

                if (pivot + 1 < right)
                    QuickSort<TValue, TKey, TGetter>(data, pivot + 1, right);
            }
        }

        // Just a sort function that doesn't allocate memory
        // Note: Should be replace by a radix sort for positive integer
        static int Partition<TValue, TKey, TGetter>(void* data, int left, int right)
            where TKey : struct, IComparable<TKey>
            where TValue : struct
            where TGetter : struct, IKeyGetter<TValue, TKey>
        {
            var getter = new TGetter();
            var pivotvalue = UnsafeUtility.ReadArrayElement<TValue>(data, left);
            var pivot = getter.Get(ref pivotvalue);

            --left;
            ++right;
            while (true)
            {
                var c = 0;
                var lvalue = default(TValue);
                var lkey = default(TKey);
                do
                {
                    ++left;
                    lvalue = UnsafeUtility.ReadArrayElement<TValue>(data, left);
                    lkey = getter.Get(ref lvalue);
                    c = lkey.CompareTo(pivot);
                }
                while (c < 0);

                var rvalue = default(TValue);
                var rkey = default(TKey);
                do
                {
                    --right;
                    rvalue = UnsafeUtility.ReadArrayElement<TValue>(data, right);
                    rkey = getter.Get(ref rvalue);
                    c = rkey.CompareTo(pivot);
                }
                while (c > 0);

                if (left < right)
                {
                    UnsafeUtility.WriteArrayElement(data, right, lvalue);
                    UnsafeUtility.WriteArrayElement(data, left, rvalue);
                }
                else
                {
                    return right;
                }
            }
        }
    }
}
