using UnityEngine;

namespace MOIT.Register
{
    // used to add moit rendered objects to a list that the render 
    [DisallowMultipleComponent]
    public abstract class MOITRegister : MonoBehaviour
    {
        public abstract Bounds GetBounds();
        public abstract bool IsVisible();

        public virtual int GetRendererCount() => 1;

        public virtual void OnEnable()
        {
            MOITRendererList.Instance.RegisterObject(this);
        }

        public virtual void OnDisable()
        {
            MOITRendererList.Instance.DelistObject(this);
        }
    }
}
