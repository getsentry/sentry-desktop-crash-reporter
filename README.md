# Desktop for crash report submission

[![CI](https://github.com/getsentry/sentry-desktop-crash-reporter/actions/workflows/ci.yml/badge.svg)](https://github.com/getsentry/sentry-desktop-crash-reporter/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/getsentry/sentry-desktop-crash-reporter/graph/badge.svg)](https://codecov.io/gh/getsentry/sentry-desktop-crash-reporter)

This app allows:

* Taking over the crash the crash file from [an SDK such as sentry-native](https://github.com/getsentry/sentry-native/pull/1303) to request user feedback
* It has access to crash information including crash reason from a minidump
* Allows control over user consent, implicitly, so the user can choose to submit or not the crash report
