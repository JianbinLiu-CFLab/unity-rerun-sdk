# Unity2Rerun SDK Documentation

- [English](en/) - primary language
- [中文](zh/) - synchronized

## Quick Links

- [Prerequisites](en/00_Prerequisites.md)
- [Installation & Quick Start](en/01_Installation_and_Quick_Start.md)
- [Publisher Components](en/02_Publisher_Components.md)
- [Output Modes & Live Troubleshooting](en/03_Output_Modes_and_Live_Troubleshooting.md)
- [Architecture](en/04_Architecture.md)
- [IL2CPP Build Guide](en/05_IL2CPP_Build_Guide.md)
- [IL2CPP 构建指南](zh/05_IL2CPP构建指南.md)
- [RerunLog Source Generator](en/06_RerunLog_Source_Generator.md)
- [RerunLog Source Generator 中文](zh/06_RerunLog_Source_Generator.md)
- [Interactive 3D Control](en/07_Interactive_3D_Control.md)
- [RRD Compression](en/08_RRD_Compression.md)
- [Rerun Type Coverage Matrix](../../../docs/releases/RERUN_TYPE_COVERAGE_MATRIX.md)
- [Interactive 3D Control 中文](zh/07_Interactive_3D_Control.md)

## Release Validation

RRD compression validation writes comparable None and LZ4 recordings, prints ArrowMsg compression evidence, and verifies both files with the Rerun CLI:

```powershell
dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase14-compression-comparison build/RRD/phase14_compression
rerun rrd verify build/RRD/phase14_compression_none.rrd
rerun rrd verify build/RRD/phase14_compression_lz4.rrd
```

Rerun official type coverage is tracked by `docs/releases/RERUN_TYPE_COVERAGE_MATRIX.md` and checked with:

```powershell
python Scripts/release/check_rerun_type_coverage.py
```
