Grass:
Geometry shader that creates interactable grass from a plane. This grass is highly customizable, and has unique wind pulse movement.

- Update global material property "PlayerPosition" at runtime to have player push away grass.
- To enable meshes that hide grass:
      1. Setup an orthographic Camera overhead of player, looking down at the ground. The output of this camera should be to a Render Texture.
      2. Create mesh with black material over area you want the grass hidden. 
      3. Render Texture Camera culling mask only views meshes with this black material.
      4. Set main camera culling mask to not view these meshes.
      5. Update global material properties "CameraPosition", "OrthographicCamSize" at runtime to update shader based on meshes currently in view of camera.
      6. Set global material property "GlobalEffectRT" on Awake() for the same script as #5.
- Highly customizable, play around with it!
