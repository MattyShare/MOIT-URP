#if HAS_VFX_GRAPH
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    internal class VFXShaderGraphMOITLitGUI : ShaderGraphMOITLitGUI
    //internal class VFXShaderGraphMOITLitGUI : ShaderGraphLitGUI
    {
        protected override uint materialFilter => uint.MaxValue & ~(uint)Expandable.SurfaceInputs;
    }
}
#endif