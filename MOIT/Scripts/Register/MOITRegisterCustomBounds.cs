using UnityEngine;

namespace MOIT.Register
{
    [RequireComponent(typeof(Transform))]
    public class MOITRegisterCustomBounds : MOITRegister
    {
        public Vector3 size = new Vector3(1, 1, 1);

        private Bounds bounds;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //bounds = new Bounds(transform.position, Vector3.Scale(transform.localScale, size));
            bounds = new Bounds(transform.position, size);
        }

        public override Bounds GetBounds()
        {
            return bounds;
        }

        public override bool IsVisible()
        {
            return true;
        }
    }
}
