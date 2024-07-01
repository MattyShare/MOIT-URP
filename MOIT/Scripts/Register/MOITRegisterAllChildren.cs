using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

namespace MOIT.Register
{
    [RequireComponent(typeof(Transform))]
    public class MOITRegisterAllChildren : MOITRegister
    {
        [Tooltip("Layers of MOIT transparent (set the same as in the MOIT Settings used in the MOIT Renderer Feature)")]
        public LayerMask layerMask = Physics.AllLayers;
        [Tooltip("Recalculate bounds everytime the feature asks for it (once per frame)?\nSet this to true if this object or any children move")]
        public bool updateOnGet = false;

        private Bounds bounds;
        //private Renderer[] renderers;
        [HideInInspector, SerializeField]
        private List<Renderer> renderers;

        private bool isRegistered = false;

        public override void OnEnable()
        {
            SearchForChildren();
            if (!isRegistered && renderers.Count > 0)
            {
                if (!updateOnGet)
                    UpdateBounds();
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

        public override Bounds GetBounds()
        {
            if (updateOnGet)
                UpdateBounds();
            return bounds;
        }

        // debug function
        public override int GetRendererCount()
        {
            //return renderers.Length;
            return renderers.Count;
        }

        public override bool IsVisible()
        {
            //for (int i = 0; i < renderers.Length; i++)
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i].isVisible)
                    return true;
            }
            return false;
        }

        private void UpdateBounds()
        {
            bounds = renderers[0].bounds;
            //for (int i = 1; i < renderers.Length; i++)
            for (int i = 1; i < renderers.Count; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        // call this in case you added or removed children (not needed if this object got disabled and reenabled)
        public void SearchForChildren(bool updateBounds = false)
        {
            Renderer[] r = GetComponentsInChildren<Renderer>();
            List<Renderer> renderersList = new List<Renderer>(r.Length);
            for (int i = 0; i < r.Length; i++)
            {
                if ((layerMask & 1 << r[i].gameObject.layer) > 0)
                    renderersList.Add(r[i]);
            }
            //renderers = renderersList.ToArray();
            renderers = renderersList;
            //bounds = new Bounds();
            if (updateBounds)
                UpdateBounds();
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
                OnDisable(); // leave the list if emptied
        }
    }
}
