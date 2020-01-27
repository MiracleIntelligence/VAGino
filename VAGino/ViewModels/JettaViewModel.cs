using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using MessageAnalyzer.Models;
using ReadlnLibrary.Core.Collections;
using VAGino.Models;

namespace VAGino.ViewModels
{
    public class JettaViewModel : ViewModelBase
    {
        public GroupedObservableCollection<string, MessageRow> Messages { get; private set; }
        public object AddMessage { get; internal set; }

        public JettaViewModel()
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

            ClearGroups();
        }

        internal void AddCanMessage(CANMessage message)
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
            Messages.Add(row);
        }

        internal void ClearGroups()
        {
            Messages = new GroupedObservableCollection<string, MessageRow>((m) => m.Id);
            //Messages.ClearItems();
        }
    }
}
