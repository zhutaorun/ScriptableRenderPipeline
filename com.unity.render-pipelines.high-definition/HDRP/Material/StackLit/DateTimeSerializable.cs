using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [System.Serializable]
    public struct DateTimeSerializable
    {
        public long Value;
        public DateTimeSerializable(long val)
        {
            Value = val;
        }

        public static implicit operator DateTime(DateTimeSerializable dtserializable)
        {
            return DateTime.FromFileTimeUtc(dtserializable.Value);
        }
        public static implicit operator DateTimeSerializable(DateTime dt)
        {
            return new DateTimeSerializable(dt.ToFileTimeUtc());
        }
    }
}
