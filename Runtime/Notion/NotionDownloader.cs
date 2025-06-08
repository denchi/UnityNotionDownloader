using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Serialization.Notion
{
    public class NotionDownloader
    {
        private string _notionApiKey;
        private string _notionVersion;
        
        private readonly DatabaseCache _databaseCache;

        public NotionDownloader(DatabaseCache databaseCache, string notionApiKey, string notionVersion)
        {
            _databaseCache = databaseCache;
            _notionApiKey = notionApiKey;
            _notionVersion = notionVersion;
        }
        
        public IObservable<List<T>> DownloadDatabaseToListObservable<T>(string databaseId, Func<JObject,T> createAction, bool allowCaching) where T: class
        {
            var downloader = new NotionDatabaseTableDownloader<T>(
                _databaseCache,
                databaseId,
                allowCaching,
                createAction,
                _notionApiKey,
                _notionVersion);

            return downloader.DownloadDatabaseToListObservable();
        }

        public T DownloadPage<T>(string id, Func<JObject,T> createFunc, bool cache = true)
        {
            var url = $"https://api.notion.com/v1/pages/{id}";
            
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Notion-Version", _notionVersion);
            request.SetRequestHeader("Authorization", $"Bearer {_notionApiKey}");
            
            Debug.Log($"[NotionDownloader] Downloading page at: {id}...");
            
            request.SendWebRequest();

            while (!request.isDone) { }

            Debug.Log($"[NotionDownloader] Downloading page done");
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                return default;
            }
            
            try
            {
                var page = ReadPage();
                return page;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            
            return default;

            T ReadPage()
            {
                Debug.Log($"[NotionDownloader] Reading page {id}...");

                var jsonResponse = request.downloadHandler.text;
                if (Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse) is JObject page)
                {
                    Debug.Log($"[NotionDownloader] Dea=serialized page {id}");
                    
                    var properties = page["properties"] as JObject;

                    var guid = (string)page["id"];
                    if (properties != null)
                    {
                        properties["guid"] = guid;
                        Debug.Log($"[NotionDownloader] Creating func for page {id}...");
                        var obj = createFunc(properties);

                        if (cache)
                            _databaseCache.AddItemToCache(guid, obj);
                        
                        return obj;
                    }
                }

                return default;
            }
        }
    }
}