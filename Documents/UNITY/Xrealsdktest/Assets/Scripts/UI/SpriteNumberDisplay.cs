using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NRKernal
{
    /// <summary>
    /// 数字画像で数値を表示するコンポーネント
    /// </summary>
    public class SpriteNumberDisplay : MonoBehaviour
    {
        [Header("Settings")]
        public string numberSpritePath = "Assets/UI/Number/white/digit-";
        public float digitWidth = 30f;
        public float digitSpacing = 2f;
        public bool centerAlign = true;
        
        [Header("Optional")]
        public Sprite minusSign;
        public Sprite degreeSign;
        public Sprite colonSign;
        public Sprite celsiusSign; // ℃
        
        [Header("Digit Sprites (Auto-loaded or Manual)")]
        public Sprite[] digitSprites = new Sprite[10]; // 0-9
        
        private List<Image> _digitImages = new List<Image>();
        private Dictionary<char, Sprite> _digitSpriteMap = new Dictionary<char, Sprite>();
        private HorizontalLayoutGroup _layoutGroup;
        
        void Awake()
        {
            BuildSpriteMap();
            SetupLayout();
        }
        
        void BuildSpriteMap()
        {
            // Build map from digit sprites array
            for (int i = 0; i <= 9 && i < digitSprites.Length; i++)
            {
                if (digitSprites[i] != null)
                {
                    _digitSpriteMap[i.ToString()[0]] = digitSprites[i];
                }
            }
        }
        
        void SetupLayout()
        {
            _layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (_layoutGroup == null)
            {
                _layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            _layoutGroup.spacing = digitSpacing;
            _layoutGroup.childAlignment = centerAlign ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            _layoutGroup.childControlWidth = false;
            _layoutGroup.childControlHeight = false;
            _layoutGroup.childForceExpandWidth = false;
            _layoutGroup.childForceExpandHeight = false;
        }
        
        /// <summary>
        /// 整数を表示
        /// </summary>
        public void SetNumber(int number)
        {
            SetText(number.ToString());
        }
        
        /// <summary>
        /// 時刻を表示 (HH:MM形式)
        /// </summary>
        public void SetTime(int hours, int minutes)
        {
            string timeStr = $"{hours:D2}:{minutes:D2}";
            SetText(timeStr);
        }
        
        /// <summary>
        /// 気温を表示 (℃付き)
        /// </summary>
        public void SetTemperature(int temp)
        {
            string tempStr = temp.ToString() + "℃";
            SetText(tempStr);
        }
        
        /// <summary>
        /// 文字列を数字画像で表示
        /// </summary>
        public void SetText(string text)
        {
            // Clear existing digits
            ClearDigits();
            
            // Create digit images
            foreach (char c in text)
            {
                CreateDigitImage(c);
            }
        }
        
        void CreateDigitImage(char c)
        {
            GameObject digitObj = new GameObject($"Digit_{c}");
            digitObj.transform.SetParent(transform, false);
            
            Image img = digitObj.AddComponent<Image>();
            RectTransform rect = digitObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(digitWidth, digitWidth * 1.5f);
            
            if (_digitSpriteMap.TryGetValue(c, out Sprite sprite))
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            else if (c == '-' && minusSign != null)
            {
                img.sprite = minusSign;
                img.preserveAspect = true;
            }
            else if (c == '°' && degreeSign != null)
            {
                img.sprite = degreeSign;
                img.preserveAspect = true;
                rect.sizeDelta = new Vector2(digitWidth * 0.5f, digitWidth * 0.75f);
            }
            else if (c == ':' && colonSign != null)
            {
                img.sprite = colonSign;
                img.preserveAspect = true;
                rect.sizeDelta = new Vector2(digitWidth * 0.4f, digitWidth * 1.2f);
            }
            else if ((c == '℃' || c == 'C') && celsiusSign != null)
            {
                img.sprite = celsiusSign;
                img.preserveAspect = true;
                rect.sizeDelta = new Vector2(digitWidth * 1.2f, digitWidth * 1.5f);
            }
            else if (c == ':')
            {
                // Fallback: create colon using text
                Text colonText = digitObj.AddComponent<Text>();
                colonText.text = ":";
                colonText.fontSize = (int)(digitWidth * 1.5f);
                colonText.color = Color.white;
                colonText.alignment = TextAnchor.MiddleCenter;
                colonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Destroy(img);
                rect.sizeDelta = new Vector2(digitWidth * 0.4f, digitWidth * 1.5f);
            }
            else if (c == '°')
            {
                // Fallback: create degree using text
                Text degText = digitObj.AddComponent<Text>();
                degText.text = "°";
                degText.fontSize = (int)(digitWidth);
                degText.color = Color.white;
                degText.alignment = TextAnchor.UpperCenter;
                degText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Destroy(img);
                rect.sizeDelta = new Vector2(digitWidth * 0.5f, digitWidth * 1.5f);
            }
            else
            {
                // Unknown character - use text fallback
                Text fallbackText = digitObj.AddComponent<Text>();
                fallbackText.text = c.ToString();
                fallbackText.fontSize = (int)(digitWidth * 1.2f);
                fallbackText.color = Color.white;
                fallbackText.alignment = TextAnchor.MiddleCenter;
                fallbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Destroy(img);
            }
            
            _digitImages.Add(img);
        }
        
        void ClearDigits()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            _digitImages.Clear();
        }
    }
}
