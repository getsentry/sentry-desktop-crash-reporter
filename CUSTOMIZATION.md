# Customization

This guide explains how to rebrand the crash reporter with your own logo, colors,
and window title.

<img src=".github/screenshots/screenshot-custom.png" alt="Custom crash reporter" width="600">

## Logo / Icon

All paths below are relative to `Sentry.CrashReporter/`.

### Header logo

The header logo is displayed in the top-right corner of the main view
(`Views/HeaderView.cs:43-47`).

Replace the following files:

| File | Description |
|------|-------------|
| `Assets/AppLogo.light.png` | Light theme (1x) |
| `Assets/AppLogo.light.scale-200.png` | Light theme (2x) |
| `Assets/AppLogo.dark.png` | Dark theme (1x) |
| `Assets/AppLogo.dark.scale-200.png` | Dark theme (2x) |
| `Assets/AppLogo.png` | Fallback (1x) |
| `Assets/AppLogo.scale-200.png` | Fallback (2x) |

The theme-to-file mapping is defined in `Styles/Images.xaml` via the
`AppLogoIcon` resource key.

### Window / taskbar icon

Replace `Assets/Icons/icon.svg`. This is set in `App.xaml.cs:115` via
`SetWindowIcon()`.

### Multi-scale conventions

See `Assets/SharedAssets.md` for naming conventions and the scale-to-platform
mapping table (e.g. `scale-100` = iOS @1x, `scale-200` = iOS @2x / Android
xhdpi).

## Colors

All color resources are defined in `App.xaml` inside the `<ResourceDictionary>`.

### Accent color

The primary brand color used for buttons and highlights (`App.xaml:13-19`):

```xml
<Color x:Key="SystemAccentColor">#8866FF</Color>
<Color x:Key="SystemAccentColorLight1">#9C7FFF</Color>
<Color x:Key="SystemAccentColorLight2">#7554FF</Color>
<Color x:Key="SystemAccentColorLight3">#B08CFF</Color>
<Color x:Key="SystemAccentColorDark1">#7554FF</Color>
<Color x:Key="SystemAccentColorDark2">#5A3FCC</Color>
<Color x:Key="SystemAccentColorDark3">#3F2B99</Color>
```

Replace `#8866FF` with your brand color and adjust the Light/Dark variants
accordingly.

### Accent button foreground

The text color on accent-colored buttons (`App.xaml:22-25`):

```xml
<SolidColorBrush x:Key="AccentButtonForeground" Color="#FFFFFF" />
<SolidColorBrush x:Key="AccentButtonForegroundPointerOver" Color="#FFFFFF" />
<SolidColorBrush x:Key="AccentButtonForegroundPressed" Color="#FFFFFF" Opacity="0.9" />
<SolidColorBrush x:Key="AccentButtonForegroundDisabled" Color="#FFFFFF" Opacity="0.75" />
```

## Window Title & Header Text

Both are defined as string resources in `App.xaml`:

```xml
<x:String x:Key="WindowTitle">Sentry Crash Reporter</x:String>
<x:String x:Key="HeaderText">Report a Bug</x:String>
```
