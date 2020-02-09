using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MessageAnalyzer;
using MessageAnalyzer.Models;

using ReadlnLibrary.Core.Collections;
using SDKTemplate;
using SerialArduino;
using VAGino.Models;
using Windows.ApplicationModel.Core;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace VAGino.ViewModels
{
    public class JettaViewModel : ViewModelBase
    {
        public ICommand ClearCommand { get; }
        public ICommand AddTestRowCommand { get; }
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; }

        public GroupedObservableCollection<string, MessageRow> Messages { get; private set; }

        //ObservableCollection<MessageRow> Messages { get; set; }
        public object AddMessage { get; internal set; }
        public int BatteryLevel { get; private set; }

        private CancellationTokenSource ReadCancellationTokenSource;
        private DataReader DataReaderObject = null;
        private bool _started;
        private Object ReadCancelLock = new Object();

        // Track Write Operation
        private CancellationTokenSource WriteCancellationTokenSource;
        private Object WriteCancelLock = new Object();

        DataWriter DataWriterObject = null;

        private Dictionary<string, int> _messages;
        private Dictionary<string, int> _newMessages;
        private List<string> _filters;

        string _canTemp;
        CANEmulator.CANEmulator _emulator = new CANEmulator.CANEmulator();
        Analyzer _analyzer = new Analyzer();

        public JettaViewModel()
        {
            _messages = new Dictionary<string, int>();
            _newMessages = new Dictionary<string, int>();
            _filters = Filters.IDFilters;

            ClearCommand = new RelayCommand(ClearGroups);
            AddTestRowCommand = new RelayCommand(AddTestRow);
            StartCommand = new RelayCommand(OnStartTracking, () => !_started && EventHandlerForDevice.Current.Device != null);
            StopCommand = new RelayCommand(OnStopTracking, () => _started);

            //SetTestData();
            // So we can reset future tasks
            ResetReadCancellationTokenSource();
            ResetWriteCancellationTokenSource();

            Messages = new GroupedObservableCollection<string, MessageRow>((m) => m.Id);
        }

        public void OnNavigatedTo()
        {

        }

        public void OnNavigatingFrom()
        {
            CancelAllIoTasks();
        }

        private async void OnStopTracking()
        {
            ReadCancellationTokenSource.Cancel();

            await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
            {
                RaisePropertyChanged(nameof(StartCommand));
                RaisePropertyChanged(nameof(StopCommand));
            }));
        }

        private async void AddTestRow()
        {
            var r = new Random();
            var i = r.Next(0, 9);
            var id = r.Next(0, 9);
            var msg = $"CAN:7{id}D 8 {i} 62 2 8C 42 AA AA AA;";
            //var message = new CANMessage($"7{id}D 8 {i} 62 2 8C 42 AA AA AA");
            ProcessMessage(msg);
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

        internal void ClearGroups()
        {
            //Messages = new GroupedObservableCollection<string, MessageRow>((m) => m.Id);
            Messages.ClearItems();
            RaisePropertyChanged(nameof(Messages));
        }

        internal void FilterGroups()
        {
            _newMessages.Clear();
            ClearGroups();
        }

        internal void RemoveGroup(Grouping<string, MessageRow> group)
        {
            _filters.Add(group.Key);
            foreach (var item in group.ToList())
            {
                Messages.Remove(item);
            }
        }

        /// <summary>
        /// Invoked when the Temperature Button is clicked
        /// </summary>
        private async void OnStartTracking()
        {
            try
            {
                _started = true;
                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
                {
                    RaisePropertyChanged(nameof(StartCommand));
                    RaisePropertyChanged(nameof(StopCommand));
                }));

                ReadCancellationTokenSource = new CancellationTokenSource();
                DataReaderObject = new DataReader(EventHandlerForDevice.Current.Device.InputStream);
                await ReadAsync(ReadCancellationTokenSource.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Stopped");
            }
            finally
            {
                _started = false;
                await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
                {
                    RaisePropertyChanged(nameof(StartCommand));
                    RaisePropertyChanged(nameof(StopCommand));
                }));
            }

            return;
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 16;

            while (true)
            {
                //// Don't start any IO if we canceled the task
                lock (ReadCancelLock)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Cancellation Token will be used so we can stop the task operation explicitly
                    // The completion function should still be called so that we can properly handle a canceled task
                    DataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
                    loadAsyncTask = DataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);
                }

                UInt32 bytesRead = await loadAsyncTask;


                if (bytesRead > 0)
                {
                    byte[] fileContent = new byte[DataReaderObject.UnconsumedBufferLength];
                    try
                    {
                        string msg = string.Empty;
                        //read data
                        DataReaderObject.ReadBytes(fileContent);
                        string text = System.Text.Encoding.UTF8.GetString(fileContent);

                        ProcessMessage(text);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ReadAsync: " + ex.Message);
                    }
                }
            }
        }

        private async void ProcessMessage(string msg)
        {
            var m = ProcessSerial(msg);
            if (m != null)
            {
                SerialMessage s;
                try
                {
                    s = _analyzer.Analyze(m);
                }
                catch (Exception ex)
                {
                    return;
                }
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

                    if (Filters.IDFilters.Any(f => canMessage.StartsWith(f)))
                    {
                        return;
                    }

                    if (_messages.ContainsKey(canMessage))
                    {
                        _messages[canMessage]++;

                        if (_newMessages.ContainsKey(canMessage))
                        {
                            _newMessages[canMessage]++;
                            //Messages.First(row => row.Message.Equals(canMessage)).Count++;
                            await AddCanMessage(canM);

                        }
                    }
                    else
                    {
                        _messages.Add(canMessage, 1);

                        _newMessages.Add(canMessage, 1);
                        //Messages.Add(new MessageRow()
                        //{
                        //    Message = canMessage,
                        //    Count = 1
                        //});

                        await AddCanMessage(canM);
                    }

                }
                //await WriteConsole(m);
            }
        }

        private string ProcessSerial(string msg)
        {
            if (!String.IsNullOrEmpty(msg))
            {
                _canTemp += msg;
            }

            if (_canTemp.Contains(";"))
            {
                var cmd = _canTemp.Substring(0, _canTemp.IndexOf(";"));
                _canTemp = _canTemp.Substring(cmd.Length + 1);
                return cmd;
            }
            return null;


        }

        private void SetTestData()
        {
            //var messages = new List<CANMessage>
            //{
            //    new CANMessage("7ED 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("7AD 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("70D 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("72D 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("73D 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("72D 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("71D 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("73D 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("72D 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("7BD 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("7FD 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("7FD 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("7ED 8 4 62 2 8C 42 AA AA AA"),
            //    new CANMessage("7ED 8 4 62 2 8C 42 AA AA AA"),
            //};
            //var rows = messages.Select(m => new MessageRow(m)).ToList();

            //Messages = new GroupedObservableCollection<string, MessageRow>((m) => m.Id, rows);
        }

        private async void RequestData()
        {
            await SendCommand(Commands.BATTERY);
        }

        private void UpdateData(CANMessage msg)
        {
            BatteryLevel = int.Parse(msg.Data[4], System.Globalization.NumberStyles.HexNumber);
            //await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            //    new DispatchedHandler(() =>
            //    {
            //        TBBatteryLevelValue.Text = int.Parse(msg.Data[4], System.Globalization.NumberStyles.HexNumber).ToString();
            //    }));
        }

        public async Task SendCommand(string cmd)
        {
            if (String.IsNullOrEmpty(cmd))
            {
                return;
            }

            //OnStopTracking(null, null);
            //await WriteConsole(cmd);
            await WriteCommandAsync(cmd);
        }

        private async Task WriteCommandAsync(String command)
        {
            if (EventHandlerForDevice.Current.IsDeviceConnected)
            {
                try
                {
                    //rootPage.NotifyUser("Writing...", NotifyType.StatusMessage);

                    DataWriterObject = new DataWriter(EventHandlerForDevice.Current.Device.OutputStream);
                    DataWriterObject.WriteString($"{command}\n\r");
                    //switch (ledState)
                    //{
                    //    case LedState.LedStateOn:
                    //        DataWriterObject.WriteString("ledon " + ledNumber + "\r");
                    //        break;

                    //    case LedState.LedStateOff:
                    //        DataWriterObject.WriteString("ledoff " + ledNumber + "\r");
                    //        break;

                    //    default:
                    //        break;
                    //}

                    await WriteAsync(WriteCancellationTokenSource.Token);
                }
                catch (OperationCanceledException /*exception*/)
                {
                    //NotifyWriteTaskCanceled();
                }
                catch (Exception exception)
                {
                    MainPage.Current.NotifyUser(exception.Message.ToString(), NotifyType.ErrorMessage);
                    Debug.WriteLine(exception.Message.ToString());
                }
                finally
                {
                    DataWriterObject.DetachStream();
                    DataWriterObject = null;
                }
            }
            else
            {
                Utilities.NotifyDeviceNotConnected();
            }
        }

        /// <summary>
        /// Write to the output stream using a task 
        /// </summary>
        /// <param name="cancellationToken"></param>
        private async Task WriteAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> storeAsyncTask;

            // Don't start any IO if we canceled the task
            lock (WriteCancelLock)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Cancellation Token will be used so we can stop the task operation explicitly
                // The completion function should still be called so that we can properly handle a canceled task
                storeAsyncTask = DataWriterObject.StoreAsync().AsTask(cancellationToken);
            }

            UInt32 bytesWritten = await storeAsyncTask;
            //rootPage.NotifyUser("Write completed - " + bytesWritten.ToString() + " bytes written", NotifyType.StatusMessage);
        }


        public void Dispose()
        {
            if (ReadCancellationTokenSource != null)
            {
                ReadCancellationTokenSource.Dispose();
                ReadCancellationTokenSource = null;
            }

            if (WriteCancellationTokenSource != null)
            {
                WriteCancellationTokenSource.Dispose();
                WriteCancellationTokenSource = null;
            }
        }

        private void CancelReadTask()
        {
            lock (ReadCancelLock)
            {
                if (ReadCancellationTokenSource != null)
                {
                    if (!ReadCancellationTokenSource.IsCancellationRequested)
                    {
                        ReadCancellationTokenSource.Cancel();

                        // Existing IO already has a local copy of the old cancellation token so this reset won't affect it
                        ResetReadCancellationTokenSource();
                    }
                }
            }
        }

        private void CancelWriteTask()
        {
            lock (WriteCancelLock)
            {
                if (WriteCancellationTokenSource != null)
                {
                    if (!WriteCancellationTokenSource.IsCancellationRequested)
                    {
                        WriteCancellationTokenSource.Cancel();

                        // Existing IO already has a local copy of the old cancellation token so this reset won't affect it
                        ResetWriteCancellationTokenSource();
                    }
                }
            }
        }

        private void CancelAllIoTasks()
        {
            CancelReadTask();
            CancelWriteTask();
        }

        private void ResetReadCancellationTokenSource()
        {
            // Create a new cancellation token source so that can cancel all the tokens again
            ReadCancellationTokenSource = new CancellationTokenSource();

            // Hook the cancellation callback (called whenever Task.cancel is called)
            ReadCancellationTokenSource.Token.Register(() => NotifyReadCancelingTask());
        }

        private void ResetWriteCancellationTokenSource()
        {
            // Create a new cancellation token source so that can cancel all the tokens again
            WriteCancellationTokenSource = new CancellationTokenSource();

            // Hook the cancellation callback (called whenever Task.cancel is called)
            WriteCancellationTokenSource.Token.Register(() => NotifyWriteCancelingTask());
        }

        /// <summary>
        /// Print a status message saying we are canceling a task and disable all buttons to prevent multiple cancel requests.
        /// <summary>
        private async void NotifyReadCancelingTask()
        {
            // Setting the dispatcher priority to high allows the UI to handle disabling of all the buttons
            // before any of the IO completion callbacks get a chance to modify the UI; that way this method
            // will never get the opportunity to overwrite UI changes made by IO callbacks
            //await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.High,
            //    new DispatchedHandler(() =>
            //    {
            //        if (!IsNavigatedAway)
            //        {
            //            rootPage.NotifyUser("Canceling Read... Please wait...", NotifyType.StatusMessage);
            //        }
            //    }));
        }

        private async void NotifyWriteCancelingTask()
        {
            // Setting the dispatcher priority to high allows the UI to handle disabling of all the buttons
            // before any of the IO completion callbacks get a chance to modify the UI; that way this method
            // will never get the opportunity to overwrite UI changes made by IO callbacks
            //await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.High,
            //    new DispatchedHandler(() =>
            //    {
            //        if (!IsNavigatedAway)
            //        {
            //            rootPage.NotifyUser("Canceling Write... Please wait...", NotifyType.StatusMessage);
            //        }
            //    }));
        }

    }
}
