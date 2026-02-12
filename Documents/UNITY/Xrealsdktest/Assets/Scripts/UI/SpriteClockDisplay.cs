using UnityEngine;
using UnityEngine.UI;
using System;

namespace NRKernal
{
    /// <summary>
    /// スプライト数字を使った時計表示
    /// </summary>
    public class SpriteClockDisplay : MonoBehaviour
    {
        [Header("Display Settings")]
        public SpriteNumberDisplay hourDisplay;
        public SpriteNumberDisplay minuteDisplay;
        public Text colonText; // コロン用テキスト
        public GameObject colonObject; // コロン用オブジェクト（点滅用）
        
        [Header("Blink Settings")]
        public bool blinkColon = true;
        public float blinkInterval = 0.5f;
        
        [Header("Update Settings")]
        public float updateInterval = 1f; // 更新間隔（秒）
        
        private float _lastUpdateTime;
        private float _lastBlinkTime;
        private bool _colonVisible = true;
        
        void Start()
        {
            UpdateClock();
        }
        
        void Update()
        {
            // 時計更新
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                UpdateClock();
                _lastUpdateTime = Time.time;
            }
            
            // コロン点滅
            if (blinkColon && colonObject != null)
            {
                if (Time.time - _lastBlinkTime >= blinkInterval)
                {
                    _colonVisible = !_colonVisible;
                    colonObject.SetActive(_colonVisible);
                    _lastBlinkTime = Time.time;
                }
            }
        }
        
        void UpdateClock()
        {
            DateTime now = DateTime.Now;
            
            if (hourDisplay != null)
            {
                // 2桁表示（01, 02, ... 23）
                hourDisplay.SetText(now.Hour.ToString("D2"));
            }
            
            if (minuteDisplay != null)
            {
                // 2桁表示（00, 01, ... 59）
                minuteDisplay.SetText(now.Minute.ToString("D2"));
            }
        }
        
        /// <summary>
        /// 手動で時刻を設定
        /// </summary>
        public void SetTime(int hours, int minutes)
        {
            if (hourDisplay != null)
            {
                hourDisplay.SetText(hours.ToString("D2"));
            }
            
            if (minuteDisplay != null)
            {
                minuteDisplay.SetText(minutes.ToString("D2"));
            }
        }
    }
}
