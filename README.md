# MOIT-URP
A Renderer Feature implementing Moment Based Order Independent Transparency for Unity URP.

* URP Transparency :

[https://github-production-user-asset-6210df.s3.amazonaws.com/173833411/344429350-c5d79828-c7a5-4e9b-ae3b-f886cf50efe6.mp4](https://github.com/MattyShare/MOIT-URP/assets/173833411/ecc6b588-bde4-494c-b67e-2b33c970d545)

* MOIT :

[https://github.com/MattyShare/MOIT-URP/assets/173833411/1fbdecd9-5dad-4238-918d-0450dccec323](https://github.com/MattyShare/MOIT-URP/assets/173833411/cf80efca-ac9e-468e-9248-a2f2d1e1bf5c)

All videos have been recorded with the lowest quality mode.

The recommended quality modes (in single precision) are :
* 4 power moments
* 3 trigonometric moments
* 4 trigonometric moments

These 3 seem to have the best quality to performance ratios according to [the paper](https://momentsingraphics.de/I3D2018.html).
Higher quality modes get rid of color blending between overlapping transparent objects, at the expense of performance and/or memory.
It is possible to switch quality modes in runtime, an options menu, for cutscenes...


# Features
* Order independent transparency
* Several quality modes
* Includes templates for Amplify Shader Editor
* Shader examples, and step by step tutorial to implement MOIT support to your shaders
* Demo scenes 


# Requirements
```
Unity 6, Universal Render Pipeline (URP), render graph only
```

# Quick Start
1. In a Unity 6 URP project, import the "MOIT" folder into the "Assets" folder
2. Open the Project Settings, Tags and Layers tab, and add a TransparentMOIT layer in any available User Layer. You can make more according to your project's needs.
3. Find your Universal Renderer Data and add a Moment Based Order Independent Transparency Feature.
4. Assign the MOIT Settings and Bias Settings scriptable objects in the Feature. There are examples in the MOIT folder. The bias settings use the recommended values from the paper.
5. Assign the layer(s) you created at step 2 in the MOITSettings Layer Mask field and remove them from the Transparent Layer Mask on your Universal Renderer Data.
This will prevent drawing transparent objects twice, once as MOIT and once as normal transparent.
6. Any transparent object must be on the TransparentMOIT layer(s) to be rendered by the MOIT Feature. If not, they will be rendered as normal transparent (if they have a forward pass)

> [!NOTE]
> You can get shader examples from the releases tab or the demo package.
> Included shaders : ASE templates (Lit and Unlit), lit, unlit, Text Mesh Pro.


# License
Custom must share, share alike variant of the BSD-3 license.
> [!WARNING]
> **Please note the license requires licensees to share their improvements to this library for free, and under this license.**
> * Does the license apply to bigger works?
>   * You only have to share the improvements you made to the library, not your whole game/app/etc.
> * How to share?
>   * However feels the best for you. As an example, small contributions can be a text message in Issues, bigger changes can be Pull Requests, forks, another repository...
> * What is meant by "in good faith"?
>   * This is just to avoid "we technically respected the license" grifters hiding random non numbered lines in .txts in a nondescript random name repository, and other funny business I can't think of.
> * Why make this license?
>   * Basically it's just please share any insight you get, if you end up using it in a project. You got this for free, give a brick back to others. Maybe we'll all get a better implementation this way.


# Limitations
* Blending of close surfaces, especially with differing colors, a tradeoff of the technique.
* Could not force VFX Graph support. Opaque vfx work as expected of course, but transparent vfx will be rendered by the normal transparent pass, either over or under all MOIT transparent. MOIT Particle systems work fine.
* The technique allows to quantize moments into half instead of single precision, unfortunately this requires ROVs (Rasterizer Orderered Views) which seemed out of scope for this little project.
* This implementation uses a Renderer Feature. This makes it easier to import in a project to try it out, but straight up replacing the transparent pass would probably be better. Which requires modifying URP files, making it harder to update Unity version.
* As this project is experimental, did not implement non render graph compatibility mode.
* Has the same limitations as normal transparency for refracting objects (the closest refracting object hides other transparents behind)


# Future works
- [ ] Jobify fractions of the bound encapsulations from register renderers mode
- [ ] Find a better way to get the bounds of MOIT renderers (hopefully using the RendererListHandle from render graph...)
- [ ] Switch to Rasterizer Ordered Views
- [ ] Use ROVs to finish implementation of quantized moments (adding a half precision mode)
- [ ] Additional passes to allow refracting objects to refract transparent objects behind them
- [ ] Moment shadow pass (for transparent)
- [ ] VFX graph support


# Acknowledgements
* [Original Paper](https://momentsingraphics.de/I3D2018.html)
* [This implementation on LWRP (MIT)](https://github.com/ecidevilin/KhaosLWRP) helped a lot
* Includes [Unity Singleton from UnityCommunity (MIT)](https://github.com/UnityCommunity/UnitySingleton)
* Uses a demo scene from [OIT_Lab (MIT)](https://github.com/candycat1992/OIT_Lab)
* "Glass of Tea" (https://skfb.ly/or7uZ) by Tiago Lopes is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
* "phoenix bird" (https://skfb.ly/6vLBp) by NORBERTO-3D is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
* "Robot Playground" (https://skfb.ly/6QXFq) by Hadrien59 is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).


# More videos
> [!NOTE]
> Normal transparency and MOIT are not supposed to be used at the same time as they are different passes that can't know each other's depth.
> In the following comparison recordings, the transparent pass happens after and will always render on top.

* Colorful close objects. Difficult situation for MOIT, can see blending. But the popping from back to front transparency is more distracting.
  * Spheres : 

https://github.com/MattyShare/MOIT-URP/assets/173833411/ceb8eb98-0b4b-4eff-9bba-2c9a8b101d42

* Quads : 

https://github.com/MattyShare/MOIT-URP/assets/173833411/b5f248c7-b115-4cab-a3bf-1cd5100443be

* Robot model : 

https://github.com/MattyShare/MOIT-URP/assets/173833411/07adf8ad-ab0f-49c1-89e5-947dc0be1862
