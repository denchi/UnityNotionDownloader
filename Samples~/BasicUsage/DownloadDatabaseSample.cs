using UnityEngine;
using Game.Serialization;
using Game.Serialization.Notion;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UniRx;

public class DownloadDatabaseSample : MonoBehaviour
{
    public string apiKey;
    public string notionVersion = "2022-06-28";
    public string databaseId;

    void Start()
    {
        var cache = new DatabaseCache();
        var downloader = new NotionDownloader(cache, apiKey, notionVersion);
        downloader
            .DownloadDatabaseToListObservable<Dictionary<string, object>>(databaseId, jo => jo.ToObject<Dictionary<string, object>>(), true)
            .Subscribe(list => Debug.Log($"Downloaded {list.Count} rows"));
    }
}
