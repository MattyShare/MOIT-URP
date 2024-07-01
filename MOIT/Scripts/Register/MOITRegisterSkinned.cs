using UnityEngine;

// use this version if you want bounds to be calculated each frame
// most of the time MOITRegisterRenderers is fine by default (Unity bounds according to the model and the animations on its animator), if you add skinned meshes in runtime just manually setup your bounds big enough
// the MOITRegisterSkinneds component is available if you have multiple skinned renderers on your mesh(es)

namespace MOIT.Register
{
    //[RequireComponent(typeof(SkinnedMeshRenderer))]
    public class MOITRegisterSkinned : MOITRegister
    {
        [SerializeField]
        private SkinnedMeshRenderer _renderer;

        public override Bounds GetBounds()
        {
            _renderer.sharedMesh.RecalculateBounds();
            return _renderer.bounds;
        }

        public override bool IsVisible()
        {
            return _renderer.isVisible;
        }
    }
}
