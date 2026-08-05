# Contributing

## Goldens

Golden baselines live in `tests/goldens/<RID>-<view>-<theme>.png`.

```bash
make test-golden
make update-goldens
make update-goldens VIEW=stacktrace
make update-goldens THEME=dark
```

`make update-goldens` publishes a golden build, captures every view/theme combination listed in `tests/goldens/views.json`, and updates those baselines for the current RID. `make test-golden` verifies every committed baseline for the current RID. Use `RID=<rid>` to target a different RID, `VIEW=<view>` or `THEME=<theme>` to narrow the set, and `FIXTURE=<path>` only when overriding the manifest's per-view fixture defaults.

Golden comparisons use Magick.NET RMSE after a small blur so minor font rendering differences are tolerated.
