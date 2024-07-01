using System.Linq;
using UnityEngine;

// note : if static assumes renderers don't move relative to each other over time

namespace MOIT.Register
{
    //[RequireComponent(typeof(Renderer))]
    public class MOITRegisterRenderers : MOITRegister
    {
        [SerializeField]
        private Renderer[] renderers;
        //[SerializeField]
        //private bool updateOnGet = false;
        private Bounds bounds;

        public override Bounds GetBounds() => gameObject.isStatic ? GetBoundsStatic() : GetBoundsDynamic();

        void Start()
        {
            UpdateBounds();
        }

        public void UpdateBounds()
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }
        /*
        public override Bounds GetBounds()
        {
            if (updateOnGet)
                UpdateBounds();
            return bounds;
        }
        */

        public Bounds GetBoundsStatic()
        {
            return bounds;
        }

        public Bounds GetBoundsDynamic()
        {
            UpdateBounds();
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