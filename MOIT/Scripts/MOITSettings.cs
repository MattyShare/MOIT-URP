using UnityEngine;

[CreateAssetMenu(fileName = "MOITSettings", menuName = "Scriptable Objects/MOITSettings")]
public class MOITSettings : ScriptableObject
{
    [Header("Quality features")]
    [Tooltip("4 is fine for most situations and the most performant.\nCan be upped to 6 if needed (lots of close objects with varying colors) and still stay somewhat performant\n8 has diminishing returns and trigonometric should be used instead at that point")]
    public MOITRenderFeature.MomentsCount momentsCount = MOITRenderFeature.MomentsCount._4;
    [Tooltip("Store moments in 16 (half) of 32bit precision (half is fine for most realtime uses)\nUNFORTUNATELY HALF POWER MOMENTS REQUIRE ROVs (Rasterizer Ordered Views) - out of scope for now, so will override to single precision unless trigonometric")]
    public MOITRenderFeature.FloatPrecision momentPrecision = MOITRenderFeature.FloatPrecision._Single; //_Half;
    [Tooltip("If better precision is needed and performance is not a concern, set trigonometric to true, use single precision and 6 moments (or 8)\n(4 moments = 2 trigonometric, 6m = 3t, 8m = 4t)")]
    public bool trigonometric = false;
    [Header("Bounds")]
    [Tooltip("Method of finding MOIT renderers in order to build the conservative bounding sphere that we use to warp depth, lowering numerical errors\n- NearFarPlanes: just use near and far planes (essentially keep low precision)\n- FindObjects: not optimized but automatic (Renderers only)\n- Register: user needs to add a script to every transparent object (Renderer and VFX)")]
    public MOITRenderFeature.BoundsType boundsType = MOITRenderFeature.BoundsType.FindObjects;
    [Tooltip("Setting for BoundsType.FindObject and BoundType.Register :\nShould we check if each renderer is visible (by any camera) before adding its bounds?")]
    public bool onlyVisibleRenderers = false;
    [Header("Rendering")]
    [Tooltip("Works with Everything but usually MOIT objects should be set on specific layers that are removed from the Transparent Layer Mask (in Universal Renderer Data) to prevent double rendering")]
    public LayerMask layerMask = Physics.AllLayers;
    [Tooltip("Set a different RenderQueueRange than default Transparent")]
    public int renderQueueMin = 2501;
    public int renderQueueMax = 3000;
    public Material compositeMaterial;
    [Tooltip("Back to front sorting does not matter in OIT techniques so we skip it, however this is probably desirable if you intend to write to depth\n(example : base DoF on the closest transparent object instead of the first opaque on the pixel)")]
    public bool sortBackToFront = false;
    [Header("Debug")]
    [Tooltip("Set this to true to be able to visualize the MOIT texture in the fullscreen feature using RT_test_mat (else only B0 B1 and B2 (if applicable) are available)")]
    public bool debugMakeMOITTexGlobal = false;
}
