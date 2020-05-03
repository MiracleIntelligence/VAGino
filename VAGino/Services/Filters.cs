using Microsoft.Toolkit.Helpers;

using System;
using System.Collections.ObjectModel;
using System.Linq;

using VAGino.Models;

namespace VAGino.Services
{
    public class Filters : ObservableCollection<string>
    {
        public void Init()
        {
            var items = Singleton<DBService>.Instance.Connection.Table<FilteredBlock>().ToList();
            foreach (var item in items)
            {
                base.Add(item.Id);
            }
        }

        public new void Add(string id)
        {
            var filter = new FilteredBlock()
            {
                Id = id
            };
            base.Add(id);
            Singleton<DBService>.Instance.Connection.InsertOrReplace(filter);
        }

        public new void Remove(string id)
        {
                base.Remove(id);
                Singleton<DBService>.Instance.Connection.Delete<FilteredBlock>(id);
        }
    }
}
