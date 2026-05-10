# Unity2Rerun Research Positioning

Working title:

**Unity2Rerun: A Unity-Native Rerun Recording and Live Visualization Pipeline for Robotics Debugging**

## Abstract

Unity projects often need runtime telemetry that can be inspected outside the Game view, replayed after a run, and shared with robotics or simulation tooling. Existing approaches frequently rely on ad-hoc logs, custom editor windows, or external bridge processes that complicate player builds and IL2CPP deployment.

Unity2Rerun explores a Unity-native approach for Rerun workflows. The package records Unity runtime data into `.rrd` files, supports live output to Rerun Viewer, and provides attribute-driven generated logging through `[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]`. The design emphasizes visible Unity components, pure C# package integration, and validation across runtime tests, source-generator builds, live output smoke tests, and IL2CPP build paths.

To the best of our knowledge, Unity2Rerun is among the first public Unity packages we have found that combines managed Rerun `.rrd` writing, live Rerun Viewer output, and generated IL2CPP-safe attribute logging inside a Unity package without requiring an external bridge process for the basic workflow.

## Thesis

Unity can act as a practical Rerun telemetry source when logging is treated as a Unity-native package concern rather than as a separate bridge process. The key requirements are:

- Managed recording paths that fit Unity package workflows.
- Visible Unity components for lifecycle and user control.
- Generated logging paths that avoid reflection-heavy runtime discovery.
- Repeatable tests and manual smoke checks that cover file and live output.

## Contributions

1. **Unity-native Rerun output pipeline**

   Unity2Rerun provides package-level APIs and components for writing `.rrd` data and sending live output to Rerun Viewer from Unity runtime code.

2. **Declarative generated logging**

   `[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]` provide a concise way to mark Unity fields and transforms for generated logging. The generated code is driven by an explicit `RerunManager` rather than a hidden runtime hub.

3. **Pure C# package integration**

   The project is organized as a Unity package with runtime code, editor source generation, tests, samples, and documentation designed around Unity installation and player-build constraints.

4. **Evidence-oriented validation**

   Runtime tests, source-generator builds, encoding spike builds, gRPC transport spike builds, manual `.rrd` smoke checks, live Viewer smoke checks, and IL2CPP build checks are treated as project evidence.

## Novelty Boundary

Unity2Rerun does not claim to invent Rerun, `.rrd`, gRPC, Unity source generation, or runtime logging. Its contribution is the integration of these ideas into a Unity-native SDK shape, with generated logging and validation designed for Unity workflows and IL2CPP constraints.

The project is not an official Rerun SDK and should not be evaluated as a replacement for Rerun's own ecosystem. It is a Unity-focused bridge and package experiment.

## Related Work Boundary

Relevant comparison points include:

- Rerun's official SDKs and viewer ecosystem.
- Unity logging/debugging workflows based on editor windows, screenshots, or custom file formats.
- Unity robotics bridge approaches that rely on external middleware processes.
- AOT-oriented C# source-generation systems used for serialization, dependency injection, or codegen-heavy SDKs.
- General telemetry and visualization systems outside the Unity package model.

Unity2Rerun should be evaluated as a Unity package for Rerun-oriented runtime evidence, not as a universal robotics middleware or a complete clone of Rerun's official SDKs.

## Evidence Table

| Evidence | Purpose |
| --- | --- |
| Runtime unit tests | Validate core package behavior and generated logging contracts. |
| Source-generator build | Confirms generated logging code compiles independently of Unity runtime scenes. |
| Encoding spike build | Guards `.rrd` encoding experiments and package assumptions. |
| gRPC transport spike build | Guards live-output transport experiments. |
| Manual `.rrd` smoke | Confirms recorded files can be opened by Rerun Viewer. |
| Manual live-output smoke | Confirms Unity runtime data appears in Rerun Viewer. |
| IL2CPP build smoke | Confirms generated code and package references survive player builds. |
| Release evidence tag | Intended to pin the exact commit, tests, and artifacts used for research citation. |

## Citation Note

Software citation metadata is provided in [`CITATION.cff`](CITATION.cff). The initial evidence release is intended to use a tag such as `paper-evidence-2026-05-09`. A DOI can be added after archiving the GitHub release through Zenodo or another software archive.
