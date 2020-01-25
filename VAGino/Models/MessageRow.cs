using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace VAGino.Models
{
    public class MessageRow : ObservableObject
    {
        private int _count;
        private string _name;

        public string Name { get => _name; set => Set(ref _name, value, nameof(Name)); }
        public string Message { get; set; }

        public int Count { get => _count; set => Set(ref _count, value, nameof(Count)); }
    }
}
