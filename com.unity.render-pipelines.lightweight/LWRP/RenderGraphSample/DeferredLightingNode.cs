using RenderGraph;
using UnityEngine.Experimental.Rendering;

namespace RenderGraphSample
{
    public struct DeferredLightingNode : IRenderNode
    {
        public NodeAttachment depth { get; private set; }
        public NodeAttachment albedo { get; private set; }
        public NodeAttachment specRough { get; private set; }
        public NodeAttachment normal { get; private set; }
        public NodeAttachment lighting { get; private set; }

        public void Setup(ref ResourceBuilder builder)
        {
            depth = builder.ReadAttachment();
            albedo = builder.ReadAttachment();
            specRough = builder.ReadAttachment();
            normal = builder.ReadAttachment();
            lighting = builder.WriteAttachment();
        }

        public void Run(ref ResourceContext r, ScriptableRenderContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
