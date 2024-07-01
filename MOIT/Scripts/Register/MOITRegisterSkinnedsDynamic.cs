using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace MOIT.Register
{
    public class MOITRegisterSkinnedsDynamic : MOITRegister
    {
        [SerializeField]
        private List<SkinnedMeshRenderer> renderers;
        private Bounds bounds;

        private bool isRegistered = false;

        private void CalculateBounds()
        {
            renderers[0].sharedMesh.RecalculateBounds();
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++)
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
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i].isVisible)
                    return true;
            }
            return false;
        }

        public override int GetRendererCount()
        {
            return renderers.Count;
        }

        public void AddRenderer(SkinnedMeshRenderer r)
        {
            renderers.Add(r);
            if (renderers.Count > 0 && !isRegistered)
                OnEnable(); // if was empty, will join the list again
        }

        public void RemoveRenderer(SkinnedMeshRenderer r)
        {
            renderers.Remove(r);
            if (renderers.Count < 1)
                OnDisable(); // leave the list
        }

        public override void OnEnable()
        {
            if (!isRegistered && renderers.Count > 0)
            {
                base.OnEnable();
                isRegistered = true;
            }
        }

        public override void OnDisable()
        {
            if (isRegistered)
            {
                base.OnDisable();
                isRegistered = false;
            }
        }
    }
}
