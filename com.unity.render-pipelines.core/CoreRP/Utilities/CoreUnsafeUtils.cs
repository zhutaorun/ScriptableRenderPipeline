using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Collection of utilities for unsafe C# code.
    /// </summary>
    public static unsafe class CoreUnsafeUtils
    {
        /// <summary>Combine all of the hashes of a collection of hashes.</summary>
        /// <param name="count">Number of hash to combine.</param>
        /// <param name="hashes">Hashes to combine.</param>
        /// <param name="outHash">Hash to update.</param>
        public static void CombineHashes(int count, Hash128* hashes, Hash128* outHash)
        {
            for (int i = 0; i < count; ++i)
                HashUtilities.AppendHash(ref hashes[i], ref *outHash);
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
        public static int CompareHashes(
            int oldHashCount, Hash128* oldHashes,
            int newHashCount, Hash128* newHashes,
            // assume that the capacity of indices is >= max(oldHashCount, newHashCount)
            int* addIndices, int* removeIndices,
            out int addCount, out int remCount
        )
        {
            addCount = 0;
            remCount = 0;
            // Check combined hashes
            if (oldHashCount == newHashCount)
            {
                var oldHash = new Hash128();
                var newHash = new Hash128();
                CombineHashes(oldHashCount, oldHashes, &oldHash);
                CombineHashes(newHashCount, newHashes, &newHash);
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
                if (newHashes[newI] == oldHashes[oldI])
                {
                    // Matching hash, skip.
                    ++newI;
                    ++oldI;
                    continue;
                }

                // Both arrays have data, but hashes do not match.
                if (newHashes[newI] < oldHashes[oldI])
                {
                    // oldIter is the greater hash. Push "add" jobs from the new array until reaching the oldIter hash.
                    while (newI < newHashCount && newHashes[newI] < oldHashes[oldI])
                    {
                        addIndices[addCount++] = newI;
                        ++newI;
                        ++numOperations;
                    }
                }
                else
                {
                    // newIter is the greater hash. Push "remove" jobs from the old array until reaching the newIter hash.
                    while (oldI < oldHashCount && oldHashes[oldI] < newHashes[newI])
                    {
                        removeIndices[remCount++] = oldI;
                        ++numOperations;
                    }
                }
            }

            return numOperations;
        }

        /// <summary>
        /// Remove and resize all the item in <paramref name="array"/> at indices <paramref name="indices"/>.
        /// <paramref name="indices"/> are expected to be sorted in increasing order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="count"></param>
        /// <param name="indices"></param>
        public static void RemoveSortedIndicesInArray<T>(ref T[] array, int count, int* indices)
        {
            if (count > array.Length)
                throw new ArgumentException();

            var lastItemIndex = array.Length - 1;
            for (int i = count - 1; i > 0; --i)
            {
                Assert.IsTrue(indices[i] > indices[i - 1], "Provided indices are not sorted in increasing order.");

                var index = indices[i];
                // Swap back to delete
                array[index] = array[lastItemIndex];
                --lastItemIndex;
            }
            // Unroll last loop
            {
                var index = indices[0];
                // Swap back to delete
                array[index] = array[lastItemIndex];
                --lastItemIndex;
            }

            Array.Resize(ref array, lastItemIndex + 1);
        }

        /// <summary>
        /// Copy into <paramref name="dest"/> the values from <paramref name="src"/> designated by the indices <paramref name="indices"/>.
        /// </summary>
        /// <param name="count">Number of indices</param>
        /// <param name="indices"></param>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="stride">stride of one element in bytes.</param>
        public static void CopyToIndirect(int count, int* indices, void* src, void* dest, int stride)
        {
            var bsrc = (byte*)src;
            var bdest = (byte*)dest;
            for (int i = 0; i < count; ++i)
            {
                var srcAddress = bsrc + stride * indices[i];
                var destAddress = bdest + stride * i;
                UnsafeUtility.MemCpy(destAddress, srcAddress, stride);
            }
        }

        /// <summary>Copy the content of a list into a memory pointer.</summary>
        /// <typeparam name="T">Type of the element in the collection.</typeparam>
        /// <param name="list">The source of the copy.</param>
        /// <param name="dest">The destination of the copy.</param>
        /// <param name="count">The maximum number of element to be copied.</param>
        /// <returns>Number of copied elements.</returns>
        public static int CopyTo<T>(this List<T> list, void* dest, int count)
            where T : struct
        {
            var c = Mathf.Min(count, list.Count);
            for (int i = 0; i < c; ++i)
                UnsafeUtility.WriteArrayElement<T>(dest, i, list[i]);

            return c;
        }

        /// <summary>Copy the content of aan array into a memory pointer.</summary>
        /// <typeparam name="T">Type of the element in the collection.</typeparam>
        /// <param name="list">The source of the copy.</param>
        /// <param name="dest">The destination of the copy.</param>
        /// <param name="count">The maximum number of element to be copied.</param>
        /// <returns>Number of copied elements.</returns>
        public static int CopyTo<T>(this T[] list, void* dest, int count)
            where T : struct
        {
            var c = Mathf.Min(count, list.Length);
            for (int i = 0; i < c; ++i)
                UnsafeUtility.WriteArrayElement<T>(dest, i, list[i]);

            return c;
        }

        /// <summary>
        /// Perform a quick sort on a array provided as a pointer.
        ///
        /// The provided pointer <paramref name="data"/> will be modified with the sorted data.
        /// </summary>
        /// <typeparam name="T">Type of element to process.</typeparam>
        /// <param name="count">Number of element in the array.</param>
        /// <param name="data">The pointer to the elements to sort.</param>
        public static void QuickSort<T>(int count, void* data)
            where T : struct, IComparable<T>
        {
            QuickSort<T>(data, 0, count - 1);
        }

        /// <summary>
        /// Perform a quick sort on a array provided as a pointer.
        ///
        /// The provided pointer <paramref name="data"/> will be modified with the sorted data.
        /// <see cref="QuickSort{T}(int, void*)"/>.
        /// </summary>
        /// <typeparam name="T">Type of element to process.</typeparam>
        /// <param name="data">The pointer to the elements to sort.</param>
        /// <param name="left">Index of the first element to sort.</param>
        /// <param name="right">Index of the last element to sort.</param>
        public static void QuickSort<T>(void* data, int left, int right)
            where T : struct, IComparable<T>
        {
            // For Recursion
            if (left < right)
            {
                int pivot = Partition<T>(data, left, right);

                if (pivot > 1)
                    QuickSort<T>(data, left, pivot);

                if (pivot + 1 < right)
                    QuickSort<T>(data, pivot + 1, right);
            }
        }

        // Just a sort function that doesn't allocate memory
        // Note: Shoud be repalc by a radix sort for positive integer
        static int Partition<T>(void* data, int left, int right)
            where T : struct, IComparable<T>
        {
            var pivot = UnsafeUtility.ReadArrayElement<T>(data, left);

            --left;
            ++right;
            while (true)
            {
                var lvalue = default(T);
                do { ++left; }
                while ((lvalue = UnsafeUtility.ReadArrayElement<T>(data, left)).CompareTo(pivot) < 0);

                var rvalue = default(T);
                do { --right; }
                while ((rvalue = UnsafeUtility.ReadArrayElement<T>(data, right)).CompareTo(pivot) > 0);

                if (left < right)
                {
                    UnsafeUtility.WriteArrayElement<T>(data, right, lvalue);
                    UnsafeUtility.WriteArrayElement<T>(data, left, rvalue);
                }
                else
                {
                    return right;
                }
            }
        }
    }
}
