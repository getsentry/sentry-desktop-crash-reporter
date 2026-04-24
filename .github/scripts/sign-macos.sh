#!/bin/bash
set -euo pipefail

# Sign and notarize a macOS .app bundle.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENTITLEMENTS="$SCRIPT_DIR/../../Sentry.CrashReporter/Platforms/Desktop/entitlements.plist"

APP=""
SIGNING_IDENTITY=""
API_KEY_PATH=""
API_KEY_ID=""
API_ISSUER_ID=""

usage() {
    echo "Usage: $0"
    echo "  -a|--app <path>              Path to the .app bundle"
    echo "  -s|--signing-identity <id>   Developer ID Application identity (keychain CN)"
    echo "  -k|--api-key-path <path>     Path to App Store Connect API key .p8 file"
    echo "  -i|--api-key-id <id>         App Store Connect API Key ID"
    echo "  -I|--api-issuer-id <uuid>    App Store Connect API Issuer ID"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case $1 in
        -a|--app)
            APP="$2"
            shift 2
            ;;
        -s|--signing-identity)
            SIGNING_IDENTITY="$2"
            shift 2
            ;;
        -k|--api-key-path)
            API_KEY_PATH="$2"
            shift 2
            ;;
        -i|--api-key-id)
            API_KEY_ID="$2"
            shift 2
            ;;
        -I|--api-issuer-id)
            API_ISSUER_ID="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            usage
            ;;
    esac
done

if [ -z "$APP" ] || [ -z "$SIGNING_IDENTITY" ] || [ -z "$API_KEY_PATH" ] || [ -z "$API_KEY_ID" ] || [ -z "$API_ISSUER_ID" ]; then
    usage
fi

# Strip extended attributes that can trip up codesign.
xattr -rc "$APP"

# codesign --deep signs Contents/MacOS/ but does not recurse into
# Contents/Resources/, where the Uno publish drops a duplicate apphost.
# Sign Mach-O files under Resources/ explicitly, then let --deep handle
# the bundle's main executable and dylibs.
if [ -d "$APP/Contents/Resources" ]; then
    while IFS= read -r -d '' f; do
        if file -b "$f" | grep -q "Mach-O"; then
            codesign --force --options runtime --timestamp --sign "$SIGNING_IDENTITY" "$f"
        fi
    done < <(find "$APP/Contents/Resources" -type f -print0)
fi

codesign --force --deep --options runtime --timestamp --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP"
codesign --verify --deep --strict --verbose=2 "$APP"

ZIP="$(dirname "$APP")/.notarize.zip"
trap 'rm -f "$ZIP"' EXIT
ditto -c -k --keepParent "$APP" "$ZIP"

SUBMIT=$(xcrun notarytool submit "$ZIP" \
    --key "$API_KEY_PATH" \
    --key-id "$API_KEY_ID" \
    --issuer "$API_ISSUER_ID" \
    --output-format json \
    --wait)
echo "$SUBMIT"

ID=$(echo "$SUBMIT" | jq -r '.id')
STATUS=$(echo "$SUBMIT" | jq -r '.status')

if [ "$STATUS" != "Accepted" ]; then
    echo "::group::Notary log for $ID"
    xcrun notarytool log "$ID" \
        --key "$API_KEY_PATH" \
        --key-id "$API_KEY_ID" \
        --issuer "$API_ISSUER_ID"
    echo "::endgroup::"
    exit 1
fi

xcrun stapler staple "$APP"
