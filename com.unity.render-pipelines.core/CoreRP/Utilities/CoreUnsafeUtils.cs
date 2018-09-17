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

        public static int IndexOf<T>(void* data, int count, T v)
            where T : struct, IEquatable<T>
        {
            for (int i = 0; i < count; ++i)
            {
                if (UnsafeUtility.ReadArrayElement<T>(data, i).Equals(v))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Compare hashes of two collections and provide
        /// a list of indices <paramref name="removeIndices"/> to remove in <paramref name="oldHashes"/>
        /// and a list of indices <paramref name="addIndices"/> to add in <paramref name="newHashes"/>.
        ///
        /// Assumes that <paramref name="newHashes"/> and <paramref name="oldHashes"/> are sorted.
        /// </summary>
        /// <param name="oldHashCount">Number of hashes in <paramref name="oldHashes"/>.</param>
        /// <param name="oldHashes">Previous hashes to compare.</param>
        /// <param name="newHashCount">Number of hashes in <paramref name="newHashes"/>.</param>
        /// <param name="newHashes">New hashes to compare.</param>
        /// <param name="addIndices">Indices of element to add in <paramref name="newHashes"/> will be written here.</param>
        /// <param name="removeIndices">Indices of element to remove in <paramref name="oldHashes"/> will be written here.</param>
        /// <param name="addCount">Number of elements to add will be written here.</param>
        /// <param name="remCount">Number of elements to remove will be written here.</param>
        /// <returns>The number of operation to perform (<code><paramref name="addCount"/> + <paramref name="remCount"/></code>)</returns>
        public static int CompareHashes<TOldValue, TOldGetter, TNewValue, TNewGetter>(
            int oldHashCount, void* oldHashes,
            int newHashCount, void* newHashes,
            // assume that the capacity of indices is >= max(oldHashCount, newHashCount)
            int* addIndices, int* removeIndices,
            out int addCount, out int remCount
        )
            where TOldValue : struct
            where TNewValue : struct
            where TOldGetter : struct, IKeyGetter<TOldValue, Hash128>
            where TNewGetter : struct, IKeyGetter<TNewValue, Hash128>
        {
            var oldGetter = new TOldGetter();
            var newGetter = new TNewGetter();

            addCount = 0;
            remCount = 0;
            // Check combined hashes
            if (oldHashCount == newHashCount)
            {
                var oldHash = new Hash128();
                var newHash = new Hash128();
                CombineHashes<TOldValue, TOldGetter>(oldHashCount, oldHashes, &oldHash);
                CombineHashes<TNewValue, TNewGetter>(newHashCount, newHashes, &newHash);
                if (oldHash == newHash)
                    return 0;
            }

            var numOperations = 0;

            var oldI = 0;
            var newI = 0;

            while (oldI < oldHashCount || newI < newHashCount)
            {
                // At the end of old array.
                if (oldI == oldHashCount)
                {
                    // No more hashes in old array. Add remaining entries from new array.
                    for (; newI < newHashCount; ++newI)
                    {
                        addIndices[addCount++] = newI;
                        ++numOperations;
                    }
                    continue;
                }

                // At end of new array.
                if (newI == newHashCount)
                {
                    // No more hashes in old array. Remove remaining entries from old array.
                    for (; oldI < oldHashCount; ++oldI)
                    {
                        removeIndices[remCount++] = oldI;
                        ++numOperations;
                    }
                    continue;
                }

                // Both arrays have data.
                var newVal = UnsafeUtility.ReadArrayElement<TNewValue>(newHashes, newI);
                var oldVal = UnsafeUtility.ReadArrayElement<TOldValue>(oldHashes, oldI);
                var newKey = newGetter.Get(ref newVal);
                var oldKey = oldGetter.Get(ref oldVal);
                if (newKey == oldKey)
                {
                    // Matching hash, skip.
                    ++newI;
                    ++oldI;
                    continue;
                }

                // Both arrays have data, but hashes do not match.
                if (newKey < oldKey)
                {
                    // oldIter is the greater hash. Push "add" jobs from the new array until reaching the oldIter hash.
                    while (newI < newHashCount && newKey < oldKey)
                    {
                        addIndices[addCount++] = newI;
                        ++newI;
                        ++numOperations;
                        newVal = UnsafeUtility.ReadArrayElement<TNewValue>(newHashes, newI);
                        newKey = newGetter.Get(ref newVal);
                    }
                }
                else
                {
                    // newIter is the greater hash. Push "remove" jobs from the old array until reaching the newIter hash.
                    while (oldI < oldHashCount && oldKey < newKey)
                    {
                        removeIndices[remCount++] = oldI;
                        ++numOperations;
                        ++oldI;
                    }
                }
            }

            return numOperations;
        }

        public static int CompareHashes(
            int oldHashCount, Hash128* oldHashes,
            int newHashCount, Hash128* newHashes,
            // assume that the capacity of indices is >= max(oldHashCount, newHashCount)
            int* addIndices, int* removeIndices,
            out int addCount, out int remCount
        )
        {
            return CompareHashes<Hash128, DefaultKeyGetter<Hash128>, Hash128, DefaultKeyGetter<Hash128>>(
                oldHashCount, oldHashes,
                newHashCount, newHashes,
                addIndices, removeIndices,
                out addCount, out remCount
            );
        }

        /// <summary>Combine all of the hashes of a collection of hashes.</summary>
        /// <param name="count">Number of hash to combine.</param>
        /// <param name="hashes">Hashes to combine.</param>
        /// <param name="outHash">Hash to update.</param>
        public static void CombineHashes<TValue, TGetter>(int count, void* hashes, Hash128* outHash)
            where TValue : struct
            where TGetter : struct, IKeyGetter<TValue, Hash128>
        {
            var getter = new TGetter();
            for (int i = 0; i < count; ++i)
            {
                var v = UnsafeUtility.ReadArrayElement<TValue>(hashes, i);
                var h = getter.Get(ref v);
                HashUtilities.AppendHash(ref h, ref *outHash);
            }
                
        }

        public static void CombineHashes(int count, Hash128* hashes, Hash128* outHash)
        {
            CombineHashes<Hash128, DefaultKeyGetter<Hash128>>(count, hashes, outHash);
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
