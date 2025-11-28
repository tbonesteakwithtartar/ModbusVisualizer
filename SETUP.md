## Setup Instructions

### Prerequisites
- Windows 10 or later
- USB-RS485 adapter (with CH340, CP2102, or similar chip)
- .NET 6.0 Runtime (or SDK if building from source)

### Build and Run

```powershell
# Clone the repository
git clone https://github.com/yourusername/ModbusVisualizer.git
cd ModbusVisualizer/ModbusVisualizer

# Restore NuGet packages
dotnet restore

# Build
dotnet build --configuration Release

# Run
dotnet run --configuration Release
```

### Create Standalone Executable

```powershell
# Publish as self-contained executable (no .NET runtime needed)
dotnet publish --configuration Release --self-contained --runtime win-x64 --output ./bin/publish
```

This creates a standalone `.exe` in `bin/publish/` that users can run without installing .NET.

### USB-RS485 Driver Installation

If your device isn't detected:
1. Download drivers for your adapter chip:
   - CH340: https://www.wch.cn/download/ch341ser_exe.zip
   - CP2102: https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers
   - FTDI: https://ftdichip.com/drivers/d2xx/

2. Install and restart your computer
3. Check Device Manager for COM port assignment

### Hardware Wiring

Connect your USB-RS485 adapter to your Modbus device:

```
Adapter    Device
   D+  ──→  A/+ (Data High)
   D-  ──→  B/- (Data Low)
   GND ──→  GND (Ground)
```

For multi-drop Modbus networks, use proper termination (120Ω resistor between D+ and D-).

### First Run

1. Plug in your USB-RS485 adapter
2. Open ModbusVisualizer.exe
3. The COM port should auto-populate in the dropdown
4. Set the correct **Baud Rate** (check your device manual)
5. Set the correct **Slave ID** (usually 1, check device settings)
6. Click **Connect**

---

For troubleshooting, see README.md.
