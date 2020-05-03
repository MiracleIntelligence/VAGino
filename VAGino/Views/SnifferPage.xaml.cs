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

using Microsoft.Toolkit.Uwp.UI.Controls;

using ReadlnLibrary.Core.Collections;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using VAGino.Models;
using VAGino.ViewModels;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VAGino
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SnifferPage : Page
    {

        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        private MainPage rootPage = MainPage.Current;
        private SnifferViewModel ViewModel { get; }
        private bool _collapseNewGroup = true;


        // Indicate if we navigate away from this page or not.
        private Boolean IsNavigatedAway;
        private Paragraph _paragraph;

        public SnifferPage()
        {
            this.InitializeComponent();
            ViewModel = new SnifferViewModel();
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

        private Grouping<string, MessageRow> _targetGroup;

        private async void SendCommand()
        {
            var cmd = TBCommand.Text;
            await SendCommand(cmd);
        }

        private async Task SendCommand(string cmd)
        {
            if (String.IsNullOrEmpty(cmd))
            {
                return;
            }

            TBCommand.Text = String.Empty;

            //OnStopTracking(null, null);
            //await WriteConsole(cmd);
            if (ViewModel.SendRawCommand?.CanExecute(cmd) == true)
            {
                ViewModel.SendRawCommand.Execute(cmd);
            }
        }

        //private async Task WriteConsole(string str)
        //{
        //    await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
        //    {
        //        var run = new Run();
        //        run.Text = str;

        //        if (_paragraph == null)
        //        {
        //            _paragraph = new Paragraph();
        //            RTBConsole.Blocks.Add(_paragraph);
        //        }

        //        _paragraph.Inlines.Add(run);
        //        _paragraph.Inlines.Add(new LineBreak());


        //        SVConsole.ChangeView(null, SVConsole.ActualHeight, null);
        //    }));
        //}

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

            _targetGroup = group;
            FlyoutGroup.ShowAt(sender as FrameworkElement);
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

        private void FilterNew()
        {
            DataGridGroupd.ItemsSource = null;

            ViewModel.FilterItems();

            CreateCollectionView(DataGridGroupd);
        }

        private void DeleteAllGroups()
        {
            ViewModel.DeleteRecords();
            CreateCollectionView(DataGridGroupd);
        }

        private void OnDeleteGroup(object sender, RoutedEventArgs e)
        {
            DataGridGroupd.ItemsSource = null;

            ViewModel.RemoveGroup(_targetGroup);

            CreateCollectionView(DataGridGroupd);
        }

        private void OnDeleteFilter(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (ViewModel.DeleteFilterCommand != null)
            {
                ViewModel.DeleteFilterCommand.Execute(args.SwipeControl.DataContext);
            }
        }

        private void OnTBFilterKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (ViewModel.AddFilterCommand != null && !String.IsNullOrEmpty(TBFilter.Text))
                {
                    ViewModel.AddFilterCommand.Execute(TBFilter.Text);
                    TBFilter.Text = null;
                }
            }
        }

        private void OnDeleteAndFilterGroup(object sender, RoutedEventArgs e)
        {
            DataGridGroupd.ItemsSource = null;

            ViewModel.RemoveGroupAndFilter(_targetGroup);

            CreateCollectionView(DataGridGroupd);
        }

        private void ListViewSwipeContainer_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            if (ViewModel.DeleteFilterCommand != null)
            {
                ViewModel.DeleteFilterCommand.Execute((sender as FrameworkElement).DataContext);
            }
        }
    }
}
