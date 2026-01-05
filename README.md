<p align="center">
  <a href="https://sentry.io/?utm_source=github&utm_medium=logo" target="_blank">
    <picture>
      <source srcset="https://sentry-brand.storage.googleapis.com/sentry-logo-white.png" media="(prefers-color-scheme: dark)" />
      <source srcset="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" media="(prefers-color-scheme: light), (prefers-color-scheme: no-preference)" />
      <img src="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" alt="Sentry" width="280">
    </picture>
  </a>
</p>

# Sentry Desktop Crash Reporter

[![CI](https://github.com/getsentry/sentry-desktop-crash-reporter/actions/workflows/ci.yml/badge.svg)](https://github.com/getsentry/sentry-desktop-crash-reporter/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/getsentry/sentry-desktop-crash-reporter/graph/badge.svg)](https://codecov.io/gh/getsentry/sentry-desktop-crash-reporter)

A reference implementation of an external crash reporter for desktop applications using the [Sentry Native SDK](https://docs.sentry.io/platforms/native/).

![Screenshots](.github/screenshots/all.png)

## Features

* **Cross-Platform:** Works on Windows, macOS, and Linux thanks to [.NET](https://dot.net) and the [Uno Platform](https://platform.uno/).
* **User Consent:** Gives the user explicit control over whether their crash data is sent.
* **User Feedback:** Allows users to add comments to the crash report.
* **Crash Information:** Displays crash details from the attached memory dump.
* **Attachments**: Allows users to preview crash report attachments, such as screenshots.

## Building

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Run [`uno-check`](https://platform.uno/docs/articles/uno-check.html) to verify and install additional requirements

### Development Build

```bash
dotnet build -f net9.0-desktop Sentry.CrashReporter/Sentry.CrashReporter.csproj
```

### Release Build

```bash
dotnet publish -f net9.0-desktop -r <RID> Sentry.CrashReporter/Sentry.CrashReporter.csproj
```

Replace `<RID>` with your target platform runtime identifier (e.g., `win-x64`, `osx-arm64`, `linux-x64`). See the [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) for more options.

### Running

```bash
dotnet run -p Sentry.CrashReporter/Sentry.CrashReporter.csproj -f net9.0-desktop
```

## Usage

```c
sentry_options_t *options = sentry_options_new();
sentry_options_set_external_crash_reporter_path(options, "/path/to/Sentry.CrashReporter");
/* ... */
sentry_init(options);
```
