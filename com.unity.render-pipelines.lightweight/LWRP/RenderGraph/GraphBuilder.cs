using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraph
{
    public struct GraphBuilder
    {
        public NodeAttachment CreateAttachment(RenderTextureFormat format)
        {
            throw new NotImplementedException();
        }

        public T AddNode<T>(T node) where T : struct, IRenderNode
        {
            throw new NotImplementedException();
        }

        public RenderEdge<AttachmentIdentifier, AttachmentIdentifier> Connect(NodeAttachment from, NodeAttachment to)
        {
            throw new NotImplementedException();
        }
    }
}
