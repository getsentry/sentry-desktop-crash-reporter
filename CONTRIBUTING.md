# Contributing

## Goldens

Golden baselines live in `tests/goldens/<RID>-<view>-<theme>.png`.

### Verify baselines locally

```bash
make test-golden
```

`make test-golden` verifies every committed baseline for the current RID. Use `RID=<rid>` to target a different RID, `VIEW=<view>` or `THEME=<theme>` to narrow the set, and `FIXTURE=<path>` only when overriding the manifest's per-view fixture defaults.

### Update baselines in CI

For same-repository PRs, prefer updating baselines in CI so that every supported platform is regenerated:

1. Add the `update-goldens` label to the PR.
2. Wait for the `Update Goldens` workflow to regenerate the Linux, macOS, and Windows baselines.
3. If any baselines changed, review the `test(goldens): update baselines` commit that CI pushes to the PR branch.
4. Wait for the regular golden tests to pass against any generated commit.

A follow-up workflow removes the label after the update run finishes, whether it succeeds or fails. If an update fails, inspect the `update-golden-results-<RID>` artifacts, fix the failure, and add the label again to retry.

Fork PRs are skipped because CI cannot push updated baselines to the fork branch.

### Update baselines locally

`make update-goldens` updates the selected RID locally and is useful for focused changes, fork PRs, or debugging captures. When practical, prefer the CI update so that every supported platform is regenerated consistently. The command uses the same RID, view, theme, fixture, and output overrides as `make test-golden`.

All golden captures, including `make test-golden`, local fallback updates, and CI workflows, require rasterization scale `1.0`. They fail before comparing or updating baselines if the app reports a fractional scale. Golden comparisons use Magick.NET RMSE plus a cap on high-delta pixels.
