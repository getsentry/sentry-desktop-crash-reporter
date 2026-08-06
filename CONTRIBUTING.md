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

For same-repository PRs, add the `update-goldens` label to regenerate baselines in CI. Fork PRs are skipped because CI cannot push updated baselines to the fork branch.

Local golden captures require rasterization scale `1.0`; `make update-goldens` fails instead of updating baselines if the app reports a fractional scale. Golden comparisons use Magick.NET RMSE plus a cap on high-delta pixels.
