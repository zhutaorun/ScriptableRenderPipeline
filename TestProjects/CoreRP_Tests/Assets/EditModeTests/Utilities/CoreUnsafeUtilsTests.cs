using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.Tests
{
    public unsafe class CoreUnsafeUtilsTests
    {
        public struct TestData : IEquatable<TestData>
        {
            public int intValue;
            public float floatValue;

            public bool Equals(TestData other)
            {
                return intValue == other.intValue && floatValue == other.floatValue;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TestData))
                    return false;
                return Equals((TestData)obj);
            }

            public override int GetHashCode()
            {
                fixed (float* fptr = &floatValue)
                return intValue ^ *(int*)fptr;
            }
        }

        static object[][] s_CopyToList = new object[][]
        {
            new object[] { new List<TestData>
            {
                new TestData { floatValue = 2, intValue = 1 },
                new TestData { floatValue = 3, intValue = 2 },
                new TestData { floatValue = 4, intValue = 3 },
                new TestData { floatValue = 5, intValue = 4 },
                new TestData { floatValue = 6, intValue = 5 },
            } }
        };

        [Test]
        [TestCaseSource("s_CopyToList")]
        public void CopyToList(List<TestData> datas)
        {
            var dest = stackalloc TestData[datas.Count];
            datas.CopyTo(dest, datas.Count);

            for (int i = 0; i < datas.Count; ++i)
                Assert.AreEqual(datas[i], dest[i]);
        }



        static object[][] s_CopyToArray = new object[][]
        {
            new object[] { new TestData[]
            {
                new TestData { floatValue = 2, intValue = 1 },
                new TestData { floatValue = 3, intValue = 2 },
                new TestData { floatValue = 4, intValue = 3 },
                new TestData { floatValue = 5, intValue = 4 },
                new TestData { floatValue = 6, intValue = 5 },
            } }
        };

        [Test]
        [TestCaseSource("s_CopyToArray")]
        public void CopyToArray(TestData[] datas)
        {
            var dest = stackalloc TestData[datas.Length];
            datas.CopyTo(dest, datas.Length);

            for (int i = 0; i < datas.Length; ++i)
                Assert.AreEqual(datas[i], dest[i]);
        }

        static object[][] s_QuickSort = new object[][]
        {
            new object[] { "noop",                  new int[] { 0, 1 } },
            new object[] { "Invert 2 element",      new int[] { 1, 0 } },
            new object[] { "Unique set",            new int[] { 0, 4, 2, 6, 3, 7, 1, 5 } },
            new object[] { "Set with duplicates",   new int[] { 0, 4, 2, 6, 4, 7, 1, 5 } },
        };

        [Test]
        [TestCaseSource("s_QuickSort")]
        public void QuickSort(string name, int[] values)
        {
            // We must perform a copy to avoid messing the test data directly
            var ptrValues = stackalloc int[values.Length];
            values.CopyTo(ptrValues, values.Length);

            CoreUnsafeUtils.QuickSort<int>(values.Length, ptrValues);

            for (int i = 0; i < values.Length - 1; ++i)
                Assert.LessOrEqual(ptrValues[i], ptrValues[i + 1]);
        }

        static object[][] s_CompareHashes = new object[][]
        {
            new object[] {
                "no data",
                new Hash128[] { },
                new Hash128[] { },
                new int[] { }, new int[] { }, 0
            },
            new object[] {
                "1 to remove",
                new Hash128[] { new Hash128(0x0, 0x0) },
                new Hash128[] { },
                new int[] { }, new int[] { 0 }, 1
            },
            new object[] {
                "1 to add",
                new Hash128[] { },
                new Hash128[] { new Hash128(0x0, 0x0) },
                new int[] { 0 }, new int[] { }, 1
            },
            new object[] {
                "1 and nothing to do",
                new Hash128[] { new Hash128(0x0, 0x0) },
                new Hash128[] { new Hash128(0x0, 0x0) },
                new int[] { }, new int[] { }, 0
            },
            new object[] {
                "1 to add, 1 to remove",
                new Hash128[] { new Hash128(0x0, 0x1) },
                new Hash128[] { new Hash128(0x0, 0x0) },
                new int[] { 0 }, new int[] { 0 }, 2
            },
            new object[] {
                "1 to remove, with existing hashes",
                new Hash128[] { new Hash128(0x0, 0x0), new Hash128(0x0, 0x1) },
                new Hash128[] { new Hash128(0x0, 0x0) },
                new int[] { }, new int[] { 1 }, 1
            },
            new object[] {
                "1 to add, with existing hashes",
                new Hash128[] { new Hash128(0x0, 0x0) },
                new Hash128[] { new Hash128(0x0, 0x0), new Hash128(0x0, 0x1) },
                new int[] { 1 }, new int[] { }, 1
            }
        };

        [Test]
        [TestCaseSource("s_CompareHashes")]
        public void CompareHashes(
            string name,
            Hash128[] oldHashesArray,
            Hash128[] newHashesArray,
            int[] expAddIndices, int[] expRemIndices,
            int expNumOp
        )
        {
            fixed (Hash128* oldHashes = oldHashesArray)
            fixed (Hash128* newHashes = newHashesArray)
            {
                var addIndices = stackalloc int[newHashesArray.Length];
                var remIndices = stackalloc int[oldHashesArray.Length];
                var addCount = 0;
                var remCount = 0;

                var numOp = CoreUnsafeUtils.CompareHashes(
                    oldHashesArray.Length, oldHashes,
                    newHashesArray.Length, newHashes,
                    addIndices, remIndices,
                    out addCount, out remCount
                );

                Assert.AreEqual(expNumOp, numOp);
                Assert.AreEqual(expAddIndices.Length, addCount);
                Assert.AreEqual(expRemIndices.Length, remCount);

                for (int i = 0; i < expAddIndices.Length; ++i)
                    Assert.AreEqual(expAddIndices[i], addIndices[i]);
                for (int i = 0; i < expRemIndices.Length; ++i)
                    Assert.AreEqual(expRemIndices[i], remIndices[i]);
            }
        }
    }
}
