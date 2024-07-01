using UnityEngine;

namespace MOIT.Register
{
    [RequireComponent(typeof(Renderer))]
    public class MOITRegisterRenderer : MOITRegister
    {
        [SerializeField]
        private Renderer _renderer;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
        }

        public override Bounds GetBounds()
        {
            return _renderer.bounds;
        }

        public override bool IsVisible()
        {
            return _renderer.isVisible;
        }
    }
}
