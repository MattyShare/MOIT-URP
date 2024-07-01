using UnityEngine;
using System.Collections.Generic;

namespace MOIT.Register
{
    // assumes that bounds must be updated on each call (each frame if moit render feature is on)
    public class MOITRegisterRenderersDynamic : MOITRegister
    {
        [SerializeField]
        private List<Renderer> renderers;
        [Tooltip("If the renderers list is empty, how big should we make the list (settings ignored if prewarmSize < 1)\nUseful to prepare for usage by pools")] // note : pool objects can also each have a register on them, that will take care of everything onenabled and ondisabled, but i imagine it would be more performant to do with fewer components if there are a lot of instances
        [SerializeField]
        private int prewarmSize = 0;
        private Bounds bounds;

        private bool isRegistered = false;

        void Start()
        {
            // if this list starts empty, set to a known prewarmsize so that it doesn't get resized everytime a renderer is added
            if (renderers.Count < 1 && prewarmSize > 0)
                renderers = new List<Renderer>(prewarmSize);
        }

        public override Bounds GetBounds()
        {
            UpdateBounds();
            return bounds;
        }

        private void UpdateBounds()
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
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

        // debug function
        public override int GetRendererCount()
        {
            return renderers.Count;
        }

        public void AddRenderer(Renderer r)
        {
            renderers.Add(r);
            if (renderers.Count > 0 && !isRegistered)
                OnEnable(); // if was empty, will join the list again
        }

        public void RemoveRenderer(Renderer r)
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
