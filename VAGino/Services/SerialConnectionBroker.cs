using MessageAnalyzer;
using MessageAnalyzer.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage.Streams;

namespace VAGino.Services
{
    public class SerialConnectionBroker : IDisposable
    {
        public event EventHandler<string> TextRead;
        public event EventHandler<SerialMessage> MessageReceived;

        private CancellationTokenSource ReadCancellationTokenSource;
        private Object ReadCancelLock = new Object();
        private DataReader DataReaderObject = null;

        // Track Write Operation
        private CancellationTokenSource WriteCancellationTokenSource;
        private Object WriteCancelLock = new Object();
        DataWriter DataWriterObject = null;
        Analyzer _analyzer;

        public SerialConnectionBroker()
        {
            _analyzer = new Analyzer();
            // So we can reset future tasks
            ResetReadCancellationTokenSource();
            ResetWriteCancellationTokenSource();
        }

        public async Task StartReadingAsync()
        {
            ReadCancellationTokenSource = new CancellationTokenSource();
            DataReaderObject = new DataReader(EventHandlerForDevice.Current.Device.InputStream);
            try
            {
                await ReadAsync(ReadCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // canceled;
            }
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1;
            Queue<byte> serialQ = new Queue<byte>();
            while (true)
            {
                //// Don't start any IO if we canceled the task
                lock (ReadCancelLock)
                {
                    //if (DataReaderObject.UnconsumedBufferLength == 0)
                    //{
                    //    continue;
                    //}
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

                        var b = DataReaderObject.ReadByte();
                        if (b != '\r')
                        {
                            serialQ.Enqueue(b);
                        }
                        else
                        {
                            ProcessMessage(serialQ.ToArray());
                            serialQ.Clear();
                        }
                        //string msg = string.Empty;
                        //read data
                        //DataReaderObject.ReadBytes(fileContent);
                        //string text = System.Text.Encoding.UTF8.GetString(fileContent);

                        //ProcessMessage(text);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ReadAsync: " + ex.Message);
                    }
                }
            }
        }

        private void ProcessMessage(byte[] serialData)
        {
            if (serialData.Length == 0)
            {
                return;
            }

            var cmd = serialData[0];
            // CAN data;
            if (cmd == 1)
            {
                var id = BitConverter.ToInt16(serialData, 1);
                var dlc = serialData[3];
                if (serialData.Length != dlc + 4)
                {
                    return;
                }
                else
                {
                    StringBuilder stringBuilder = new StringBuilder("CAN:");
                    stringBuilder.Append(id.ToString("x").ToUpper());
                    stringBuilder.Append(" ");
                    stringBuilder.Append(dlc.ToString("x").ToUpper());
                    for (int i = 4; i < serialData.Length; ++i)
                    {
                        stringBuilder.Append(" ");
                        stringBuilder.Append(serialData[i].ToString("x").ToUpper());
                    }
                    ProcessMessage(stringBuilder.ToString());
                    //Debug.WriteLine("");
                }
            }
            else
            {
                string text = System.Text.Encoding.UTF8.GetString(serialData.Skip(1).ToArray());
                ProcessMessage(text);
            }
        }

        private void ProcessMessage(string text)
        {
            TextRead?.Invoke(this, text);

            //var rawMessage = ProcessSerial(text);
            var rawMessage = text;
            if (!String.IsNullOrEmpty(rawMessage))
            {
                SerialMessage s;
                try
                {
                    s = _analyzer.Analyze(rawMessage);
                    Debug.WriteLine(s.Message);
                    MessageReceived?.Invoke(this, s);
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }

        public async Task WriteCommandAsync(String command)
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


        public void CancelAllIoTasks()
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

        public void CancelReadTask()
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

        string _canTemp;
        public string ProcessSerial(string msg)
        {
            if (!String.IsNullOrEmpty(msg))
            {
                _canTemp += msg;
            }

            if (_canTemp.Contains("\r\n"))
            {
                var cmd = _canTemp.Substring(0, _canTemp.IndexOf("\r\n"));
                _canTemp = _canTemp.Substring(cmd.Length + "\r\n".Length); ;
                return cmd;
            }
            return null;
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

            TextRead = null;
        }
    }
}
