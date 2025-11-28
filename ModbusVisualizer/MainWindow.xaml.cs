using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NModbus;
using NModbus.Serial;
using OxyPlot;
using OxyPlot.Series;

namespace ModbusVisualizer
{
    public partial class MainWindow : Window
    {
        private IModbusSerialMaster _master;
        private SerialPort _serialPort;
        private CancellationTokenSource _cancellationTokenSource;
        private PlotModel _plotModel;
        private PlotModel _barChartModel;
        private LineSeries _lineSeries;
        private int _dataPointCount = 0;
        private List<ushort> _registerHistory = new List<ushort>();
        private Dictionary<int, (double min, double max, double avg)> _registerStats = new Dictionary<int, (double, double, double)>();
        private bool _isLogging = false;
        private List<(DateTime timestamp, ushort[] values)> _loggedData = new List<(DateTime, ushort[])>();

        public MainWindow()
        {
            InitializeComponent();
            InitializePlot();
            PopulateCOMPorts();
        }

        private void InitializePlot()
        {
            // Time Series Plot
            _plotModel = new PlotModel { Title = "Modbus Data Visualization" };
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Bottom, Title = "Time (s)" });
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Title = "Value" });

            _lineSeries = new LineSeries { Title = "Register Value" };
            _plotModel.Series.Add(_lineSeries);

            PlotView.Model = _plotModel;

            // Bar Chart Plot for Last Values
            _barChartModel = new PlotModel { Title = "Last Register Values" };
            _barChartModel.Axes.Add(new OxyPlot.Axes.CategoryAxis { Position = OxyPlot.Axes.AxisPosition.Bottom, Title = "Register" });
            _barChartModel.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Title = "Value" });

            BarChartView.Model = _barChartModel;
        }

        private void PopulateCOMPorts()
        {
            var ports = SerialPort.GetPortNames();
            ComPortCombo.ItemsSource = ports.Length > 0 ? ports : new[] { "No ports found" };
            if (ports.Length > 0)
                ComPortCombo.SelectedIndex = 0;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string comPort = ComPortCombo.SelectedItem?.ToString();
                int baudRate = int.Parse(BaudRateCombo.SelectedItem?.ToString() ?? "19200");
                byte slaveId = byte.Parse(SlaveIdTextBox.Text);

                if (string.IsNullOrEmpty(comPort) || comPort == "No ports found")
                {
                    MessageBox.Show("Please select a valid COM port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _serialPort = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.One) { WriteTimeout = 2000, ReadTimeout = 2000 };
                _serialPort.Open();

                var factory = new ModbusSerialFactory();
                _master = factory.CreateMaster(_serialPort);

                StatusLabel.Content = $"Connected to {comPort}";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Green;
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                WriteSingleButton.IsEnabled = true;
                WriteCoilButton.IsEnabled = true;
                ComPortCombo.IsEnabled = false;
                BaudRateCombo.IsEnabled = false;
                SlaveIdTextBox.IsEnabled = false;

                _cancellationTokenSource = new CancellationTokenSource();
                _ = ReadModbusDataAsync(slaveId, _cancellationTokenSource.Token);
                LoggingButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _master?.Dispose();
                _serialPort?.Close();
                _serialPort?.Dispose();

                StatusLabel.Content = "Disconnected";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                WriteSingleButton.IsEnabled = false;
                WriteCoilButton.IsEnabled = false;
                LoggingButton.IsEnabled = false;
                ExportButton.IsEnabled = _loggedData.Count > 0;
                ComPortCombo.IsEnabled = true;
                BaudRateCombo.IsEnabled = true;
                SlaveIdTextBox.IsEnabled = true;
                ErrorLabel.Text = "Disconnected";
                _isLogging = false;
                if (LoggingButton.Content.ToString() == "Stop Logging")
                    LoggingButton.Content = "Start Logging";
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void WriteSingleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ushort.TryParse(WriteAddressTextBox.Text, out ushort address) ||
                    !ushort.TryParse(WriteValueTextBox.Text, out ushort value) ||
                    !byte.TryParse(SlaveIdTextBox.Text, out byte slaveId))
                {
                    WriteStatusLabel.Text = "Invalid input";
                    WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Run(() => _master.WriteSingleRegister(slaveId, address, value));
                        Dispatcher.Invoke(() =>
                        {
                            WriteStatusLabel.Text = $"Wrote {value} to address {address}";
                            WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Green;
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            WriteStatusLabel.Text = $"Write failed: {ex.Message}";
                            WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                WriteStatusLabel.Text = $"Error: {ex.Message}";
                WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void WriteCoilButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ushort.TryParse(WriteAddressTextBox.Text, out ushort address) ||
                    !byte.TryParse(SlaveIdTextBox.Text, out byte slaveId))
                {
                    WriteStatusLabel.Text = "Invalid input";
                    WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                bool coilValue = !string.IsNullOrEmpty(WriteValueTextBox.Text) &&
                                 (WriteValueTextBox.Text == "1" || WriteValueTextBox.Text.ToLower() == "true");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Run(() => _master.WriteSingleCoil(slaveId, address, coilValue));
                        Dispatcher.Invoke(() =>
                        {
                            WriteStatusLabel.Text = $"Coil {address} set to {(coilValue ? "ON" : "OFF")}";
                            WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Green;
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            WriteStatusLabel.Text = $"Write failed: {ex.Message}";
                            WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                WriteStatusLabel.Text = $"Error: {ex.Message}";
                WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private async Task ReadModbusDataAsync(byte slaveId, CancellationToken cancellationToken)
        {
            int timeCounter = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Read Holding Registers (4x - address 0, count 10)
                    var holdingRegisters = await Task.Run(() => _master.ReadHoldingRegisters(slaveId, 0, 10), cancellationToken);
                    
                    // Read Coils (0x - address 0, count 8)
                    var coils = await Task.Run(() => _master.ReadCoils(slaveId, 0, 8), cancellationToken);
                    
                    // Read Input Registers (3x - address 0, count 10)
                    var inputRegisters = await Task.Run(() => _master.ReadInputRegisters(slaveId, 0, 10), cancellationToken);

                    Dispatcher.Invoke(() =>
                    {
                        // Update graph with first holding register
                        if (holdingRegisters.Length > 0)
                        {
                            _lineSeries.Points.Add(new DataPoint(timeCounter, holdingRegisters[0]));
                            _registerHistory.Add(holdingRegisters[0]);
                            _dataPointCount++;
                            DataCountLabel.Text = $"Data points: {_dataPointCount}";
                            PlotView.InvalidatePlot();

                            // Update bar chart with current holding register values
                            UpdateBarChart(holdingRegisters);

                            // Update statistics
                            UpdateStatistics(holdingRegisters);

                            // Log data if logging is enabled
                            if (_isLogging)
                            {
                                _loggedData.Add((DateTime.Now, holdingRegisters));
                            }
                        }
                        CoilsListBox.Items.Clear();
                        for (int i = 0; i < coils.Length; i++)
                            CoilsListBox.Items.Add($"Coil {i}: {(coils[i] ? "ON" : "OFF")}");

                        // Update Holding Registers ListBox
                        HoldingListBox.Items.Clear();
                        for (int i = 0; i < holdingRegisters.Length; i++)
                            HoldingListBox.Items.Add($"Reg {i}: {holdingRegisters[i]}");

                        // Update Input Registers ListBox
                        InputsListBox.Items.Clear();
                        for (int i = 0; i < inputRegisters.Length; i++)
                            InputsListBox.Items.Add($"In {i}: {inputRegisters[i]}");

                        ErrorLabel.Text = "Connected";
                        ErrorLabel.Foreground = System.Windows.Media.Brushes.Green;
                    });

                    timeCounter++;
                    await Task.Delay(500, cancellationToken); // Update every 500ms
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ErrorLabel.Text = $"Read Error: {ex.Message}";
                        ErrorLabel.Foreground = System.Windows.Media.Brushes.Red;
                    });
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private void UpdateBarChart(ushort[] registerValues)
        {
            var barSeries = new OxyPlot.Series.BarSeries();
            _barChartModel.Series.Clear();

            var categoryAxis = _barChartModel.Axes[0] as OxyPlot.Axes.CategoryAxis;
            categoryAxis.Labels.Clear();

            for (int i = 0; i < Math.Min(registerValues.Length, 10); i++)
            {
                categoryAxis.Labels.Add($"R{i}");
                barSeries.Items.Add(new OxyPlot.Series.BarItem { Value = registerValues[i] });
            }

            _barChartModel.Series.Add(barSeries);
            BarChartView.InvalidatePlot();
        }

        private void UpdateStatistics(ushort[] registerValues)
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("Register Statistics:");
            stats.AppendLine(new string('=', 40));

            for (int i = 0; i < Math.Min(registerValues.Length, 10); i++)
            {
                double value = registerValues[i];
                stats.AppendLine($"Register {i}:");
                stats.AppendLine($"  Current: {value}");

                // Calculate rolling min/max/avg
                if (_registerHistory.Count > 0)
                {
                    var lastN = _registerHistory.TakeLast(Math.Min(100, _registerHistory.Count)).ToList();
                    double min = lastN.Min();
                    double max = lastN.Max();
                    double avg = lastN.Average();
                    stats.AppendLine($"  Min: {min}, Max: {max}, Avg: {avg:F2}");
                }
                stats.AppendLine();
            }

            StatsTextBlock.Text = stats.ToString();
        }

        private void LoggingButton_Click(object sender, RoutedEventArgs e)
        {
            _isLogging = !_isLogging;
            LoggingButton.Content = _isLogging ? "Stop Logging" : "Start Logging";
            ExportButton.IsEnabled = _isLogging || _loggedData.Count > 0;

            if (_isLogging)
            {
                _loggedData.Clear();
                WriteStatusLabel.Text = "Logging started...";
                WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Blue;
            }
            else
            {
                WriteStatusLabel.Text = $"Logging stopped. {_loggedData.Count} entries recorded.";
                WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loggedData.Count == 0)
            {
                MessageBox.Show("No data to export", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"modbus_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    using (var writer = new System.IO.StreamWriter(dialog.FileName))
                    {
                        // Write header
                        writer.WriteLine("Timestamp," + string.Join(",", Enumerable.Range(0, 10).Select(i => $"Register_{i}")));

                        // Write data
                        foreach (var (timestamp, values) in _loggedData)
                        {
                            var line = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff") + "," + 
                                      string.Join(",", values.Take(10));
                            writer.WriteLine(line);
                        }
                    }

                    WriteStatusLabel.Text = $"Exported {_loggedData.Count} entries to {System.IO.Path.GetFileName(dialog.FileName)}";
                    WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Green;
                }
                catch (Exception ex)
                {
                    WriteStatusLabel.Text = $"Export failed: {ex.Message}";
                    WriteStatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
        }
