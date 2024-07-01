using UnityEngine;
using UnityCommunity.UnitySingleton;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace MOIT.Register
{
    //[ExecuteInEditMode]
    public class MOITRendererList : PersistentMonoSingleton<MOITRendererList>
    {
        // set this to the expected max number of MOIT renderers, it's fine if there is more but resizing the list too often is not too good
        public int initialListSize = 1000;
        private List<MOITRegister> registeredObjects;
        private Bounds bounds;

        protected override void OnInitializing()
        {
            registeredObjects = new List<MOITRegister>(initialListSize);
            base.OnInitializing();
        }

        // returns the amount of registered MOITRegister components (can be multiple renderers in one)
        public int GetCount()
        {
            return registeredObjects.Count;
        }

        // returns the total amount of registered renderers
        public int GetRendererCount()
        {
            int ret = 0;
            for (int i = 0; i < registeredObjects.Count; i++)
            {
                ret += registeredObjects[i].GetRendererCount();
            }
            return ret;
        }

        // returns the total amount of visible renderers
        // note: for register components with multiple renderers, if one is visible, all are considered visible ; this is the same for the rendering part
        public int GetVisibleRendererCount()
        {
            int ret = 0;
            for (int i = 0; i < registeredObjects.Count; i++)
            {
                if (registeredObjects[i].IsVisible())
                    ret += registeredObjects[i].GetRendererCount();
            }
            return ret;
        }

        // only call this after checking GetCount() > 0
        public Bounds GetBounds()
        {
            bounds = registeredObjects[0].GetBounds();
            for (int i = 1; i < registeredObjects.Count; i++)
            {
                bounds.Encapsulate(registeredObjects[i].GetBounds());
            }
            return bounds;
        }

        // only call this after checking GetCount() > 0
        public Bounds GetVisibleBounds()
        {
            bounds = registeredObjects[0].GetBounds();
            for (int i = 1; i < registeredObjects.Count; i++)
            {
                if (registeredObjects[i].IsVisible())
                    bounds.Encapsulate(registeredObjects[i].GetBounds());
            }
            return bounds;
        }

        public bool GetVisibleBounds(out Bounds bounds)
        {
            bool oneVisible = false;
            //bounds = registeredObjects[0].GetBounds();
            bounds = new Bounds();
            for (int i = 0; i < registeredObjects.Count; i++)
            {
                if (registeredObjects[i].IsVisible())
                {
                    Bounds rb = registeredObjects[i].GetBounds();
                    if (oneVisible)
                    {
                        bounds.Encapsulate(rb);
                    }
                    else
                    {
                        bounds = rb;
                        oneVisible = true;
                    }
                }
            }
            return oneVisible;
        }

        public Bounds SafeGetBounds()
        {
            bounds = new Bounds();
            if (registeredObjects.Count > 0)
            {
                bounds = GetBounds();
            }
            return bounds;
        }

        public void RegisterObject(MOITRegister obj)
        {
            registeredObjects.Add(obj);
        }

        public void DelistObject(MOITRegister obj)
        {
            registeredObjects.Remove(obj);
        }
        /*
        private void Update()
        {
            Debug.Log(registeredObjects.Count.ToString());
        }
        */
    }
}
