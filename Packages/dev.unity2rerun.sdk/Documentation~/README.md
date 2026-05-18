# Unity2Rerun SDK Documentation

This folder is the user manual entry point for the Unity2Rerun package.

English documentation is the canonical documentation for this package line. Chinese documentation remains under `zh/` and is maintained separately.

## Start Here

- [Prerequisites](en/00_Prerequisites.md): install Unity, Rerun Viewer/CLI, and optional live transport dependencies.
- [Installation and first recording](en/01_Installation_and_Quick_Start.md): install the package and write a first `.rrd` recording.
- [Samples and demo project](en/09_Samples_and_Demo_Project.md): choose between your own project, the repository demo project, and package samples.

## Runtime Publishing

- [Publisher components](en/02_Publisher_Components.md): use Inspector-driven TextLog, Scalar, Transform3D, camera, point, and scan publishers.
- [RerunLog source generator](en/06_RerunLog_Source_Generator.md): publish fields with generated `[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]` code.
- [Interactive 3D control](en/07_Interactive_3D_Control.md): verify image, pinhole, 3D geometry, point-cloud, laser-scan, metrics, logs, and loopback control workflows.

## Recording, Live Output, and Builds

- [Output modes and live troubleshooting](en/03_Output_Modes_and_Live_Troubleshooting.md): choose FileOnly, LiveOnly, or FileAndLive and diagnose live gRPC setup.
- [RRD compression](en/08_RRD_Compression.md): understand file-recording compression and LZ4 evidence.
- [IL2CPP build guide](en/05_IL2CPP_Build_Guide.md): build and verify standalone Players.

## Advanced and Reference

- [Architecture](en/04_Architecture.md): understand runtime modules, data flow, and Rerun type coverage boundaries.
- [Inspector reference](en/10_Inspector_Reference.md): field-by-field reference for `RerunManager`, publishers, and sidecar control.
- [Troubleshooting](en/11_Troubleshooting.md): symptom-based fixes for file output, live output, images, generated logs, sidecar control, and builds.
- [Rerun type coverage matrix](../../../docs/releases/RERUN_TYPE_COVERAGE_MATRIX.md): audited coverage against official Rerun runtime archetypes and components.

## Validation and Acceptance

- [Validation and acceptance](en/12_Validation_and_Acceptance.md): automated checks, RRD evidence commands, and manual Unity acceptance steps.

For release validation and manual acceptance, use the validation page rather than this entry page.

## Other Languages

- [Chinese documentation](zh/)
