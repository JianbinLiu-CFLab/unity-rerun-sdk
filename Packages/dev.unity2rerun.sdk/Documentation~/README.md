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
- [Interactive 3D Control 中文](zh/07_Interactive_3D_Control.md)

## Release Validation

Phase 13 `.rrd` output writes RRD footer/manifests by default and can optionally compress file Arrow payloads with LZ4. Generated files are expected to pass:

```powershell
rerun rrd verify <file.rrd>
rerun rrd stats <file.rrd>
```
