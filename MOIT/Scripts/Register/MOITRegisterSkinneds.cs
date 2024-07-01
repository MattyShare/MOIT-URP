using System.Linq;
using UnityEngine;

namespace MOIT.Register
{
    //[RequireComponent(typeof(SkinnedMeshRenderer))]
    public class MOITRegisterSkinneds : MOITRegister
    {
        [SerializeField]
        private SkinnedMeshRenderer[] renderers;
        private Bounds bounds;

        private void CalculateBounds()
        {
            renderers[0].sharedMesh.RecalculateBounds();
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                renderers[i].sharedMesh.RecalculateBounds();
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        public override Bounds GetBounds()
        {
            CalculateBounds();
            return bounds;
        }

        public override bool IsVisible()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].isVisible)
                    return true;
            }
            return false;
        }

        public override int GetRendererCount()
        {
            return renderers.Length;
        }
    }
}
