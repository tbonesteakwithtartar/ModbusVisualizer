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
        private LineSeries _lineSeries;
        private int _dataPointCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            InitializePlot();
            PopulateCOMPorts();
        }

        private void InitializePlot()
        {
            _plotModel = new PlotModel { Title = "Modbus Data Visualization" };
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Bottom, Title = "Time (s)" });
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Title = "Value" });

            _lineSeries = new LineSeries { Title = "Register Value" };
            _plotModel.Series.Add(_lineSeries);

            PlotView.Model = _plotModel;
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
                ComPortCombo.IsEnabled = false;
                BaudRateCombo.IsEnabled = false;
                SlaveIdTextBox.IsEnabled = false;

                _cancellationTokenSource = new CancellationTokenSource();
                _ = ReadModbusDataAsync(slaveId, _cancellationTokenSource.Token);
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
                ComPortCombo.IsEnabled = true;
                BaudRateCombo.IsEnabled = true;
                SlaveIdTextBox.IsEnabled = true;
                ErrorLabel.Text = "Disconnected";
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}";
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
                            _dataPointCount++;
                            DataCountLabel.Text = $"Data points: {_dataPointCount}";
                            PlotView.InvalidatePlot();
                        }

                        // Update Coils ListBox
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
    }
}
