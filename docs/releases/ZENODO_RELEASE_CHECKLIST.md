# Zenodo Release Checklist

This checklist prepares Unity2Rerun for a citable GitHub release archived by Zenodo.

## Why

Zenodo archival does not prevent plagiarism by itself, but it creates a durable, timestamped, citable snapshot with DOI metadata. Together with Git history, release notes, license metadata, and public documentation, it provides strong provenance evidence for priority and attribution.

## Metadata Prepared in This Repository

- `CITATION.cff` describes the software for GitHub citation and Zenodo metadata import.
- `README.md` points readers to citation, release, and provenance documents.
- `CHANGELOG.md` records release-level changes.
- `docs/releases/RELEASE_NOTES_v0.4.0.md` records the release scope and validation evidence.
- `AI_NOTICE.md` records the author's ethical position without modifying the Apache-2.0 license.
- `Packages/dev.unity2rerun.sdk/RELEASE_NOTES.md` records package-level release evidence.

Do not add `.zenodo.json` unless there is a specific metadata field that `CITATION.cff` cannot express. Zenodo supports both, but if both files are present, Zenodo uses `.zenodo.json` and ignores `CITATION.cff` for GitHub release archiving.

## Before Creating the GitHub Release

1. Confirm the working tree contains only intended release changes.

   ```powershell
   git status --short --untracked-files=all
   git diff --check
   ```

2. Run automated tests.

   ```powershell
   dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
   ```

3. Regenerate and verify the Phase 11 smoke recording.

   ```powershell
   dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase11-rrd build/RRD/phase11_sensor_smoke.rrd
   rerun rrd verify build/RRD/phase11_sensor_smoke.rrd
   rerun rrd stats build/RRD/phase11_sensor_smoke.rrd
   ```

4. Commit the release-preparation changes.

5. Tag the exact commit.

   ```powershell
   git tag -a v0.4.0 -m "Unity2Rerun v0.4.0"
   git push origin feature/Phase11_Rerun_Sensor_Typed_Publisher_Parity
   git push origin v0.4.0
   ```

6. Merge to the public release branch if needed before creating the GitHub Release.

## Zenodo GitHub Integration

1. Log in to Zenodo and link the GitHub account.
2. In Zenodo GitHub settings, enable the `unity-rerun-sdk` repository.
3. Create a GitHub Release for tag `v0.4.0`.
4. Wait for Zenodo to process the release.
5. Open the generated Zenodo record and confirm:
   - Title: `Unity2Rerun`
   - Version: `0.4.0`
   - Author: `Jianbin Liu`
   - License: `Apache-2.0`
   - Repository URL: `https://github.com/JianbinLiu-CFLab/unity-rerun-sdk`
   - Keywords include Unity, Rerun, RRD, telemetry, IL2CPP, source generation
6. Copy the version DOI and Concept DOI.
7. Update:
   - `README.md` DOI badge
   - `CITATION.cff` DOI field
   - `docs/releases/RELEASE_NOTES_v0.4.0.md` citation section
8. Commit the DOI metadata update as a follow-up release metadata commit.

## Important Zenodo Behavior

- GitHub integration assigns DOI after the GitHub Release is created and processed.
- Zenodo GitHub integration does not support pre-reserving a DOI before the release.
- If a pre-reserved DOI is required before public release, use Zenodo manual upload instead of the GitHub integration.
