using NUnit.Framework;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public static class Utilities
    {
        public static float RandomFloat(float i, float seed)
        {
            var f = Mathf.Sin((i + 1) * 2.0f) * seed;
            f = f - (int)f + 1;
            return f;
        }
        public static Color RandomColor(float i)
        {
            return new Color(
                RandomFloat(i, 1634.3643f),
                RandomFloat(i, 5938.1651f),
                RandomFloat(i, 8315.3246f)
            );
        }

        public static T RandomEnum<T>(float i)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new InvalidOperationException();
            var length = Enum.GetValues(typeof(T)).Length;
            return (T)(object)(int)(RandomFloat(i, 6142.1359f) * (length - 1));
        }

        public static bool RandomBool(float i)
        {
            return RandomFloat(i, 26756.25634f) > 0.5f;
        }

        public static int RandomInt(float i)
        {
            return (int)(RandomFloat(i, 7325.7824f) * 100000);
        }

        public static Vector3 RandomVector3(float i)
        {
            return new Vector3(
                RandomFloat(i, 62054.6842f) * 10000.0f,
                RandomFloat(i, 78645.9785f) * 10000.0f,
                RandomFloat(i, 13056.8760f) * 10000.0f
            );
        }

        public static Quaternion RandomQuaternion(float i)
        {
            return Quaternion.LookRotation(RandomVector3(i));
        }

        public static void AssertAreEqual(Vector3 l, Vector3 r)
        {
            Assert.True(
                Mathf.Approximately(l.x, r.x)
                && Mathf.Approximately(l.y, r.y)
                && Mathf.Approximately(l.z, r.z)
            );
        }

        public static void AssertAreEqual(Quaternion l, Quaternion r)
        {
            AssertAreEqual(l.eulerAngles, r.eulerAngles);
        }

        public static void AssertAreEqual(Matrix4x4 l, Matrix4x4 r)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 4; ++x)
                    Assert.True(Mathf.Approximately(l[x, y], r[x, y]));
            }
        }
    }
}
