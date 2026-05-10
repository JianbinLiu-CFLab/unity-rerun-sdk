# Unity2Rerun

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)](https://unity.com/)

Unity2Rerun is a Unity-native SDK for [Rerun](https://rerun.io). It records Unity runtime data to `.rrd` files and can stream live output to Rerun Viewer without requiring an external bridge process for the basic workflow.

## Status

The package currently focuses on runtime logging, `.rrd` output, live Viewer output, publisher components, IL2CPP build support, and `[RerunLog]` source generation.

## Package

Install the Unity package from:

```text
Packages/dev.unity2rerun.sdk/package.json
```

Package-level documentation is available in [Packages/dev.unity2rerun.sdk/README.md](Packages/dev.unity2rerun.sdk/README.md).

## Citation / Research Positioning

If you use Unity2Rerun in research, please cite the software metadata in [CITATION.cff](CITATION.cff). A concise research-positioning note is available in [PAPER.md](PAPER.md).

## License

This project is licensed under the [Apache License 2.0](LICENSE).
