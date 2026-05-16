# Unity2Rerun

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity)](https://unity.com/)
[![Release](https://img.shields.io/badge/release-v0.4.0-green)](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/releases)

Unity2Rerun is a Unity-native SDK for [Rerun](https://rerun.io). It records Unity runtime data to `.rrd` files and can stream live output to Rerun Viewer without requiring an external bridge process for the basic workflow.

## Status

The current release target is v0.4.0. The package focuses on runtime logging, `.rrd` output with official-compatible footer/manifests, live Viewer output, publisher components, IL2CPP build support, `[RerunLog]` source generation, EncodedImage, Pinhole camera metadata, 3D boxes, trajectories, point clouds, planar laser scans, and local sidecar control.

## Version Requirements

- Unity 6000.0 LTSC or later (developed on 6000.3.14f1 LTSC; compatible with 6000.0.74f1 LTSC)
- Editor + Standalone Player. Windows is the verified target for the current SDK work; macOS/Linux are intended targets but not yet verified.
- Rerun Viewer / CLI 0.31.4+

## Package

Install the Unity package from:

```text
Packages/dev.unity2rerun.sdk/package.json
```

Package-level documentation is available in [Packages/dev.unity2rerun.sdk/README.md](Packages/dev.unity2rerun.sdk/README.md).

## Citation / Research Positioning

If you use Unity2Rerun in research, please cite the software metadata in [CITATION.cff](CITATION.cff). After the first GitHub release is archived by Zenodo, cite the Zenodo Concept DOI for the project or the version-specific DOI for exact reproducibility. A concise research-positioning note is available in [PAPER.md](PAPER.md).

Release and provenance documents:

- [Changelog](CHANGELOG.md)
- [v0.4.0 release notes](docs/releases/RELEASE_NOTES_v0.4.0.md)
- [Zenodo release checklist](docs/releases/ZENODO_RELEASE_CHECKLIST.md)
- [AI training notice](AI_NOTICE.md)

## License

This project is licensed under the [Apache License 2.0](LICENSE).
