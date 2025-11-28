#!/bin/bash
# Build script for ModbusVisualizer - Creates standalone executables

set -e

echo "========================================="
echo "ModbusVisualizer Build Script"
echo "========================================="

# Configuration
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/ModbusVisualizer"
BUILD_DIR="$PROJECT_DIR/../bin/publish"
RUNTIME="win-x64"

echo ""
echo "Project Directory: $PROJECT_DIR"
echo "Build Output: $BUILD_DIR"
echo ""

# Clean previous build
echo "Cleaning previous build..."
rm -rf "$BUILD_DIR"

# Restore dependencies
echo "Restoring NuGet packages..."
cd "$PROJECT_DIR"
dotnet restore

# Build Release
echo "Building Release configuration..."
dotnet build --configuration Release --no-restore

# Publish as self-contained single-file executable
echo "Publishing self-contained executable..."
dotnet publish \
  --configuration Release \
  --runtime $RUNTIME \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  --output "$BUILD_DIR"

echo ""
echo "========================================="
echo "Build Complete!"
echo "========================================="
echo ""
echo "Executable Location:"
echo "  $BUILD_DIR/ModbusVisualizer.exe"
echo ""
echo "File Size: $(du -h $BUILD_DIR/ModbusVisualizer.exe | cut -f1)"
echo ""
echo "To run:"
echo "  $BUILD_DIR/ModbusVisualizer.exe"
echo ""
