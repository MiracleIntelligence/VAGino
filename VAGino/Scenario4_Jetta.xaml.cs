//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Diagnostics;
using System.Threading;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using Windows.Devices.SerialCommunication;

using System.Threading.Tasks;
using Windows.Storage.Streams;

using SDKTemplate;
using SerialArduino;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using CANEmulator;
using System.Collections.Generic;
using MessageAnalyzer.Models;
using MessageAnalyzer;
using System.Collections.ObjectModel;
using VAGino.Models;
using System.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SerialArduino
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario4_Jetta : Page, IDisposable
    {

        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        private MainPage rootPage = MainPage.Current;

        // Track Read Operation
        private CancellationTokenSource ReadCancellationTokenSource;
        private Object ReadCancelLock = new Object();

        DataReader DataReaderObject = null;

        // Track Write Operation
        private CancellationTokenSource WriteCancellationTokenSource;
        private Object WriteCancelLock = new Object();

        DataWriter DataWriterObject = null;

        // Indicate if we navigate away from this page or not.
        private Boolean IsNavigatedAway;
        private Paragraph _paragraph;
        private Dictionary<string, int> _messages;
        private Dictionary<string, int> _newMessages;

        ObservableCollection<MessageRow> Messages { get; set; }

        public enum LedState
        {
            LedStateOn,
            LedStateOff
        };

        public Scenario4_Jetta()
        {
            this.InitializeComponent();

            KeyEventHandler keyeventHandler = new KeyEventHandler(OnTBCommandEnter);

            TBCommand.AddHandler(TextBox.KeyDownEvent, keyeventHandler, true);

            _messages = new Dictionary<string, int>();
            _newMessages = new Dictionary<string, int>();
            Messages = new ObservableCollection<MessageRow>();
            LVMessages.ItemsSource = Messages;
        }

        private void OnTBCommandEnter(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendCommand();
            }
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

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        ///
        /// We will enable/disable parts of the UI if the device doesn't support it.
        /// </summary>
        /// <param name="eventArgs">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (EventHandlerForDevice.Current.Device == null)
            {
                //    LEDTempScrollViewer.Visibility = Visibility.Collapsed;
                MainPage.Current.NotifyUser("Device is not connected", NotifyType.ErrorMessage);
            }
            else
            {
                MainPage.Current.NotifyUser("Connected to " + EventHandlerForDevice.Current.DeviceInformation.Id,
                                            NotifyType.StatusMessage);

                // So we can reset future tasks
                ResetReadCancellationTokenSource();
                ResetWriteCancellationTokenSource();

                EventHandlerForDevice.Current.Device.BaudRate = 1000000;
                //EventHandlerForDevice.Current.Device.StopBits = SerialStopBitCount.One;
                //EventHandlerForDevice.Current.Device.DataBits = 8;
                //EventHandlerForDevice.Current.Device.Parity = SerialParity.None;
                //EventHandlerForDevice.Current.Device.Handshake = SerialHandshake.None;
                //EventHandlerForDevice.Current.Device.WriteTimeout = TimeSpan.FromMilliseconds(500);
                //EventHandlerForDevice.Current.Device.ReadTimeout = TimeSpan.FromMilliseconds(500);
            }

        }

        /// <summary>
        /// Cancel any on going tasks when navigating away from the page so the device is in a consistent state throughout
        /// all the scenarios
        /// </summary>
        /// <param name="eventArgs"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            IsNavigatedAway = true;
            CancelAllIoTasks();
        }


        /// <summary>
        /// Writes a custom command to the device to turn LEDs on/off
        /// using a DataWriter to the OutputStream
        /// </summary>
        /// <param name="ledNumber"></param>
        /// <param name="ledState"></param>
        private async void WriteLedCommand(String ledNumber, LedState ledState)
        {
            if (EventHandlerForDevice.Current.IsDeviceConnected)
            {
                try
                {
                    rootPage.NotifyUser("Writing...", NotifyType.StatusMessage);

                    DataWriterObject = new DataWriter(EventHandlerForDevice.Current.Device.OutputStream);

                    switch (ledState)
                    {
                        case LedState.LedStateOn:
                            DataWriterObject.WriteString("ledon " + ledNumber + "\r");
                            break;

                        case LedState.LedStateOff:
                            DataWriterObject.WriteString("ledoff " + ledNumber + "\r");
                            break;

                        default:
                            break;
                    }

                    await WriteAsync(WriteCancellationTokenSource.Token);
                }
                catch (OperationCanceledException /*exception*/)
                {
                    NotifyWriteTaskCanceled();
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

        private async Task WriteCommandAsync(String command)
        {
            if (EventHandlerForDevice.Current.IsDeviceConnected)
            {
                try
                {
                    rootPage.NotifyUser("Writing...", NotifyType.StatusMessage);

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
                    NotifyWriteTaskCanceled();
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
        /// Invoked when the Temperature Button is clicked
        /// </summary>
        private async void OnStartTracking(object sender, RoutedEventArgs e)
        {
            try
            {
                TemperatureButton.IsEnabled = false;
                ReadCancellationTokenSource = new CancellationTokenSource();
                DataReaderObject = new DataReader(EventHandlerForDevice.Current.Device.InputStream);
                await ReadAsync(ReadCancellationTokenSource.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Stopped");
                TemperatureButton.IsEnabled = true;
            };

            return;

            if (EventHandlerForDevice.Current.IsDeviceConnected)
            {
                try
                {
                    rootPage.NotifyUser("Getting temperature...", NotifyType.StatusMessage);

                    DataWriterObject = new DataWriter(EventHandlerForDevice.Current.Device.OutputStream);
                    DataWriterObject.WriteString("temp\r");

                    await WriteAsync(WriteCancellationTokenSource.Token);
                }
                catch (OperationCanceledException /*exception*/)
                {
                    NotifyWriteTaskCanceled();
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

                try
                {
                    TemperatureValue.Text = String.Empty;

                    DataReaderObject = new DataReader(EventHandlerForDevice.Current.Device.InputStream);
                    await ReadAsync(ReadCancellationTokenSource.Token);
                }
                catch (OperationCanceledException /*exception*/)
                {
                    NotifyReadTaskCanceled();
                }
                catch (Exception exception)
                {
                    MainPage.Current.NotifyUser(exception.Message.ToString(), NotifyType.ErrorMessage);
                    Debug.WriteLine(exception.Message.ToString());
                }
                finally
                {
                    DataReaderObject.DetachStream();
                    DataReaderObject = null;
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
            rootPage.NotifyUser("Write completed - " + bytesWritten.ToString() + " bytes written", NotifyType.StatusMessage);
        }

        CANEmulator.CANEmulator _emulator = new CANEmulator.CANEmulator();
        Analyzer _analyzer = new Analyzer();
        /// <summary>
        /// Read from the input output stream using a task 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// 

        Queue<Byte> _buffer = new Queue<byte>();
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

                        //Convert to HEX 
                        //for (int i = 0; i < fileContent.Length; i++)
                        //{
                        //    string recvdtxt1 = fileContent[i].ToString();
                        //    msg += recvdtxt1;
                        //}

                        await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
                        {
                            await ProcessMessage(text);
                        }));
                        //System.Diagnostics.Debug.WriteLine(recvdtxt);
                        //this.textBoxRecvdText.Text = recvdtxt;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ReadAsync: " + ex.Message);
                    }
                }

                //String msg = DataReaderObject.ReadString(bytesRead);
                //byte[] buffer = new byte[bytesRead];


                //DataReaderObject.ReadBytes(buffer);
                //for (int i = 0; i < bytesRead; i++)
                //{
                //    if (_buffer.Count == 8)
                //    {
                //        if (_buffer)
                //    }
                //}

                //if (Convert.ToInt32(buffer) == 255)
                //{

                //}
                //string text = System.Text.Encoding.UTF8.GetString(buffer);
                //string textb64 = Convert.ToBase64String(buffer);
                //ProcessMessage(msg);
                //TemperatureValue.Text = temp.Trim() + "°C";
            }

            //foreach (var msg in _emulator.GetMessage())
            //{
            //    await ProcessMessage(msg);
            //    //await Task.Delay(100).ConfigureAwait(true);
            //}
            //}

            //rootPage.NotifyUser("Read completed - " + bytesRead.ToString() + " bytes were read", NotifyType.StatusMessage);

        }

        private async Task ReadOnceAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 10;

            // Don't start any IO if we canceled the task
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
                DataReaderObject.ReadBytes(fileContent);
                string text = System.Text.Encoding.UTF8.GetString(fileContent);

                await WriteConsole(text);
            }

            rootPage.NotifyUser("Read completed - " + bytesRead.ToString() + " bytes were read", NotifyType.StatusMessage);

        }


        private async Task ReadAnswerAsync(CancellationToken cancellationToken)
        {
            if (EventHandlerForDevice.Current.IsDeviceConnected)
            {
                try
                {
                    DataReaderObject = new DataReader(EventHandlerForDevice.Current.Device.InputStream);
                    await ReadOnceAsync(ReadCancellationTokenSource.Token);
                }
                catch (OperationCanceledException /*exception*/)
                {
                    NotifyReadTaskCanceled();
                }
                catch (Exception exception)
                {
                    MainPage.Current.NotifyUser(exception.Message.ToString(), NotifyType.ErrorMessage);
                    Debug.WriteLine(exception.Message.ToString());
                }
                finally
                {
                    DataReaderObject.DetachStream();
                    DataReaderObject = null;
                }
            }
            else
            {
                Utilities.NotifyDeviceNotConnected();
            }
        }


        private async Task ProcessMessage(string msg)
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
                            Messages.First(row => row.Message.Equals(canMessage)).Count++;
                        }
                    }
                    else
                    {
                        _messages.Add(canMessage, 1);

                        _newMessages.Add(canMessage, 1);
                        Messages.Add(new MessageRow()
                        {
                            Message = canMessage,
                            Count = 1
                        });
                    }                   

                }
                //await WriteConsole(m);
            }
        }

        string _canTemp;

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

        /// <summary>
        /// It is important to be able to cancel tasks that may take a while to complete. Cancelling tasks is the only way to stop any pending IO
        /// operations asynchronously. If the Serial Device is closed/deleted while there are pending IOs, the destructor will cancel all pending IO 
        /// operations.
        /// </summary>
        /// 

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
            await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                new DispatchedHandler(() =>
                {
                    if (!IsNavigatedAway)
                    {
                        rootPage.NotifyUser("Canceling Read... Please wait...", NotifyType.StatusMessage);
                    }
                }));
        }

        private async void NotifyWriteCancelingTask()
        {
            // Setting the dispatcher priority to high allows the UI to handle disabling of all the buttons
            // before any of the IO completion callbacks get a chance to modify the UI; that way this method
            // will never get the opportunity to overwrite UI changes made by IO callbacks
            await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.High,
                new DispatchedHandler(() =>
                {
                    if (!IsNavigatedAway)
                    {
                        rootPage.NotifyUser("Canceling Write... Please wait...", NotifyType.StatusMessage);
                    }
                }));
        }

        /// <summary>
        /// Notifies the UI that the operation has been cancelled
        /// </summary>
        private async void NotifyReadTaskCanceled()
        {
            await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                new DispatchedHandler(() =>
                {
                    if (!IsNavigatedAway)
                    {
                        rootPage.NotifyUser("Read request has been cancelled", NotifyType.StatusMessage);
                    }
                }));
        }

        private async void NotifyWriteTaskCanceled()
        {
            await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                new DispatchedHandler(() =>
                {
                    if (!IsNavigatedAway)
                    {
                        rootPage.NotifyUser("Write request has been cancelled", NotifyType.StatusMessage);
                    }
                }));
        }


        private async void UpdateData(CANMessage msg)
        {
            await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                new DispatchedHandler(() =>
                {
                    TBBatteryLevelValue.Text = int.Parse(msg.Data[4], System.Globalization.NumberStyles.HexNumber).ToString();
                }));
        }

        private void BTNCommand_Click(object sender, RoutedEventArgs e)
        {
            SendCommand();
        }

        private async void SendCommand()
        {
            var cmd = TBCommand.Text;
            await SendCommand(cmd);
            //OnStartTracking(null, null);
            //await ReadAnswerAsync(ReadCancellationTokenSource.Token);
        }

        private async Task SendCommand(string cmd)
        {
            if (String.IsNullOrEmpty(cmd))
            {
                return;
            }

            TBCommand.Text = String.Empty;

            //OnStopTracking(null, null);
            await WriteConsole(cmd);
            await WriteCommandAsync(cmd);
        }

        private async Task WriteConsole(string str)
        {
            await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                var run = new Run();
                run.Text = str;

                if (_paragraph == null)
                {
                    _paragraph = new Paragraph();
                    RTBConsole.Blocks.Add(_paragraph);
                }

                _paragraph.Inlines.Add(run);
                _paragraph.Inlines.Add(new LineBreak());


                SVConsole.ChangeView(null, SVConsole.ActualHeight, null);
            }));
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            Messages.Clear();
            _newMessages.Clear();
        }

        private void OnStopTracking(object sender, RoutedEventArgs e)
        {
            ReadCancellationTokenSource.Cancel();
        }

        private void OnSort(object sender, RoutedEventArgs e)
        {
            Messages = new ObservableCollection<MessageRow>(Messages.OrderBy(m => m.Message));
            LVMessages.ItemsSource = Messages;

        }

        private async void UpdateData_Click(object sender, RoutedEventArgs e)
        {
            await SendCommand(Commands.BATTERY);
        }

        private async void LFWDown_Click(object sender, RoutedEventArgs e)
        {
            await SendCommand(Commands.LFW_DOWN);
        }
    }
}
