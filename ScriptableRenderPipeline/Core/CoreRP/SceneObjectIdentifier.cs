using System;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public struct SceneObjectIdentifier
    {
        public static readonly SceneObjectIdentifier Invalid = new SceneObjectIdentifier(-1);

        [SerializeField]
        int m_ObjectId;

        public int objectId { get { return m_ObjectId; } }

        public SceneObjectIdentifier(int objectId)
        {
            this.m_ObjectId = objectId;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SceneObjectIdentifier))
                return false;

            var tobj = (SceneObjectIdentifier)obj;
            return tobj == this;
        }

        public override int GetHashCode()
        {
            return m_ObjectId.GetHashCode();
        }

        public static bool operator ==(SceneObjectIdentifier l, SceneObjectIdentifier r)
        {
            return l.m_ObjectId == r.m_ObjectId;
        }

        public static bool operator !=(SceneObjectIdentifier l, SceneObjectIdentifier r)
        {
            return !(l == r);
        }

        public override string ToString()
        {
            return m_ObjectId.ToString();
        }
    }
}
