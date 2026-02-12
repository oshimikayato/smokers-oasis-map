using UnityEngine;
using UnityEngine.UI;

namespace NRKernal
{
    /// <summary>
    /// 背景スプライトのループアニメーション
    /// </summary>
    public class BackgroundSpriteAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public Sprite[] frames;
        public float frameRate = 10f;
        public bool playOnStart = true;
        public bool loop = true;
        
        private Image _image;
        private int _currentFrame = 0;
        private float _timer = 0f;
        private bool _isPlaying = false;
        
        void Awake()
        {
            _image = GetComponent<Image>();
        }
        
        void Start()
        {
            if (playOnStart && frames != null && frames.Length > 0)
            {
                Play();
            }
        }
        
        void Update()
        {
            if (!_isPlaying || frames == null || frames.Length == 0) return;
            
            _timer += Time.deltaTime;
            float frameInterval = 1f / frameRate;
            
            if (_timer >= frameInterval)
            {
                _timer -= frameInterval;
                _currentFrame++;
                
                if (_currentFrame >= frames.Length)
                {
                    if (loop)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = frames.Length - 1;
                        _isPlaying = false;
                        return;
                    }
                }
                
                if (_image != null && frames[_currentFrame] != null)
                {
                    _image.sprite = frames[_currentFrame];
                }
            }
        }
        
        public void Play()
        {
            if (frames == null || frames.Length == 0) return;
            
            _isPlaying = true;
            _currentFrame = 0;
            _timer = 0f;
            
            if (_image != null && frames[0] != null)
            {
                _image.sprite = frames[0];
            }
        }
        
        public void Stop()
        {
            _isPlaying = false;
        }
        
        public void Pause()
        {
            _isPlaying = false;
        }
        
        public void Resume()
        {
            _isPlaying = true;
        }
    }
}
