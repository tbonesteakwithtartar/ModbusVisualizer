# Modbus Visualizer

A native Windows desktop application for reading Modbus data from USB-RS485 serial connections and displaying real-time visualizations.

## Features

- **USB-RS485 Communication**: Direct serial port connection without external drivers
- **Real-time Graphing**: Live visualization of Modbus register values using OxyPlot
- **Multi-register Support**: View coils, holding registers, and input registers simultaneously
- **No External Dependencies**: Standalone Windows executable (NET 6.0)
- **User-Friendly GUI**: Simple interface to connect, configure, and monitor Modbus devices

## Requirements

- Windows 10 or newer
- USB-RS485 serial adapter
- .NET 6.0 Runtime (included in installer)

## Installation

### Option 1: Download Executable
Download the compiled `.exe` from the Releases page and run directly.

### Option 2: Build from Source
1. Install [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
2. Clone this repository
3. Run: `dotnet build --configuration Release`
4. Run: `dotnet publish --configuration Release --output ./bin/publish`

## Usage

1. **Connect USB-RS485 Adapter** to your computer
2. **Open ModbusVisualizer.exe**
3. **Select COM Port** from dropdown (auto-detected)
4. **Set Baud Rate** (default 19200)
5. **Enter Slave ID** (default 1)
6. **Click Connect**
7. Watch real-time data updates in the graph and register lists

## Register Map

- **Coils (0x)**: Digital outputs (ON/OFF)
- **Holding Registers (4x)**: Read/write analog values
- **Input Registers (3x)**: Read-only analog values

Currently reads:
- First 10 Holding Registers (starting at address 0)
- First 8 Coils (starting at address 0)
- First 10 Input Registers (starting at address 0)

Modify these in `MainWindow.xaml.cs` to match your device.

## Customization

Edit `ReadModbusDataAsync()` in `MainWindow.xaml.cs` to:
- Change polling interval (default 500ms)
- Modify register addresses and quantities
- Add new register types

## Architecture

- **NModbus**: Modbus RTU protocol implementation
- **OxyPlot**: Real-time graphing library
- **WPF**: Windows Presentation Foundation for UI

## Hardware Setup

```
USB-RS485 Adapter
  ├─ D+ (Data+) → A/+ on Modbus device
  ├─ D- (Data-) → B/- on Modbus device
  └─ GND → GND
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "No ports found" | Check if USB adapter is connected and drivers installed |
| Connection fails | Verify baud rate matches your device (check manual) |
| No data | Confirm device is powered and Modbus address (Slave ID) is correct |
| Slow updates | Increase interval in code or reduce number of registers |

## License

MIT

---

Made for custom industrial monitoring and visualization.
