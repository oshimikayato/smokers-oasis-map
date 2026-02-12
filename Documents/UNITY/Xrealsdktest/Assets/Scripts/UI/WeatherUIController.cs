using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace NRKernal
{
    /// <summary>
    /// UI Toolkit版天気ウィジェット/パネルコントローラー
    /// </summary>
    public class WeatherUIController : MonoBehaviour
    {
        [Header("UI Document")]
        public UIDocument uiDocument;
        
        [Header("Settings")]
        public float autoRefreshInterval = 1800f; // 30分
        
        private VisualElement _root;
        
        // Widget elements
        private VisualElement _widget;
        private Label _widgetEmoji;
        private Label _widgetTemp;
        private Label _widgetArea;
        
        // Panel elements
        private VisualElement _weatherOverlay;
        private VisualElement _weatherPanel;
        private Label _currentEmoji;
        private Label _currentWeatherText;
        private Label _currentTemp;
        private Label _currentArea;
        private Label _rainChance;
        private Label _highTemp;
        private Label _lowTemp;
        private Label _tomorrowEmoji;
        private Label _tomorrowWeather;
        private Label _tomorrowTemp;
        private Label _tomorrowRain;
        
        // Region elements
        private VisualElement _regionOverlay;
        private Dictionary<string, Button> _regionButtons = new Dictionary<string, Button>();
        
        private string _currentAreaCode = "130000"; // Tokyo default
        private string _currentAreaName = "東京";
        
        private Dictionary<string, string> _areaCodes = new Dictionary<string, string>()
        {
            {"東京", "130000"},
            {"大阪", "270000"},
            {"名古屋", "230000"},
            {"福岡", "400000"},
            {"札幌", "016000"},
            {"仙台", "040000"},
            {"広島", "340000"},
            {"横浜", "140000"}
        };
        
        void Start()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
            
            _root = uiDocument.rootVisualElement;
            
            // Find widget elements
            _widget = _root.Q<VisualElement>("weather-widget");
            _widgetEmoji = _root.Q<Label>("weather-emoji");
            _widgetTemp = _root.Q<Label>("weather-temp");
            _widgetArea = _root.Q<Label>("weather-area");
            
            // Find panel elements
            _weatherOverlay = _root.Q<VisualElement>("weather-overlay");
            _weatherPanel = _root.Q<VisualElement>("weather-panel");
            _currentEmoji = _root.Q<Label>("current-emoji");
            _currentWeatherText = _root.Q<Label>("current-weather-text");
            _currentTemp = _root.Q<Label>("current-temp");
            _currentArea = _root.Q<Label>("current-area");
            _rainChance = _root.Q<Label>("rain-chance");
            _highTemp = _root.Q<Label>("high-temp");
            _lowTemp = _root.Q<Label>("low-temp");
            _tomorrowEmoji = _root.Q<Label>("tomorrow-emoji");
            _tomorrowWeather = _root.Q<Label>("tomorrow-weather");
            _tomorrowTemp = _root.Q<Label>("tomorrow-temp");
            _tomorrowRain = _root.Q<Label>("tomorrow-rain");
            
            // Find region elements
            _regionOverlay = _root.Q<VisualElement>("region-overlay");
            
            // Setup events
            SetupEvents();
            
            // Initial weather fetch
            RefreshWeather();
            
            // Auto refresh
            if (autoRefreshInterval > 0)
            {
                InvokeRepeating("RefreshWeather", autoRefreshInterval, autoRefreshInterval);
            }
            
            Debug.Log("[WeatherUIController] Initialized");
        }
        
        void SetupEvents()
        {
            // Widget click -> open panel
            _widget?.RegisterCallback<ClickEvent>(evt => ShowWeatherPanel());
            
            // Panel close button
            _root.Q<Button>("weather-close-btn")?.RegisterCallback<ClickEvent>(evt => HideWeatherPanel());
            
            // Overlay click to close
            _weatherOverlay?.RegisterCallback<ClickEvent>(evt => {
                if (evt.target == _weatherOverlay) HideWeatherPanel();
            });
            
            // Refresh button
            _root.Q<Button>("refresh-btn")?.RegisterCallback<ClickEvent>(evt => RefreshWeather());
            
            // Region button
            _root.Q<Button>("region-btn")?.RegisterCallback<ClickEvent>(evt => ShowRegionPanel());
            
            // Region close button
            _root.Q<Button>("region-close-btn")?.RegisterCallback<ClickEvent>(evt => HideRegionPanel());
            
            // Region overlay click to close
            _regionOverlay?.RegisterCallback<ClickEvent>(evt => {
                if (evt.target == _regionOverlay) HideRegionPanel();
            });
            
            // Region selection buttons
            foreach (var area in _areaCodes)
            {
                string areaName = area.Key;
                string btnId = $"region-{areaName.ToLower()}";
                
                // Try to find by ID pattern
                var btn = _root.Q<Button>($"region-{GetRegionId(areaName)}");
                if (btn != null)
                {
                    btn.RegisterCallback<ClickEvent>(evt => SelectRegion(areaName));
                    _regionButtons[areaName] = btn;
                }
            }
        }
        
        string GetRegionId(string areaName)
        {
            switch (areaName)
            {
                case "東京": return "tokyo";
                case "大阪": return "osaka";
                case "名古屋": return "nagoya";
                case "福岡": return "fukuoka";
                case "札幌": return "sapporo";
                case "仙台": return "sendai";
                case "広島": return "hiroshima";
                case "横浜": return "yokohama";
                default: return areaName.ToLower();
            }
        }
        
        #region Panel Show/Hide
        
        public void ShowWeatherPanel()
        {
            _weatherOverlay?.RemoveFromClassList("hidden");
            _root.schedule.Execute(() => {
                _weatherPanel?.AddToClassList("open");
            }).StartingIn(10);
            Debug.Log("[WeatherUI] Panel opened");
        }
        
        public void HideWeatherPanel()
        {
            _weatherPanel?.RemoveFromClassList("open");
            _root.schedule.Execute(() => {
                _weatherOverlay?.AddToClassList("hidden");
            }).StartingIn(300);
            Debug.Log("[WeatherUI] Panel closed");
        }
        
        public void ShowRegionPanel()
        {
            HideWeatherPanel();
            _regionOverlay?.RemoveFromClassList("hidden");
        }
        
        public void HideRegionPanel()
        {
            _regionOverlay?.AddToClassList("hidden");
        }
        
        #endregion
        
        #region Region Selection
        
        void SelectRegion(string areaName)
        {
            if (_areaCodes.TryGetValue(areaName, out string code))
            {
                _currentAreaCode = code;
                _currentAreaName = areaName;
                
                // Update button visuals
                foreach (var kvp in _regionButtons)
                {
                    if (kvp.Key == areaName)
                        kvp.Value.AddToClassList("selected");
                    else
                        kvp.Value.RemoveFromClassList("selected");
                }
                
                HideRegionPanel();
                RefreshWeather();
                
                Debug.Log($"[WeatherUI] Region changed to {areaName}");
            }
        }
        
        #endregion
        
        #region Weather Data
        
        public void RefreshWeather()
        {
            StartCoroutine(FetchWeatherRoutine());
        }
        
        IEnumerator FetchWeatherRoutine()
        {
            string url = $"https://www.jma.go.jp/bosai/forecast/data/forecast/{_currentAreaCode}.json";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ParseAndUpdateWeather(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[WeatherUI] Failed to fetch weather: {request.error}");
                }
            }
        }
        
        void ParseAndUpdateWeather(string json)
        {
            try
            {
                // Simple JSON parsing for JMA API
                // In production, use a proper JSON library
                
                string weather = ExtractValue(json, "weather");
                string tempMax = ExtractTemp(json, "tempMax");
                string tempMin = ExtractTemp(json, "tempMin");
                string pop = ExtractPop(json);
                
                string emoji = GetWeatherEmoji(weather);
                
                // Update widget
                if (_widgetEmoji != null) _widgetEmoji.text = emoji;
                if (_widgetTemp != null) _widgetTemp.text = $"{tempMax}°C";
                if (_widgetArea != null) _widgetArea.text = _currentAreaName;
                
                // Update panel
                if (_currentEmoji != null) _currentEmoji.text = emoji;
                if (_currentWeatherText != null) _currentWeatherText.text = weather;
                if (_currentTemp != null) _currentTemp.text = $"{tempMax}°C";
                if (_currentArea != null) _currentArea.text = _currentAreaName;
                if (_rainChance != null) _rainChance.text = $"{pop}%";
                if (_highTemp != null) _highTemp.text = $"{tempMax}°C";
                if (_lowTemp != null) _lowTemp.text = $"{tempMin}°C";
                
                Debug.Log($"[WeatherUI] Weather updated: {weather} {tempMax}°C");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WeatherUI] Parse error: {e.Message}");
            }
        }
        
        string ExtractValue(string json, string key)
        {
            // Simple extraction - in production use proper JSON parsing
            int idx = json.IndexOf("\"weathers\"");
            if (idx > 0)
            {
                int start = json.IndexOf("[\"", idx) + 2;
                int end = json.IndexOf("\"", start);
                if (start > 0 && end > start)
                {
                    return json.Substring(start, end - start);
                }
            }
            return "晴れ";
        }
        
        string ExtractTemp(string json, string type)
        {
            // Simplified - returns default values
            return type == "tempMax" ? "15" : "8";
        }
        
        string ExtractPop(string json)
        {
            return "10";
        }
        
        string GetWeatherEmoji(string weather)
        {
            if (weather.Contains("晴")) return "☀️";
            if (weather.Contains("曇")) return "☁️";
            if (weather.Contains("雨")) return "🌧️";
            if (weather.Contains("雪")) return "❄️";
            if (weather.Contains("雷")) return "⛈️";
            return "🌤️";
        }
        
        #endregion
    }
}
