# Generated RerunLog Sample

This sample uses `[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]` attributes.

## Steps

1. In Package Manager, select `Unity2Rerun SDK`.
2. Open the `Samples` tab.
3. Import `Generated RerunLog`.
4. In your scene, create `GameObject > 3D Object > Cube`.
5. Rename it to `SampleCube`.
6. Select `SampleCube`, then add component `Rerun Generated Log Sample`.
7. Make sure the scene also has a visible `Rerun` GameObject with `Rerun Manager`.
8. Set `Rerun Manager` to `FileOnly` or `FileAndLive`.
9. Press Play.

Expected Rerun entities:

- `logs/generated`
- `metrics/generated_fps`
- `world/generated_cube`

The sample does not create objects automatically. The cube is deliberately visible in the Hierarchy so the recording setup is easy to inspect.
