using Microsoft.VisualStudio.Language.Suggestions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Model
{
    public static class DataExtensions
    {
        public static void Replace<T>(this ObservableCollection<T> collection, IEnumerable<T> data)
        {
            var items = data.ToArray();
            //collection.Move
            for (int i = 0; i < items.Length; i++)
            {
                var c = i < collection.Count ? collection[i] : default;
                //if are same, continue
                if (EqualityComparer<T>.Default.Equals(c, items[i]))
                    continue;
                //if item is in collection, move it to position i
                var idx = collection.IndexOf(items[i]);
                if (idx >= 0)
                {
                    collection.Move(idx, i);
                    continue;
                }
                //add it to position i
                collection.Insert(i, items[i]);
            }
            //remove extra items
            while (collection.Count > items.Length)
                collection.RemoveAt(collection.Count - 1);
        }

        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            foreach (var item in values)
                set.Add(item);
        }
        public static void AddRange<T>(this HashSet<T> set, params T[] values)
        {
            set.AddRange(values);
        }
    }
}
