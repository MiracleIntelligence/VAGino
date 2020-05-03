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

using ReadlnLibrary.Core.Collections;

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

using Telerik.Data.Core;
using Telerik.UI.Xaml.Controls.Grid;

using VAGino.Models;
using VAGino.ViewModels;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

            var warningSuppression = this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => this.InitializeSelectedColumnListSelection());
            this.dataGrid.Columns.CollectionChanged += this.Columns_CollectionChanged;


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
            CreateCollectionView(sender as RadDataGrid);
        }

        private void CreateCollectionView(RadDataGrid grid)
        {
            MessageGroups.Source = null;
            MessageGroups.Source = ViewModel.Messages;
            grid.ItemsSource = MessageGroups.View;
        }

        private void ClearList()
        {
            if (ViewModel.ClearCommand.CanExecute(null))
            {
                ViewModel.ClearCommand.Execute(null);
            }
        }

        private void FilterNew()
        {
            dataGrid.ItemsSource = null;

            ViewModel.FilterItems();

            CreateCollectionView(dataGrid);
        }

        private void DeleteAllGroups()
        {
            ViewModel.DeleteRecords();
            CreateCollectionView(dataGrid);
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

        private void ListViewSwipeContainer_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            if (ViewModel.DeleteFilterCommand != null)
            {
                ViewModel.DeleteFilterCommand.Execute((sender as FrameworkElement).DataContext);
            }
        }

        #region Telerik
        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var warningSuppression = this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => this.InitializeSelectedColumnListSelection());
        }

        private void InitializeSelectedColumnListSelection()
        {
            if (selectedColumnList.Items.Count > 0)
            {
                selectedColumnList.SelectedIndex = 0;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.dataGrid.FilterDescriptors.Clear();
            string text = ((TextBox)sender).Text;

            if (!string.IsNullOrEmpty(text))
            {
                this.dataGrid.FilterDescriptors.Add(new DelegateFilterDescriptor() { Filter = new EmployeeSearchFilter(text, selectedColumnList.SelectedItem as DataGridTypedColumn) });
            }
        }


        private class EmployeeSearchFilter : IFilter
        {
            private string matchString;

            private DataGridTypedColumn column;

            public EmployeeSearchFilter(string match, DataGridTypedColumn column)
            {
                this.matchString = match;
                this.column = column;
            }

            public bool PassesFilter(object item)
            {
                var model = item as MessageRow;

                if (column == null)
                {
                    return false;
                }

                switch (column.PropertyName)
                {
                    case "Id":
                        return model.Id.Contains(this.matchString, StringComparison.OrdinalIgnoreCase);
                    case "Dlc":
                        return model.DLC == Int32.Parse(this.matchString);
                    case "Message":
                        return model.Message.Contains(this.matchString, StringComparison.OrdinalIgnoreCase);                    
                    default:
                        break;
                }

                return false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FilterText.Text = string.Empty;
        }

        private void selectedColumnList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.dataGrid.FilterDescriptors.Clear();
            string text = FilterText.Text;

            if (!string.IsNullOrEmpty(text))
            {
                this.dataGrid.FilterDescriptors.Add(new DelegateFilterDescriptor() { Filter = new EmployeeSearchFilter(text, selectedColumnList.SelectedItem as DataGridTypedColumn) });
            }
        }


        #endregion
    }
}
