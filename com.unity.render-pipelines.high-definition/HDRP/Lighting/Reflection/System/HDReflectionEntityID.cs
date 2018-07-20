namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // TODO: this ID should be persistent over a domain reload and play mode
    public struct HDReflectionEntityID
    {
        internal struct SetID
        {
            public readonly int entityId;
            public readonly int version;

            public SetID(int entityId, int version)
            {
                this.entityId = entityId;
                this.version = version;
            }
        }
        internal SetID setId;
        internal readonly HDReflectionEntityType type;

        internal HDReflectionEntityID(SetID setId, HDReflectionEntityType type)
        {
            this.setId = setId;
            this.type = type;
        }
    }
}
