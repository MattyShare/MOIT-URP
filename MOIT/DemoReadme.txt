
Included shaders : ASE templates (Lit and Unlit), lit, unlit, Text Mesh Pro.

- How to install the demo package :
1. The demo unitypackage can be downloaded from the releases tab on the Github repository. It comes with its own already setup universal renderer assets. Import the package.
2. Open your Project Settings, go to the Quality tab and add a quality level (name it Demo or anything).
3. Assign the PC_RPAsset_MOITDemo file to the quality level's Render Pipeline Asset.
4. Select that quality level to set it active.
5. Go to the Tags and Layers tab and add a TransparentMOIT layer in User Layer 10. In your own project it could be any layer, but this one was used in the demo files.
6. If your project isn't setup for TextMeshPro yet, go to Window -> Text Mesh Pro -> Import TMPro Essentials 
The Demo scenes will now work as intended.

You can safely remove the quality level and delete the Demo folder if you want to clean up your project. Just move out any file you want to keep using, such as the example shaders.