using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Serialization.Notion
{
    /// <summary>
    /// <see cref="https://developers.notion.com/docs/working-with-databases" />
    /// </summary>
    public abstract class NotionDataContainer : IDataContainer
    {
        public NotionDataContainer(string notionApiKey, string notionVersion)
        {
            DatabaseCache = new DatabaseCache();
            NotionDownloader = new NotionDownloader(DatabaseCache, notionApiKey, notionVersion);
        }

        #region IDataContainer Implementation
        
        protected NotionDownloader NotionDownloader { get; set; }
        protected DatabaseCache DatabaseCache { get; set; }
        
        /// <summary>
        /// Creates a list of deserializers for different Notion database tables.
        /// </summary>
        /// <returns>Tuples with id of the notion table, deserializer object and an action to do after complete</returns>
        // Ex:
        // => new ()
        // {
        //     new (StringDbId, new StringIdEntityDeserializer(), list => { _strings = new List<StringID>(list.Cast<StringID>()); }),
        //     new (ItemsDbId, new ItemEntityDeserializer(), list =>
        //     {
        //         _items = new List<IItemModel>(list.Cast<IItemModel>());
        //         _upgradeLevels
        //             .Where(u => u.costItems != null)
        //             .ForEach(u => u.costItems.ForEach(c =>
        //             {
        //                 var itemGuid = c.itemId;
        //                 var item = _databaseCache.GetItemFromCache<InventoryItem>(itemGuid);
        //                 if (item == null)
        //                     return;
        //              
        //                 c.itemId = item.id;
        //             }));
        //     }),
        // };
        protected abstract List<Tuple<string, IDataDeserializer, Action<IList>>> CreateDeserializersList();
        
        /// <summary>
        /// Initializes a deserializer right after creation.
        /// </summary>
        /// <param name="deserializer">The deserializer to initialize</param>
        protected abstract void InitDeserializer(INotionDeserializer deserializer);

        public void Load(ILoadsInBackground notifier)
        {
            notifier?.StartLoading();
            
            Observable
                .FromCoroutine(() => LoadAllCo(notifier))
                .DoOnCompleted(() => notifier?.EndLoading())
                .Subscribe();
        }

        #endregion

        private List<Tuple<string, IDataDeserializer, Action<IList>>> CreateDeserializers()
        {
            var tables = CreateDeserializersList();
            var deserializers = tables
                .Select(table => table.Item2)
                .OfType<INotionDeserializer>()
                .ToList();
            
            for (var i = 0; i < deserializers.Count; i++)
            {
                InitDeserializer(deserializers[i]);
            }

            return tables;
        }

        private IEnumerator LoadAllCo(ILoadsInBackground notifier)
        {
            notifier?.UpdateLoading(0.0f);

            // yield return new WaitWhile(() => jAssetsLinks == null);
            //
            // _databaseCache = new DatabaseCache();
            // _notionValueReader = new NotionValueReader();
            // _notionDownloader = new NotionDownloader(_databaseCache);
            // yield return new WaitForSeconds(1);
            
            var tables = CreateDeserializers();
            var chainedObservable = Enumerable
                .Range(0, tables.Count)
                .Select(LoadTable)
                .Concat()
                .ToArray();

            yield return Observable
                .FromCoroutine(WaitUntilEnd)
                .ToYieldInstruction();
            
            notifier?.UpdateLoading(1.0f);
            
            IObservable<IList<object>> LoadTable(int idx)
            {
                var (databaseId, databaseItem, _) = tables[idx];
                var ratio = ((float)idx) / (tables.Count + 1);

                var counter = new Stopwatch();
                counter.Start();
                
                var result = LoadDatabase(databaseId, databaseItem.CreateObject)
                    .DoOnCompleted(() =>
                    {
                        notifier?.UpdateLoading(ratio);
                        counter.Stop();
                        Debug.Log($"Loading {databaseId} took {counter.ElapsedMilliseconds} ms");
                    });

                return result;
            }

            IObservable<IList<T>> LoadDatabase<T>(string databaseId, Func<JObject, T> createAction) where T : class
            {
                return NotionDownloader.DownloadDatabaseToListObservable<T>(
                    databaseId,
                    createAction,
                    allowCaching: true);
            }

            IEnumerator WaitUntilEnd()
            {
                var didEnd = false;
                chainedObservable.Subscribe(result =>
                {
                    for (var i = 0; i < tables.Count; i++)
                    {
                        HandleDeserialization(i);
                    }
                    
                    didEnd = true;
                    
                    void HandleDeserialization(int idx)
                    {
                        var temp = result[idx];
                        var resultList = (IList) temp;
                        var (_, deserializer, actionOnEndDeserializing) = tables[idx];
                        actionOnEndDeserializing(resultList);

                        if (deserializer is IDataDeserializerEvents dataDeserializer) 
                            dataDeserializer.OnFinished(resultList);
                    }
                });

                yield return new WaitUntil(() => didEnd);
            }
        }
    }
}