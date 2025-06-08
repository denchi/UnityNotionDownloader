using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using Sirenix.Utilities;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Serialization.Notion
{
    public class NotionDatabaseTableDownloader<T> where T: class
    {
        private const int MaxTries = 5;
        
        private readonly string _databaseId;
        private readonly DatabaseCache _databaseCache;
        private readonly bool _allowCaching;
        private readonly Func<JObject,T> _createFunc;
        private readonly string _notionApiKey;
        private readonly string _notionVersion;
        private string _filter;

        public NotionDatabaseTableDownloader(
            DatabaseCache databaseCache, 
            string databaseId, 
            bool allowCaching, 
            Func<JObject,T> createFunc, 
            string apiKey, 
            string version, 
            string filter = null)
        {
            _databaseCache = databaseCache;
            _databaseId = databaseId;
            _allowCaching = allowCaching;
            _createFunc = createFunc;
            _filter = filter;
            _notionApiKey = apiKey;
            _notionVersion = version;
        }
        
        public IObservable<List<T>> DownloadDatabaseToListObservable() 
        {
            // Check cached value first!
            if (_allowCaching && _databaseCache.ContainsDatabase(_databaseId))
                return Observable.Return(_databaseCache.GetTableFromCache<T>(_databaseId));
            return Observable.FromCoroutine<List<T>>(WaitAndReturnCo);
        }

        private IEnumerator WaitAndReturnCo(IObserver<List<T>> observer, CancellationToken cancellationToken)
        {
            // CHeck if needs to update by downloading and checking last edited database time
            var shouldUpdate = false;
            var lastUpdateTime = "";
            yield return ShouldDownloadNewData(new SimpleObserver<(bool, string)>(
                onNext: (update) =>
                {
                    shouldUpdate = update.Item1;
                    lastUpdateTime = update.Item2;
                },
                onError: observer.OnError
            ), cancellationToken);

            var list = new List<T>();

            if (shouldUpdate)
            {
                var resultsAcrossCursors = new JArray();
                
                yield return RequestPagesCo(new SimpleObserver<(JArray, List<T>)>(
                    onNext: (update) =>
                    {
                        list.AddRange(update.Item2);
                        resultsAcrossCursors.AddRange(update.Item1);
                    },
                    onError: observer.OnError
                ), cancellationToken);
                
                RequestCache.CacheDatabase(_databaseId, resultsAcrossCursors.ToString());
                RequestCache.SetLastEditedTime(_databaseId, lastUpdateTime);
            }
            else
            {
                Debug.LogWarning($"[NotionDataContainer] Skipping Downloading {_databaseId}... Loading from cache...");
                
                // We skipped downloading and instead reading data from player prefs cache
                RequestCache.GetCachedDatabase(_databaseId, out var jsonString);
                
                // Parse the cache into an array on another thread
                JArray results = null;
                yield return Observable.Start(() =>
                {
                    results = JArray.Parse(jsonString);    
                }).ToYieldInstruction();
                
                // Read all pages
                foreach (var page in results)
                {
                    ReadPage(page as JObject, list);
                }
            }

            if (_allowCaching)
            {
                _databaseCache.SetTableFromCache<T>(_databaseId, list);
            }

            // Notify the observer about the result.
            observer.OnNext(list);
            observer.OnCompleted();
        }
        
        private IEnumerator RequestPagesCo(IObserver<(JArray, List<T>)> observer, CancellationToken cancellationToken)
        {
            var prevCursor = default(string);
            var resultsAcrossCursors = new JArray();
            var list = new List<T>();
            do
            {
                // Check has a cursor for multi-page downloads
                string jsonData = null;
                if (prevCursor != null)
                {
                    jsonData = "{ \"start_cursor\": \"" + prevCursor + "\"\n }";
                    prevCursor = null;
                }

                // Create a request for the current cursor / page
                // Try again up to 5 times
                // Wait 2 seconds between retries
                UnityWebRequest request = null;
                Exception lastException = null;
                for (var i = 0; i < MaxTries; i++)
                {
                    request = CreateRequest(jsonData);
                    yield return request.SendWebRequest();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        observer.OnError(new Exception("Canceled by user"));
                        yield break;
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        lastException = null;
                        break;
                    }

                    // Notify the observer about the error.
                    lastException = new Exception(request.error);

                    yield return new WaitForSeconds(2);
                }

                if (request == null)
                {
                    observer.OnError(new Exception("Error creating request"));
                    yield break;
                }
                
                // Check last exception
                if (lastException != null)
                {
                    observer.OnError(lastException);
                    yield break;
                }

                // Deserialize response results
                var jsonResponse = request.downloadHandler.text;
                JArray results = null;
                JObject result = null;
                try
                {
                    result = GetDeserializedResult(jsonResponse, out results);
                    if (result == null || results == null)
                    {
                        throw new Exception("Error Deserializing");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NotionDataContainer] error: {ex.Message}");

                    prevCursor = null;
                    results = null;

                    // Notify the observer about the error.
                    observer.OnError(ex);
                }

                ReadPagesFromResults(results, resultsAcrossCursors, list);

                var hasMore = (bool) result?["has_more"];
                prevCursor = hasMore ? (string) result?["next_cursor"] : null;
            } while (prevCursor != null);
            
            observer.OnNext((resultsAcrossCursors, list));
            observer.OnCompleted();
        }
        
        private IEnumerator ShouldDownloadNewData(IObserver<(bool, string)> observer, CancellationToken cancellationToken)
        {
            var notionDatabaseUrl = $"https://api.notion.com/v1/databases/{_databaseId}";
            using var unityWebRequest = new UnityWebRequest(notionDatabaseUrl, "GET");
            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            unityWebRequest.SetRequestHeader("Notion-Version", _notionVersion);
            unityWebRequest.SetRequestHeader("Authorization", $"Bearer {_notionApiKey}");
            unityWebRequest.SetRequestHeader("Content-Type", "application/json");

            yield return unityWebRequest.SendWebRequest();

            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                observer.OnError(new Exception(unityWebRequest.error));
                yield break;
            }

            var response = unityWebRequest.downloadHandler.text;
            var json = JObject.Parse(response);
            var lastEditedTime = json["last_edited_time"]?.ToString();

            // Compare with cached last edited time
            observer.OnNext((RequestCache.GetLastEditedTime(_databaseId) != lastEditedTime, lastEditedTime)); // Data has changed, need to update
            // Data hasn't changed, use cached data
            observer.OnCompleted();
        }

        private UnityWebRequest CreateRequest(string json)
        {
            var notionDatabaseUrl = $"https://api.notion.com/v1/databases/{_databaseId}/query";
            var unityWebRequest = new UnityWebRequest(notionDatabaseUrl, "POST");
            if (json != null)
            {
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                unityWebRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            unityWebRequest.redirectLimit = 32;

            unityWebRequest.SetRequestHeader("Notion-Version", _notionVersion);
            unityWebRequest.SetRequestHeader("Authorization", $"Bearer {_notionApiKey}");
            unityWebRequest.SetRequestHeader("Content-Type", "application/json");
            return unityWebRequest;
        }
        
        private void ReadPage(JObject page, List<T> list)
        {
            try
            {
                var properties = page["properties"] as JObject;
                if (properties == null)
                    return;

                var guid = (string)page["id"];
                properties["guid"] = guid;

                Debug.Log($"[NotionDataContainer] Create Func...");
                var obj = _createFunc(properties);
                Debug.Log($"[NotionDataContainer] Done Create Func");

                if (_allowCaching)
                    _databaseCache.AddItemToCache(guid, obj);

                list.Add(obj);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NotionDataContainer] Error Reading Page! - {e.Message}");
            }
        }
        
        private void ReadPagesFromResults(JArray results, JArray resultsAcrossCursors, List<T> list)
        {
            foreach (var page in results)
            {
                resultsAcrossCursors.Add(page);
                ReadPage(page as JObject, list);
            }
        }

        private JObject GetDeserializedResult(string json, out JArray resultsArray)
        {
            var resultObject = Newtonsoft.Json.JsonConvert.DeserializeObject(json) as JObject;
            if (resultObject == null)
                throw new Exception("Invalid result!");

            resultsArray = resultObject["results"] as JArray;
            if (resultsArray == null)
                throw new Exception("Invalid results!");
            
            return resultObject;
        }
    }
}