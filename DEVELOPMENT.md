## Modbus Visualizer Development Guide

### Project Structure

```
ModbusVisualizer/
├── ModbusVisualizer.csproj         # Project configuration & NuGet packages
├── App.xaml / App.xaml.cs          # Application entry point
├── MainWindow.xaml / MainWindow.xaml.cs  # Main UI and logic
├── README.md                        # User documentation
└── SETUP.md                         # Setup and installation guide
```

### Key Classes

#### MainWindow.xaml.cs
- **`InitializePlot()`**: Sets up OxyPlot graph with axes
- **`PopulateCOMPorts()`**: Detects available serial ports
- **`ConnectButton_Click()`**: Initiates Modbus connection
- **`ReadModbusDataAsync()`**: Main polling loop for register reads

### Modifying Register Reads

Edit the `ReadModbusDataAsync()` method to customize:

```csharp
// Current setup (example):
var holdingRegisters = await Task.Run(() => _master.ReadHoldingRegisters(slaveId, 0, 10));
// This reads 10 registers starting at address 0

// Change to:
var holdingRegisters = await Task.Run(() => _master.ReadHoldingRegisters(slaveId, 100, 5));
// Now reads 5 registers starting at address 100
```

### Modbus Functions Available

Through NModbus library:
- `ReadCoils()` - Read digital inputs (read-only)
- `ReadDiscreteInputs()` - Read discrete inputs
- `ReadHoldingRegisters()` - Read analog values (read/write)
- `ReadInputRegisters()` - Read input registers (read-only)
- `WriteSingleCoil()` - Write a single coil
- `WriteSingleRegister()` - Write a single register
- `WriteMultipleCoils()` - Write multiple coils
- `WriteMultipleRegisters()` - Write multiple registers

### Changing Update Interval

In `ReadModbusDataAsync()`, find:
```csharp
await Task.Delay(500, cancellationToken); // Update every 500ms
```

Adjust 500 to desired milliseconds (e.g., 1000 for 1 second).

### Adding More Registers to Display

1. Add new `ListBox` in `MainWindow.xaml`
2. Add corresponding read in `ReadModbusDataAsync()`
3. Update the ListBox in the `Dispatcher.Invoke()` block

Example:
```xaml
<!-- In XAML -->
<Label Content="Discrete (1x):" FontWeight="Bold"/>
<ListBox Name="DiscreteListBox" Height="100"/>
```

```csharp
// In C#
var discreteInputs = await Task.Run(() => _master.ReadDiscreteInputs(slaveId, 0, 8));
DiscreteListBox.Items.Clear();
for (int i = 0; i < discreteInputs.Length; i++)
    DiscreteListBox.Items.Add($"DI {i}: {(discreteInputs[i] ? "ON" : "OFF")}");
```

### Building Release Executable

```powershell
# Self-contained executable (no .NET runtime needed)
dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true

# Output: bin\Release\net6.0-windows\win-x64\publish\ModbusVisualizer.exe
```

### Dependencies

- **NModbus**: Modbus protocol implementation
- **OxyPlot.Wpf**: Real-time graphing
- **System.IO.Ports**: Serial port communication (built-in)

All included in `ModbusVisualizer.csproj` via NuGet.

---

Questions? Check the main README.md or review the inline code comments.
