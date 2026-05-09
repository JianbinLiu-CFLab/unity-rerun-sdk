# Publisher Components

Publisher components let you record Rerun data through the Unity Inspector — no code required.

## RerunManager

The central hub. One per scene, typically on a dedicated `Rerun` GameObject.

| Field | Description |
|-------|-------------|
| Application Id | Name shown in Rerun Viewer |
| Output Mode | FileOnly / LiveOnly / FileAndLive |
| Output Path | `.rrd` file path. Use `{TIMESTAMP}` for auto-naming |
| Record On Start | Start recording automatically on Play |
| Write View Coordinates | Write `RIGHT_HAND_Y_UP` on `world` entity |

## RerunTransformPublisher

Publishes a Transform's position and rotation to `world/<entity>`.

| Field | Description |
|-------|-------------|
| Target | Transform to publish. Empty = self |
| Entity Path | Custom path. Empty = auto from GameObject |
| Publish Rate Hz | 0 = every frame, 10 = 10 Hz |

## RerunScalarPublisher

Publishes a numeric value to `<entity>`.

| Field | Description |
|-------|-------------|
| Source | Built-in data source (Fps, DeltaTime, TransformPosition, Constant, etc.) |
| Constant Value | Value when source is Constant |
| Target | Transform for position/rotation sources |

## RerunTextLogPublisher

Publishes a text log entry to `logs/<entity>`.

| Field | Description |
|-------|-------------|
| Message | Log body text |
| Level | INFO, WARN, ERROR, DEBUG, TRACE |
| Repeat | Publish continuously (false = one-shot) |
| Append Frame Count | Add frame number to message |
