# Contributing

## Goldens

Golden baselines live in `tests/goldens/<RID>-<view>-<theme>.png`.

```bash
make test-golden
```

`make test-golden` verifies every committed baseline for the current RID. Use `RID=<rid>` to target a different RID, `VIEW=<view>` or `THEME=<theme>` to narrow the set, and `FIXTURE=<path>` only when overriding the manifest's per-view fixture defaults.

For same-repository PRs, add the `update-goldens` label to regenerate baselines in CI. Fork PRs are skipped because CI cannot push updated baselines to the fork branch.

For local fallback updates, `make update-goldens` uses the same RID, view, theme, fixture, and output overrides as `make test-golden`.

Golden captures require rasterization scale `1.0`; the update workflow fails instead of updating baselines if the app reports a fractional scale. Golden comparisons use Magick.NET RMSE plus a cap on high-delta pixels.
