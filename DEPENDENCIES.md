# Key Dependencies

## NuGet Packages (Auto-installed via dotnet restore)

- **NModbus** (v3.0.166) - Modbus RTU protocol implementation
- **OxyPlot.Wpf** (v2.1.2) - Real-time graphing for WPF

## Built-in .NET Libraries (No Installation Needed)

- **System.IO.Ports** - Serial port communication
- **System.Windows** (WPF) - UI framework
- **System.Threading.Tasks** - Async operations

## Optional: External Requirements

### USB-RS485 Driver
Required only if your USB adapter isn't recognized. Download for your chip:
- CH340: https://www.wch.cn/download/ch341ser_exe.zip
- CP2102: https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers
- FTDI: https://ftdichip.com/drivers/d2xx/

---

The application itself requires NO additional installations—just plug in your USB-RS485 adapter and run!
