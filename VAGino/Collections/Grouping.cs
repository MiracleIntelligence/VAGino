using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;


namespace ReadlnLibrary.Core.Collections
{
    public class Grouping<TKey, TElement> : ObservableCollection<TElement>, IGrouping<TKey, TElement>
    {
        public Grouping(TKey key)
        {
            this.Key = key;
            this.CollectionChanged += Grouping_CollectionChanged;
        }

        private void Grouping_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(KeyData)));
        }

        public Grouping(TKey key, IEnumerable<TElement> items)
            : this(key)
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        public TKey Key { get; }

        public string KeyData => $"{Key} {Count}";
    }
}