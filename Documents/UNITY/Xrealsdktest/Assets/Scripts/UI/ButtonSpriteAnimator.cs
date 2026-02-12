using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace NRKernal
{
    /// <summary>
    /// ボタンホバー時にスプライトアニメーションを再生するコンポーネント
    /// </summary>
    public class ButtonSpriteAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Frames")]
        [Tooltip("連番スプライト（フレーム順に設定）")]
        public Sprite[] frames;
        
        [Header("Settings")]
        [Tooltip("フレームレート（FPS）")]
        public float frameRate = 12f;
        
        [Tooltip("ループ再生するか")]
        public bool loop = false;
        
        [Tooltip("ホバー時に再生")]
        public bool playOnHover = true;
        
        [Tooltip("逆再生でアニメーション終了")]
        public bool reverseOnExit = true;
        
        private Image _image;
        private Sprite _defaultSprite;
        private Coroutine _animationCoroutine;
        private int _currentFrame = 0;
        private bool _isHovering = false;
        
        void Awake()
        {
            _image = GetComponent<Image>();
            if (_image != null && frames != null && frames.Length > 0)
            {
                _defaultSprite = frames[0];
            }
        }
        
        void Start()
        {
            // 初期フレームを設定
            if (_image != null && _defaultSprite != null)
            {
                _image.sprite = _defaultSprite;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!playOnHover) return;
            
            _isHovering = true;
            PlayAnimation(false);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!playOnHover) return;
            
            _isHovering = false;
            
            if (reverseOnExit)
            {
                PlayAnimation(true);
            }
            else
            {
                StopAnimation();
                ResetToDefault();
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // クリック時に最終フレームを表示
            if (frames != null && frames.Length > 0 && _image != null)
            {
                _image.sprite = frames[frames.Length - 1];
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            // クリック解除時
            if (_isHovering && frames != null && frames.Length > 0)
            {
                _image.sprite = frames[frames.Length - 1];
            }
        }
        
        /// <summary>
        /// アニメーション再生
        /// </summary>
        /// <param name="reverse">逆再生</param>
        public void PlayAnimation(bool reverse = false)
        {
            StopAnimation();
            _animationCoroutine = StartCoroutine(AnimationCoroutine(reverse));
        }
        
        /// <summary>
        /// アニメーション停止
        /// </summary>
        public void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }
        
        /// <summary>
        /// デフォルトスプライトにリセット
        /// </summary>
        public void ResetToDefault()
        {
            _currentFrame = 0;
            if (_image != null && _defaultSprite != null)
            {
                _image.sprite = _defaultSprite;
            }
        }
        
        IEnumerator AnimationCoroutine(bool reverse)
        {
            if (frames == null || frames.Length == 0 || _image == null) yield break;
            
            float frameDuration = 1f / frameRate;
            
            if (reverse)
            {
                // 逆再生
                for (int i = frames.Length - 1; i >= 0; i--)
                {
                    _image.sprite = frames[i];
                    _currentFrame = i;
                    yield return new WaitForSeconds(frameDuration);
                }
            }
            else
            {
                // 順再生
                do
                {
                    for (int i = 0; i < frames.Length; i++)
                    {
                        _image.sprite = frames[i];
                        _currentFrame = i;
                        yield return new WaitForSeconds(frameDuration);
                        
                        // ホバー解除されたら停止（ループ中）
                        if (!_isHovering && !loop) break;
                    }
                } while (loop && _isHovering);
                
                // ホバー中は最終フレームを維持
                if (_isHovering && !loop)
                {
                    _image.sprite = frames[frames.Length - 1];
                }
            }
        }
        
        /// <summary>
        /// 指定フォルダからフレームを自動読み込み
        /// </summary>
        public void LoadFramesFromFolder(string folderPath)
        {
            frames = Resources.LoadAll<Sprite>(folderPath);
            if (frames != null && frames.Length > 0)
            {
                _defaultSprite = frames[0];
                Debug.Log($"[ButtonSpriteAnimator] Loaded {frames.Length} frames from {folderPath}");
            }
        }
    }
}
