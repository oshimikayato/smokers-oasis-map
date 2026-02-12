using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 検索統計を管理するクラス
/// PlayerPrefsを使用して検索履歴を保存し、上位カテゴリーを優先表示
/// </summary>
public class SearchStatistics : MonoBehaviour
{
    private const string SEARCH_COUNT_PREFIX = "search_count_";
    private const string LAST_SEARCH_TIME_PREFIX = "last_search_";
    
    private static SearchStatistics _instance;
    public static SearchStatistics Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SearchStatistics>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SearchStatistics");
                    _instance = obj.AddComponent<SearchStatistics>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// 検索回数を記録
    /// </summary>
    public void RecordSearch(string category)
    {
        if (string.IsNullOrEmpty(category)) return;
        
        string key = SEARCH_COUNT_PREFIX + category.ToLower();
        int count = PlayerPrefs.GetInt(key, 0);
        PlayerPrefs.SetInt(key, count + 1);
        
        string timeKey = LAST_SEARCH_TIME_PREFIX + category.ToLower();
        PlayerPrefs.SetString(timeKey, System.DateTime.Now.ToString("o"));
        
        PlayerPrefs.Save();
        
        Debug.Log($"[SearchStats] Recorded search for '{category}'. Total: {count + 1}");
    }
    
    /// <summary>
    /// 複数カテゴリーの検索を記録
    /// </summary>
    public void RecordMultiSearch(IEnumerable<string> categories)
    {
        foreach (var cat in categories)
        {
            RecordSearch(cat);
        }
    }
    
    /// <summary>
    /// カテゴリーの検索回数を取得
    /// </summary>
    public int GetSearchCount(string category)
    {
        if (string.IsNullOrEmpty(category)) return 0;
        return PlayerPrefs.GetInt(SEARCH_COUNT_PREFIX + category.ToLower(), 0);
    }
    
    /// <summary>
    /// カテゴリーリストを検索回数順にソート
    /// </summary>
    public List<string> SortBySearchCount(List<string> categories)
    {
        return categories
            .OrderByDescending(c => {
                if (c.ToLower() == "all") return int.MaxValue; // "All" は常に先頭
                return GetSearchCount(c);
            })
            .ToList();
    }
    
    /// <summary>
    /// 検索統計を取得（デバッグ用）
    /// </summary>
    public Dictionary<string, int> GetAllStatistics(List<string> categories)
    {
        var stats = new Dictionary<string, int>();
        foreach (var cat in categories)
        {
            stats[cat] = GetSearchCount(cat);
        }
        return stats;
    }
    
    /// <summary>
    /// 統計をリセット
    /// </summary>
    public void ResetStatistics(List<string> categories)
    {
        foreach (var cat in categories)
        {
            string key = SEARCH_COUNT_PREFIX + cat.ToLower();
            PlayerPrefs.DeleteKey(key);
            string timeKey = LAST_SEARCH_TIME_PREFIX + cat.ToLower();
            PlayerPrefs.DeleteKey(timeKey);
        }
        PlayerPrefs.Save();
        Debug.Log("[SearchStats] All statistics reset.");
    }
}
