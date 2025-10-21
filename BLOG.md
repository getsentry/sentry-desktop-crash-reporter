# Sentry Desktop Crash Reporter: Engineering Story

When a native desktop application crashes, the process is terminated and the UI
vanishes along with it. The application might have been minimized, running on
another monitor, or otherwise out of sight when it crashed. How do you collect
meaningful [user feedback](https://docs.sentry.io/product/user-feedback/) in
this scenario?

This is the problem we set out to solve with
[Sentry Desktop Crash Reporter](https://github.com/getsentry/sentry-desktop-crash-reporter),
a cross-platform crash reporter for native desktop applications using the
[Sentry Native SDK](https://docs.sentry.io/platforms/native/).

![Sentry Desktop Crash Reporter](https://github.com/getsentry/sentry-desktop-crash-reporter/blob/main/.screenshots/all.png)

## The Challenge

Native crash reporting presents unique challenges compared to runtimes that can
capture errors on a higher level before the process is in a state that it can no
longer function. When a native application crashes, it is often too late to do
anything meaningful within the crashed process.

The solution? A **separate process** that outlives the crash.

## The Solution

The core idea is simple: when your application crashes, it spawns a fresh new
separate [detached process](https://github.com/getsentry/sentry-native/pull/1318)
to provide crash reporting UI in a safe environment.

This separate process:
- Receives crash data through an [envelope](https://develop.sentry.dev/sdk/envelopes/)
  file passed as a command-line argument
- Can present a user-friendly UI with crash details
- Can collect user feedback and additional context
- Submits the crash report to Sentry

## Technology Stack

[Uno Platform](https://platform.uno/) enables writing C# applications that run
natively and are easy to deploy on the desktop target platforms of this project:
Windows, macOS, and Linux.

TODO: ...

## Use Cases

While originally designed for user feedback, the external crash reporter enables
several scenarios:

1. **User Content**: Let users consent to sending crash reports
2. **User Feedback**: Collect context from users about what they were doing when
  the crash occurred
3. **Debugging**: During development, see crash details immediately by sending
  them straight to your text editor

## Try It Yourself

The crash reporter integrates with the
[Sentry Native SDK](https://docs.sentry.io/platforms/native/):

```c
sentry_options_t *options = sentry_options_new();
/* ... */
sentry_options_set_external_crash_reporter_path(options, "/path/to/Sentry.CrashReporter");
sentry_init(options);
```

Check out the [getsentry/sentry-desktop-crash-reporter](https://github.com/getsentry/sentry-desktop-crash-reporter)
repository to see it in action, or use it as a reference for building your own external crash reporter.

## References

### Issues

- Provide user-feedback capability to the Native SDK [getsentry/sentry-native#885](https://github.com/getsentry/sentry-native/issues/885)
- Feature Request: Support option to launch Crash Modal Dialogue to collect User Feedback at Crash Time [getsentry/sentry-native#1223](https://github.com/getsentry/sentry-native/issues/1223)

### PRs

- feat: external crash reporter [getsentry/sentry-native#1303](https://github.com/getsentry/sentry-native/pull/1303)
- feat: external crash reporter [getsentry/crashpad#131](https://github.com/getsentry/crashpad/pull/131)
- feat: add sentry__process_spawn() [getsentry/sentry-native#1318](https://github.com/getsentry/sentry-native/pull/1318)
