# Publisher Components Sample

Inspector-driven recording without writing code.

## Steps

1. Create an empty GameObject, name it `Rerun`.
2. Add a `RerunManager` component.
3. Create a cube (`GameObject > 3D Object > Cube`), name it `SampleCube`.
4. Select `Rerun`, then add `RerunPublisherSampleDriver`.
5. Drag `SampleCube` into the driver's `Target Object` field.
6. Enter Play Mode. The driver adds/configures the publisher components on `SampleCube`.
7. Stop after a few seconds. Check the `.rrd` file path printed in Console.
8. Open the `.rrd` with `rerun <path>` or drag it into the Viewer.

The sample does not create scene objects automatically. The cube should be visible
in the scene before entering Play Mode, matching the Unity2Foxglove sample flow.
