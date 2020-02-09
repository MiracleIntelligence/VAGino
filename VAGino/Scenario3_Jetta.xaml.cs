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

using MessageAnalyzer;
using MessageAnalyzer.Models;

using Microsoft.Toolkit.Uwp.UI.Controls;
using ReadlnLibrary.Core.Collections;
using SDKTemplate;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using VAGino.Models;
using VAGino.ViewModels;

using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SerialArduino
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario3_Jetta : Page, IDisposable
    {

        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        private MainPage rootPage = MainPage.Current;
        private JettaViewModel ViewModel { get; }


        // Indicate if we navigate away from this page or not.
        private Boolean IsNavigatedAway;
        private Paragraph _paragraph;

        public Scenario3_Jetta()
        {
            this.InitializeComponent();
            ViewModel = new JettaViewModel();
            KeyEventHandler keyeventHandler = new KeyEventHandler(OnTBCommandEnter);

            TBCommand.AddHandler(TextBox.KeyDownEvent, keyeventHandler, true);
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
            ViewModel.Dispose();
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



                EventHandlerForDevice.Current.Device.BaudRate = 1000000;
                //EventHandlerForDevice.Current.Device.StopBits = SerialStopBitCount.One;
                //EventHandlerForDevice.Current.Device.DataBits = 8;
                //EventHandlerForDevice.Current.Device.Parity = SerialParity.None;
                //EventHandlerForDevice.Current.Device.Handshake = SerialHandshake.None;
                //EventHandlerForDevice.Current.Device.WriteTimeout = TimeSpan.FromMilliseconds(500);
                //EventHandlerForDevice.Current.Device.ReadTimeout = TimeSpan.FromMilliseconds(500);

            }

            ViewModel.OnNavigatedTo();
        }

        /// <summary>
        /// Cancel any on going tasks when navigating away from the page so the device is in a consistent state throughout
        /// all the scenarios
        /// </summary>
        /// <param name="eventArgs"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            IsNavigatedAway = true;
            ViewModel.OnNavigatingFrom();
        }


        /// <summary>
        /// Read from the input output stream using a task 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// 

        Queue<Byte> _buffer = new Queue<byte>();

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
            await ViewModel.SendCommand(cmd);
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



        private async void UpdateData_Click(object sender, RoutedEventArgs e)
        {
            await SendCommand(Commands.BATTERY);
        }

        private async void LFWDown_Click(object sender, RoutedEventArgs e)
        {
            await SendCommand(Commands.LFW_DOWN);
        }

        private void OnDataGridLoaded(object sender, RoutedEventArgs e)
        {
            CreateCollectionView(sender as DataGrid);
        }

        private void CreateCollectionView(DataGrid grid)
        {
            //grid.DataContext = MessageGroups;
            MessageGroups.Source = null;
            MessageGroups.Source = ViewModel.Messages;
            grid.ItemsSource = MessageGroups.View;

            //Binding myBinding = new Binding();
            //myBinding.Source = MessageGroups;
            //myBinding.Path = new PropertyPath("View");
            //myBinding.Mode = BindingMode.OneWay;
            //myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            //BindingOperations.SetBinding(grid, DataGrid.ItemsSourceProperty, myBinding);

        }

        private void OnLoadingRowGroup(object sender, DataGridRowGroupHeaderEventArgs e)
        {
            ICollectionViewGroup group = e.RowGroupHeader.CollectionViewGroup;

            e.RowGroupHeader.RightTapped += RowGroupHeader_RightTapped;
            

            // To update items count in group header

            Binding myBinding = new Binding();
            myBinding.Source = group.Group;
            myBinding.Path = new PropertyPath("KeyData");
            myBinding.Mode = BindingMode.OneWay;
            myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            BindingOperations.SetBinding(e.RowGroupHeader, DataGridRowGroupHeader.PropertyValueProperty, myBinding);

        }

        private void RowGroupHeader_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ICollectionViewGroup groupView = (sender as DataGridRowGroupHeader).CollectionViewGroup;
            var group = groupView.Group as Grouping<string, MessageRow>;

            DataGridGroupd.ItemsSource = null;

            ViewModel.RemoveGroup(group);

            CreateCollectionView(DataGridGroupd);
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            ClearList();
        }

        private void OnFilterClick(object sender, RoutedEventArgs e)
        {
            ViewModel.FilterGroups();

            ClearList();
        }

        private void ClearList()
        {
            DataGridGroupd.ItemsSource = null;

            if (ViewModel.ClearCommand.CanExecute(null))
            {
                ViewModel.ClearCommand.Execute(null);
            }
            CreateCollectionView(DataGridGroupd);
        }
    }
}
