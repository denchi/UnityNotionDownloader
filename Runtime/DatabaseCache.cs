using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using Game.Models;

namespace Game.Serialization
{
    /// <summary>
    /// A simple cache for database and items.
    /// </summary>
    public class DatabaseCache
    {
        private Dictionary<string, object> _databaseCache = new Dictionary<string, object>();
        private Dictionary<string, object> _itemsCache = new Dictionary<string, object>();

        public List<T> GetTableFromCache<T>(string databaseId) where T: class
        {
            _databaseCache.TryGetValue(databaseId, out var list);
            
            var typesList = list as List<T>;
            if (typesList == null)
            {
                _databaseCache[databaseId] = typesList = (list as IList)!.Cast<T>().ToList();
            }
            return typesList;
        }
        
        public IList<T> GetItemsFromCache<T>(Func<T, bool> conditionFunc)
        {
            foreach (var (_, list) in _databaseCache)
            {
                if (list == null)
                    return default;

                if (list is List<object> {Count: > 0} listO && listO.First() is T)
                {
                    var listT = listO.OfType<T>().ToList();
                    return listT.FindAll(x => conditionFunc(x));
                }
            }

            return default;
        }
        
        public T GetItemFromCache<T>(string guid) where T: class
        {
            _itemsCache.TryGetValue(guid, out var item);
            if (item == null)
                return default;
            
            var itemT = item as T;
            return itemT;
        }
        
        public void SetTableFromCache<T>(string databaseId, object list) where T: class
        {
            _databaseCache[databaseId] = list;
        }

        public bool ContainsDatabase(string databaseId)
        {
            return _databaseCache.ContainsKey(databaseId);
        }

        public void AddItemToCache<T>(string guid, T obj)
        {
            _itemsCache[guid] = obj;
        }
    }
}