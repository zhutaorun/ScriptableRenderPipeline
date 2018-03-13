using NUnit.Framework;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.Tests
{
    [TestFixture]
    class HashUtilitiesTests
    {
        [Test]
        void ComputeHash128()
        {
            ComputeHash128_EqualsTest(0, 1);
            ComputeHash128_EqualsTest(0u, 1u);
            ComputeHash128_EqualsTest(0f, 1f);
            ComputeHash128_EqualsTest(0.0, 1.0);
            ComputeHash128_EqualsTest("zer", "zerez");
        }

        static void ComputeHash128_EqualsTest<T>(T a, T b)
        {
            {
                var hash0 = Hash128Utilities.ComputeHash128(a);
                var hash1 = Hash128Utilities.ComputeHash128(b);
                Assert.AreNotEqual(hash0, hash1, "Different values creates different hashes " + typeof(T));
            }

            {
                var hash0 = Hash128Utilities.ComputeHash128(a);
                var hash1 = Hash128Utilities.ComputeHash128(a);
                Assert.AreEqual(hash0, hash1, "Same values creates same hashes " + typeof(T));
            }
        }
    }
}
