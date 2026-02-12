using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

namespace NRKernal
{
    /// <summary>
    /// 天気予報マネージャー - 気象庁APIから天気データを取得
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject weatherPanel;
        public Text weatherTitleText;
        public Text weatherContentText;
        public Text weatherDateText;
        public Button closeWeatherButton;
        public Button refreshWeatherButton;
        
        [Header("Region Settings UI")]
        public GameObject regionSettingsPanel;
        public Button closeRegionButton;
        
        [Header("Top Bar Weather Display")]
        public Text topBarWeatherText; // トップバーに表示する簡易天気
        public Image topBarWeatherIcon; // 天気アイコン画像
        public SpriteNumberDisplay temperatureSpriteDisplay; // 気温のスプライト表示（オプション）
        
        [Header("Settings")]
        public string areaCode = "270000"; // 大阪 (デフォルト)
        public float autoRefreshInterval = 1800f; // 30分ごとに自動更新
        
        private WeatherData _currentWeather;
        private bool _isLoading = false;
        
        // 地域コード一覧（主要都市）
        public static readonly Dictionary<string, string> AreaCodes = new Dictionary<string, string>
        {
            {"北海道", "016000"},
            {"青森", "020000"},
            {"岩手", "030000"},
            {"宮城", "040000"},
            {"秋田", "050000"},
            {"山形", "060000"},
            {"福島", "070000"},
            {"茨城", "080000"},
            {"栃木", "090000"},
            {"群馬", "100000"},
            {"埼玉", "110000"},
            {"千葉", "120000"},
            {"東京", "130000"},
            {"神奈川", "140000"},
            {"新潟", "150000"},
            {"富山", "160000"},
            {"石川", "170000"},
            {"福井", "180000"},
            {"山梨", "190000"},
            {"長野", "200000"},
            {"岐阜", "210000"},
            {"静岡", "220000"},
            {"愛知", "230000"},
            {"三重", "240000"},
            {"滋賀", "250000"},
            {"京都", "260000"},
            {"大阪", "270000"},
            {"兵庫", "280000"},
            {"奈良", "290000"},
            {"和歌山", "300000"},
            {"鳥取", "310000"},
            {"島根", "320000"},
            {"岡山", "330000"},
            {"広島", "340000"},
            {"山口", "350000"},
            {"徳島", "360000"},
            {"香川", "370000"},
            {"愛媛", "380000"},
            {"高知", "390000"},
            {"福岡", "400000"},
            {"佐賀", "410000"},
            {"長崎", "420000"},
            {"熊本", "430000"},
            {"大分", "440000"},
            {"宮崎", "450000"},
            {"鹿児島", "460100"},
            {"沖縄", "471000"}
        };
        [Header("Toast Notification")]
        public GameObject weatherToast; // 画面下部の横長トーストパネル
        public Text weatherToastText; // トースト内のテキスト
        public float toastDisplayDuration = 5f; // 表示秒数
        private Coroutine _toastCoroutine;
        
        void Start()
        {
            // Setup Listeners
            if (closeWeatherButton != null)
            {
                closeWeatherButton.onClick.RemoveAllListeners();
                closeWeatherButton.onClick.AddListener(HideWeatherPanel);
            }
            
            if (refreshWeatherButton != null)
            {
                refreshWeatherButton.onClick.RemoveAllListeners();
                refreshWeatherButton.onClick.AddListener(RefreshWeather);
            }

            if (closeRegionButton != null)
            {
                closeRegionButton.onClick.RemoveAllListeners();
                closeRegionButton.onClick.AddListener(HideRegionPanel);
            }

            // 起動時に天気を取得（トースト表示なし、右上ウィジェットのみ更新）
            Debug.Log("[WeatherManager] Starting - updating top bar weather only (no toast)...");
            RefreshWeather(); // トーストなしで天気更新
            
            // 自動更新は無効化（ユーザーが手動で更新）
            // if (autoRefreshInterval > 0)
            // {
            //     InvokeRepeating("RefreshWeatherAndShowToast", autoRefreshInterval, autoRefreshInterval);
            // }
        }
        
        /// <summary>
        /// 天気を更新してトースト通知を表示
        /// </summary>
        public void RefreshWeatherAndShowToast()
        {
            if (!_isLoading)
            {
                StartCoroutine(FetchWeatherAndShowToastRoutine());
            }
        }
        
        IEnumerator FetchWeatherAndShowToastRoutine()
        {
            _isLoading = true;
            
            string url = $"https://www.jma.go.jp/bosai/forecast/data/forecast/{areaCode}.json";
            Debug.Log($"[WeatherManager] Fetching weather for toast from: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    ParseWeatherDataForToast(json);
                }
                else
                {
                    Debug.LogWarning($"[WeatherManager] Toast fetch failed: {request.error}");
                    if (weatherToastText != null)
                    {
                        weatherToastText.text = "天気取得失敗";
                    }
                }
            }
            
            // トースト表示
            ShowToast();
            _isLoading = false;
        }
        
        void ParseWeatherDataForToast(string json)
        {
            try
            {
                // 地域名を取得
                string areaName = GetCurrentAreaName();
                
                // 天気を抽出
                string weather = "---";
                int startIdx = json.IndexOf("\"weathers\"");
                if (startIdx > 0)
                {
                    int arrStart = json.IndexOf("[", startIdx);
                    int arrEnd = json.IndexOf("]", arrStart);
                    if (arrStart > 0 && arrEnd > arrStart)
                    {
                        string arr = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
                        string[] items = arr.Split(',');
                        if (items.Length > 0)
                        {
                            weather = items[0].Trim().Trim('"');
                            if (weather.Length > 15) weather = weather.Substring(0, 15) + "...";
                        }
                    }
                }
                
                // 気温を抽出
                string tempMax = "--";
                int tempIdx = json.IndexOf("\"tempsMax\"");
                if (tempIdx > 0)
                {
                    int tArrStart = json.IndexOf("[", tempIdx);
                    int tArrEnd = json.IndexOf("]", tArrStart);
                    if (tArrStart > 0 && tArrEnd > tArrStart)
                    {
                        string arr = json.Substring(tArrStart + 1, tArrEnd - tArrStart - 1);
                        string[] temps = arr.Split(',');
                        foreach (string t in temps)
                        {
                            string cleaned = t.Trim().Trim('"');
                            if (!string.IsNullOrEmpty(cleaned) && cleaned != "null")
                            {
                                tempMax = cleaned;
                                break;
                            }
                        }
                    }
                }
                
                // トーストテキスト設定
                string emoji = GetWeatherEmoji(weather);
                if (weatherToastText != null)
                {
                    weatherToastText.text = $"{areaName} {emoji} {weather}  最高{tempMax}°C";
                }
                
                Debug.Log($"[WeatherManager] Toast: {areaName} {weather} {tempMax}°C");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeatherManager] Toast parse error: {e.Message}");
                if (weatherToastText != null)
                {
                    weatherToastText.text = "天気データエラー";
                }
            }
        }
        
        public void ShowToast()
        {
            if (weatherToast != null)
            {
                // 既存のコルーチンをキャンセル
                if (_toastCoroutine != null)
                {
                    StopCoroutine(_toastCoroutine);
                }
                
                weatherToast.SetActive(true);
                _toastCoroutine = StartCoroutine(HideToastAfterDelay());
            }
        }
        
        IEnumerator HideToastAfterDelay()
        {
            yield return new WaitForSeconds(toastDisplayDuration);
            if (weatherToast != null)
            {
                weatherToast.SetActive(false);
            }
            _toastCoroutine = null;
        }
        
        /// <summary>
        /// 地域を設定
        /// </summary>
        public void SetRegion(string regionName)
        {
            if (AreaCodes.ContainsKey(regionName))
            {
                areaCode = AreaCodes[regionName];
                _currentAreaName = regionName;
                Debug.Log($"[WeatherManager] Region set to: {regionName} ({areaCode})");
            }
            else
            {
                Debug.LogWarning($"[WeatherManager] Unknown region: {regionName}");
            }
        }
        
        /// <summary>
        /// 現在の地域名を取得
        /// </summary>
        public string GetCurrentRegionName()
        {
            return _currentAreaName;
        }
        
        private string _currentAreaName = "大阪";
        
        /// <summary>
        /// 地域設定パネルを表示
        /// </summary>
        public void ShowRegionPanel()
        {
            Debug.Log("[WeatherManager] ShowRegionPanel called");
            if (regionSettingsPanel != null)
            {
                regionSettingsPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[WeatherManager] regionSettingsPanel is null!");
            }
        }
        
        /// <summary>
        /// 地域設定パネルを非表示
        /// </summary>
        public void HideRegionPanel()
        {
            if (regionSettingsPanel != null)
            {
                regionSettingsPanel.SetActive(false);
            }
        }
        
        public void ShowWeatherPanel()
        {
            Debug.Log("[WeatherManager] ShowWeatherPanel called");
            if (weatherPanel != null)
            {
                Debug.Log("[WeatherManager] Setting weatherPanel active");
                weatherPanel.SetActive(true);
                
                // 右からスライドインアニメーション
                RectTransform panelRect = weatherPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    StartCoroutine(SlideAnimation(panelRect, 500f, 0f, 0.3f, false));
                }
                
                RefreshWeather();
            }
            else
            {
                Debug.LogError("[WeatherManager] weatherPanel reference is null!");
            }
        }
        
        public void HideWeatherPanel()
        {
            if (weatherPanel != null)
            {
                RectTransform panelRect = weatherPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    // 右へスライドアウトアニメーション
                    StartCoroutine(SlideAnimation(panelRect, panelRect.anchoredPosition.x, 500f, 0.25f, true));
                }
                else
                {
                    weatherPanel.SetActive(false);
                }
            }
        }
        
        IEnumerator SlideAnimation(RectTransform rect, float startX, float endX, float duration, bool hideOnComplete)
        {
            rect.anchoredPosition = new Vector2(startX, rect.anchoredPosition.y);
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out cubic
                float easedT = hideOnComplete ? t * t : (1f - Mathf.Pow(1f - t, 3f));
                float currentX = Mathf.Lerp(startX, endX, easedT);
                rect.anchoredPosition = new Vector2(currentX, rect.anchoredPosition.y);
                yield return null;
            }
            
            rect.anchoredPosition = new Vector2(endX, rect.anchoredPosition.y);
            
            if (hideOnComplete && weatherPanel != null)
            {
                weatherPanel.SetActive(false);
            }
        }
        
        public void RefreshWeather()
        {
            if (!_isLoading)
            {
                StartCoroutine(FetchWeatherRoutine());
            }
        }
        
        IEnumerator FetchWeatherRoutine()
        {
            _isLoading = true;
            
            if (weatherContentText != null)
            {
                weatherContentText.text = "読み込み中...";
            }
            
            // 気象庁API URL
            string url = $"https://www.jma.go.jp/bosai/forecast/data/forecast/{areaCode}.json";
            
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.timeout = 10;
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    ParseWeatherData(json);
                    UpdateUI();
                }
                else
                {
                    Debug.LogError($"Weather API Error: {www.error}");
                    if (weatherContentText != null)
                    {
                        weatherContentText.text = $"<color=#FF7777>⚠ 天気データの取得に失敗しました</color>\n{www.error}";
                    }
                }
            }
            
            _isLoading = false;
        }
        
        void ParseWeatherData(string json)
        {
            try
            {
                // 気象庁APIのJSONをパース（簡易版）
                _currentWeather = new WeatherData();
                
                // JSONを手動でパース（Unity標準のJsonUtilityは複雑な構造に対応しにくい）
                // 簡易的にテキスト検索でデータを抽出
                
                // 地域名を抽出
                int nameStart = json.IndexOf("\"name\":\"") + 8;
                int nameEnd = json.IndexOf("\"", nameStart);
                if (nameStart > 8 && nameEnd > nameStart)
                {
                    _currentWeather.areaName = json.Substring(nameStart, nameEnd - nameStart);
                }
                
                // 天気を抽出（最初のweathersを探す）
                int weathersStart = json.IndexOf("\"weathers\":[\"") + 13;
                int weathersEnd = json.IndexOf("\"", weathersStart);
                if (weathersStart > 13 && weathersEnd > weathersStart)
                {
                    _currentWeather.weather = json.Substring(weathersStart, weathersEnd - weathersStart);
                }
                
                // 降水確率を抽出
                int popsStart = json.IndexOf("\"pops\":[");
                if (popsStart > 0)
                {
                    int popsEnd = json.IndexOf("]", popsStart);
                    string popsSection = json.Substring(popsStart + 8, popsEnd - popsStart - 8);
                    string[] popValues = popsSection.Replace("\"", "").Split(',');
                    _currentWeather.pops = new List<string>(popValues);
                }
                
                // 気温を抽出
                int tempsStart = json.IndexOf("\"temps\":[");
                if (tempsStart > 0)
                {
                    int tempsEnd = json.IndexOf("]", tempsStart);
                    string tempsSection = json.Substring(tempsStart + 9, tempsEnd - tempsStart - 9);
                    string[] tempValues = tempsSection.Replace("\"", "").Split(',');
                    _currentWeather.temps = new List<string>(tempValues);
                }
                
                // 日付
                _currentWeather.date = DateTime.Now.ToString("M月d日 (ddd)");
                
                Debug.Log($"[Weather] Parsed: {_currentWeather.areaName}, {_currentWeather.weather}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Weather Parse Error: {e.Message}");
            }
        }
        
        void UpdateUI()
        {
            if (_currentWeather == null) return;
            
            // タイトル
            if (weatherTitleText != null)
            {
                weatherTitleText.text = $"🌤 {_currentWeather.areaName} の天気";
            }
            
            // 日付
            if (weatherDateText != null)
            {
                weatherDateText.text = _currentWeather.date;
            }
            
            // コンテンツ
            if (weatherContentText != null)
            {
                string content = "";
                
                // 天気
                string weatherEmoji = GetWeatherEmoji(_currentWeather.weather);
                content += $"<size=28>{weatherEmoji} {_currentWeather.weather}</size>\n\n";
                
                // 気温
                if (_currentWeather.temps != null && _currentWeather.temps.Count >= 2)
                {
                    string minTemp = _currentWeather.temps.Count > 0 ? _currentWeather.temps[0] : "-";
                    string maxTemp = _currentWeather.temps.Count > 1 ? _currentWeather.temps[1] : "-";
                    content += $"<color=#77AAFF>🌡 気温</color>\n";
                    content += $"  最低: <color=#88CCFF>{minTemp}°C</color>  最高: <color=#FFAA77>{maxTemp}°C</color>\n\n";
                }
                
                // 降水確率
                if (_currentWeather.pops != null && _currentWeather.pops.Count > 0)
                {
                    content += $"<color=#77AAFF>☔ 降水確率</color>\n";
                    string[] timeSlots = {"0-6時", "6-12時", "12-18時", "18-24時"};
                    for (int i = 0; i < Math.Min(_currentWeather.pops.Count, 4); i++)
                    {
                        string pop = _currentWeather.pops[i];
                        if (!string.IsNullOrEmpty(pop))
                        {
                            string color = int.TryParse(pop, out int popVal) && popVal >= 50 ? "#FFAA77" : "#88FFAA";
                            content += $"  {timeSlots[i]}: <color={color}>{pop}%</color>\n";
                        }
                    }
                }
                
                weatherContentText.text = content;
            }
            
            // トップバー表示
            UpdateTopBarWeather();
        }
        
        void UpdateTopBarWeather()
        {
            if (_currentWeather == null) return;
            
            string emoji = GetWeatherEmoji(_currentWeather.weather);
            int tempValue = 0;
            
            if (_currentWeather.temps != null && _currentWeather.temps.Count > 1)
            {
                int.TryParse(_currentWeather.temps[1], out tempValue);
            }
            
            // スプライト表示が設定されている場合
            if (temperatureSpriteDisplay != null)
            {
                temperatureSpriteDisplay.SetTemperature(tempValue);
                
                // 天気アイコン用のテキストは別に表示
                if (topBarWeatherText != null)
                {
                    topBarWeatherText.text = emoji;
                }
            }
            else if (topBarWeatherText != null)
            {
                // フォールバック: テキスト表示
                topBarWeatherText.text = $"{emoji} {tempValue}°";
            }
        }
        
        string GetWeatherEmoji(string weather)
        {
            if (string.IsNullOrEmpty(weather)) return "🌤";
            
            if (weather.Contains("晴")) return "☀️";
            if (weather.Contains("曇")) return "☁️";
            if (weather.Contains("雨")) return "🌧️";
            if (weather.Contains("雪")) return "❄️";
            if (weather.Contains("雷")) return "⛈️";
            
            return "🌤";
        }
        
        // 地域変更メソッド
        public void SetArea(string areaName)
        {
            if (AreaCodes.ContainsKey(areaName))
            {
                areaCode = AreaCodes[areaName];
                Debug.Log($"[Weather] Area changed to: {areaName} ({areaCode})");
                RefreshWeather();
            }
        }
        
        public void SetAreaByCode(string code)
        {
            areaCode = code;
            RefreshWeather();
        }
        
        public string GetCurrentAreaName()
        {
            foreach (var kvp in AreaCodes)
            {
                if (kvp.Value == areaCode)
                {
                    return kvp.Key;
                }
            }
            return "不明";
        }
        
        // 主要都市リスト（UI用）- AreaCodesのキーと一致させる
        public static readonly string[] MainCities = { "大阪", "東京", "愛知", "福岡", "北海道", "宮城", "広島", "京都", "兵庫", "沖縄" };
        
        private int _currentCityIndex = 0;
        
        public void CycleCity()
        {
            // MainCitiesを順番に切り替え
            string currentArea = GetCurrentAreaName();
            int currentIndex = System.Array.IndexOf(MainCities, currentArea);
            if (currentIndex < 0) currentIndex = 0;
            
            _currentCityIndex = (currentIndex + 1) % MainCities.Length;
            SetArea(MainCities[_currentCityIndex]);
        }
    }
    
    [Serializable]
    public class WeatherData
    {
        public string areaName;
        public string date;
        public string weather;
        public List<string> pops; // 降水確率
        public List<string> temps; // 気温
    }
}
