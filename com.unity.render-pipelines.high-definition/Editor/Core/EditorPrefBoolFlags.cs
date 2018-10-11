
using System;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal struct EditorPrefBoolFlags<T> : IEquatable<T>, IEquatable<EditorPrefBoolFlags<T>>
        where T : struct, IConvertible
    {
        readonly string m_Key;

        public T value
        { get => (T)(object)EditorPrefs.GetInt(m_Key); set => EditorPrefs.SetInt(m_Key, (int)(object)value); }

        public uint rawValue
        { get => (uint)(object)EditorPrefs.GetInt(m_Key); set => EditorPrefs.SetInt(m_Key, (int)(object)value); }

        public EditorPrefBoolFlags(string key) => m_Key = key;

        public bool Equals(T other) => (int)(object)value == (int)(object)other;
        public bool Equals(EditorPrefBoolFlags<T> other) => m_Key == other.m_Key;

        public bool HasFlag(T v) => ((uint)(object)v & rawValue) == (uint)(object)v;
        public bool SetFlag(T f, bool v)
        {
            if (v) rawValue |= (uint)(object)f;
            else rawValue &= ~(uint)(object)f;
            return v;
        }

        public static explicit operator T(EditorPrefBoolFlags<T> v) => v.value;
        public static EditorPrefBoolFlags<T> operator |(EditorPrefBoolFlags<T> l, T r)
        {
            l.rawValue |= (uint)(object)r;
            return l;
        }
        public static EditorPrefBoolFlags<T> operator &(EditorPrefBoolFlags<T> l, T r)
        {
            l.rawValue &= (uint)(object)r;
            return l;
        }
        public static EditorPrefBoolFlags<T> operator ^(EditorPrefBoolFlags<T> l, T r)
        {
            l.rawValue ^= (uint)(object)r;
            return l;
        }
    }
}
