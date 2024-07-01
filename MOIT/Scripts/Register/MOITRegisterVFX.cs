using UnityEngine;
using UnityEngine.VFX;

namespace MOIT.Register
{
    // a normal moitregisterrenderer should work as well
    [RequireComponent(typeof(VisualEffect))]
    public class MOITRegisterVFX : MOITRegister
    {
        [SerializeField]
        //private VisualEffect v;
        private VFXRenderer r;

        public override Bounds GetBounds()
        {
            return r.bounds;
        }

        public override bool IsVisible()
        {
            return r.isVisible;
        }
    }
}
