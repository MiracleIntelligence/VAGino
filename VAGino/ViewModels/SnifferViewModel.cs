using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using MessageAnalyzer;
using MessageAnalyzer.Models;

using Microsoft.Toolkit.Helpers;

using ReadlnLibrary.Core.Collections;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using VAGino.Models;
using VAGino.Services;

using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace VAGino.ViewModels
{
    public class SnifferViewModel : ViewModelBase
    {
        private const string STATUS_CONNECTED = "Connected to VAGino device";
        private const string STATUS_NOT_CONNECTED = "Not connected";

        public ICommand ClearCommand { get; }
        public ICommand AddTestRowCommand { get; }
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; }
        public ICommand SendRawCommand { get; }
        public ICommand UpdateDataCommand { get; }
        public ICommand DeleteFilterCommand { get; }
        public ICommand AddFilterCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public GroupedObservableCollection<string, MessageRow> Messages { get; private set; }
        public Filters Filters { get; private set; }
        public string StatusText { get; set; }

        private SerialConnectionBroker _serialBroker;
        private ConnectionService _connectionService;

        public int BatteryLevel { get; private set; }

        private bool _started;

        private Dictionary<string, int> _messages;
        private Dictionary<string, int> _newMessages;
        private DBService _dbService;

        public ObservableCollection<string> LogLines { get; }

        public SnifferViewModel()
        {
            _messages = new Dictionary<string, int>();
            _newMessages = new Dictionary<string, int>();
            _dbService = Singleton<DBService>.Instance;
            Filters = Singleton<Filters>.Instance;

            LoadData();

            _serialBroker = new SerialConnectionBroker();
            _serialBroker.TextRead += OnSerialTextRead;
            _serialBroker.MessageReceived += OnSerialMessageReceived;

            _connectionService = new ConnectionService();
            _connectionService.DeviceAdded += OnDevicesChanged;
            _connectionService.DeviceRemoved += OnDevicesChanged;

            ClearCommand = new RelayCommand(ClearGroups);
            AddTestRowCommand = new RelayCommand(AddTestRow);
            StartCommand = new RelayCommand(OnStartTracking, () => !_started && EventHandlerForDevice.Current.Device != null);
            StopCommand = new RelayCommand(OnStopTracking, () => _started);
            UpdateDataCommand = new RelayCommand(RequestData, () => (EventHandlerForDevice.Current.Device != null));
            DeleteFilterCommand = new RelayCommand<string>(DeleteFilter);
            AddFilterCommand = new RelayCommand<string>(AddFilter);
            ConnectCommand = new RelayCommand(Connect, () => (EventHandlerForDevice.Current.Device == null && _connectionService.Devices.Count > 0));
            DisconnectCommand = new RelayCommand(Disconnect, () => (EventHandlerForDevice.Current.Device != null));
            SendRawCommand = new RelayCommand<string>(SendRawAsync, (rawCommand) => (!String.IsNullOrEmpty(rawCommand)));


            Messages = new GroupedObservableCollection<string, MessageRow>((m) => m.Id);
            LogLines = new ObservableCollection<string>();
            SetStatus(STATUS_NOT_CONNECTED);
        }

        private void LoadData()
        {
            var allBlocks = _dbService.Connection.Table<VAGBlock>().ToList();
            var allFilters = _dbService.Connection.Table<FilteredBlock>().ToList();

            Filters.Init();
        }

        private async void SendRawAsync(string rawCommand)
        {
            int count = 1;
            if (!String.IsNullOrEmpty(rawCommand))
            {
                var elements = rawCommand.Split(':');
                if (elements.Length > 1)
                {
                    count = Int32.Parse(elements[1]);
                }
                for (int i = 0; i < count; ++i)
                {
                    await SendCommand(elements[0]);
                    await Task.Delay(5);
                }
            }
        }

        private async void OnDevicesChanged(object sender, DeviceListEntry e)
        {
            await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                RaisePropertyChanged(nameof(ConnectCommand));
                RaisePropertyChanged(nameof(DisconnectCommand));
            }));
        }

        private async void Connect()
        {
            if (_connectionService.Devices.Count > 0)
            {
                await _connectionService.ConnectToDevice(_connectionService.Devices[0]);

                if (EventHandlerForDevice.Current.Device != null)
                {
                    InitializeDevice();
                    OnStartTracking();
                }

                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    RaisePropertyChanged(nameof(ConnectCommand));
                    RaisePropertyChanged(nameof(DisconnectCommand));
                    RaisePropertyChanged(nameof(UpdateDataCommand));

                    SetStatus(STATUS_CONNECTED);

                }));
            }
        }

        private void SetStatus(string text)
        {
            StatusText = text;
            RaisePropertyChanged(nameof(StatusText));
        }

        private async void Disconnect()
        {
            if (EventHandlerForDevice.Current.Device != null && _connectionService.Devices.Count > 0)
            {
                OnStopTracking();

                _connectionService.DisconnectFromDevice(_connectionService.Devices[0]);


                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    RaisePropertyChanged(nameof(ConnectCommand));
                    RaisePropertyChanged(nameof(DisconnectCommand));
                    RaisePropertyChanged(nameof(UpdateDataCommand));

                    SetStatus(STATUS_NOT_CONNECTED);
                }));
            }
        }

        private void InitializeDevice()
        {
            EventHandlerForDevice.Current.Device.BaudRate = 1000000;
            //EventHandlerForDevice.Current.Device.StopBits = SerialStopBitCount.One;
            //EventHandlerForDevice.Current.Device.DataBits = 8;
            //EventHandlerForDevice.Current.Device.Parity = SerialParity.None;
            //EventHandlerForDevice.Current.Device.Handshake = SerialHandshake.None;
            //EventHandlerForDevice.Current.Device.WriteTimeout = TimeSpan.FromMilliseconds(500);
            //EventHandlerForDevice.Current.Device.ReadTimeout = TimeSpan.FromMilliseconds(500);
        }

        private void OnSerialMessageReceived(object sender, SerialMessage e)
        {
            ProcessMessage(e);
        }

        private void OnSerialTextRead(object sender, string e)
        {
            // ProcessMessage(e);
        }

        private void AddFilter(string filterString)
        {
            if (!Filters.Contains(filterString) && !String.IsNullOrEmpty(filterString))
            {
                Filters.Add(filterString);
            }
        }

        private void DeleteFilter(string filterString)
        {
            if (Filters.Contains(filterString))
            {
                Filters.Remove(filterString);
            }
        }

        public void OnNavigatedTo()
        {
            _connectionService.OnNavigatedTo();
        }

        public void OnNavigatingFrom()
        {
            _serialBroker.CancelAllIoTasks();
            _connectionService.OnNavigatedFrom();
        }

        private async void OnStopTracking()
        {
            _serialBroker.CancelReadTask();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RaisePropertyChanged(nameof(StartCommand));
                RaisePropertyChanged(nameof(StopCommand));
            });
        }

        private async void AddTestRow()
        {
            var r = new Random();
            var i = r.Next(0, 9);
            var id = r.Next(0, 9);
            var msg = $"CAN:7{id}D 8 {i} 62 2 8C 42 AA AA AA;";
        }

        internal async Task AddCanMessage(CANMessage message)
        {
            var group = Messages.FirstOrDefault(g => g.Key == message.Id);
            if (group != null)
            {
                var item = group.FirstOrDefault(m => m.Message == message.Message);
                if (item != null)
                {
                    item.Count++;
                    return;
                }
            }
            var row = new MessageRow(message);


            await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
            {
                Messages.Add(row);
            }));
        }

        internal void DeleteRecords()
        {
            Messages.ClearItems();
            RaisePropertyChanged(nameof(Messages));
        }


        internal void ClearGroups()
        {
            _messages.Clear();
            _newMessages.Clear();
            Messages.ClearItems();
            RaisePropertyChanged(nameof(Messages));
        }

        internal void FilterItems()
        {
            _newMessages.Clear();
            Messages.ClearItems();
            RaisePropertyChanged(nameof(Messages));
        }

        internal void RemoveGroup(Grouping<string, MessageRow> group)
        {
            foreach (var item in group.ToList())
            {
                Messages.Remove(item);
            }
        }

        internal void RemoveGroupAndFilter(Grouping<string, MessageRow> group)
        {
            Filters.Add(group.Key);
            RemoveGroup(group);
        }

        /// <summary>
        /// Invoked when the Temperature Button is clicked
        /// </summary>
        private async void OnStartTracking()
        {
            try
            {
                _started = true;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    RaisePropertyChanged(nameof(StartCommand));
                    RaisePropertyChanged(nameof(StopCommand));
                });

                await _serialBroker.StartReadingAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Stopped");
            }
            finally
            {
                _started = false;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    RaisePropertyChanged(nameof(StartCommand));
                    RaisePropertyChanged(nameof(StopCommand));
                });
            }

            return;
        }

        private async void RequestData()
        {
            try
            {
                await SendCommand(Commands.CMD_CHECK_HYBRID_BATTERY);

            }
            catch
            {

            }
        }
        public async void ProcessMessage(SerialMessage s)
        {
            if (s != null)
            {
                if (s is CANMessage canM)
                {

                    if (canM.Id == CanId.HYBRID)
                    {
                        UpdateData(canM);
                    }

                    var canMessage = canM.Message.Trim();
                    //var canMessage = canM.Id;
                    //if (!canMessage.StartsWith("42"))
                    //{
                    //    return;
                    //}

                    if (Filters.Any(f => canMessage.StartsWith(f)))
                    {
                        return;
                    }

                    if (_messages.ContainsKey(canMessage))
                    {
                        _messages[canMessage]++;

                        if (_newMessages.ContainsKey(canMessage))
                        {
                            _newMessages[canMessage]++;
                            await AddCanMessage(canM);

                        }
                    }
                    else
                    {
                        _messages.Add(canMessage, 1);

                        _newMessages.Add(canMessage, 1);

                        await AddCanMessage(canM);
                    }
                }
                else
                {
                    await Log(s.Message);
                }
            }
        }

        private async void UpdateData(CANMessage msg)
        {
            BatteryLevel = int.Parse(msg.Data[4], System.Globalization.NumberStyles.HexNumber);

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RaisePropertyChanged(nameof(BatteryLevel));
            });
        }

        public async Task SendCommand(string cmd)
        {
            if (String.IsNullOrEmpty(cmd))
            {
                return;
            }

            await _serialBroker.WriteCommandAsync(cmd);
            await Log(cmd);
        }

        private async Task Log(string row)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                LogLines.Add($"{DateTime.Now.ToShortTimeString()}: {row}");
            });
        }
    }
}
