using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using MessageAnalyzer.Models;

namespace VAGino.Models
{
    public class MessageRow : ObservableObject
    {
        private int _count;
        private string _name;
        private string _id;
        private int _dlc;
        private string _data;
        private string _message;

        public MessageRow()
        { }
        public MessageRow(CANMessage message)
        {
            _count = 1;
            _message = message.Message;
            _id = message.Id;
            _dlc = message.DLC;
            _data = String.Join(' ', message.Data);
        }

        public string Id
        {
            get => _id;
            set => Set(ref _id, value, nameof(Id));
        }

        public int DLC
        {
            get => _dlc;
            set => Set(ref _dlc, value, nameof(DLC));
        }

        public string Data
        {
            get => _data;
            set => Set(ref _data, value, nameof(Data));
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value, nameof(Name));
        }
        public string Message
        {
            get => _message;
            set => Set(ref _message, value, nameof(Message));
        }

        public int Count
        {
            get => _count;
            set => Set(ref _count, value, nameof(Count));
        }
    }
}
