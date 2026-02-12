using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace NRKernal
{
    /// <summary>
    /// ボタンのホバー拡大・クリック回転アニメーション
    /// </summary>
    public class ButtonScaleRotateAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Hover Scale Settings")]
        public float hoverScale = 1.2f;
        public float hoverDuration = 0.2f;
        
        [Header("Click Rotate Settings")]
        public float rotateAngle = 360f;
        public float rotateDuration = 0.5f;
        public bool rotateClockwise = true;
        
        [Header("Easing")]
        public Ease hoverEase = Ease.OutBack;
        public Ease rotateEase = Ease.OutQuad;
        
        private Vector3 _originalScale;
        private Tweener _scaleTween;
        private Tweener _rotateTween;
        
        void Awake()
        {
            _originalScale = transform.localScale;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 既存のTweenをキャンセル
            _scaleTween?.Kill();
            
            // 拡大アニメーション
            _scaleTween = transform.DOScale(_originalScale * hoverScale, hoverDuration)
                .SetEase(hoverEase);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            // 既存のTweenをキャンセル
            _scaleTween?.Kill();
            
            // 元のサイズに戻す
            _scaleTween = transform.DOScale(_originalScale, hoverDuration)
                .SetEase(Ease.OutQuad);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // 既存の回転Tweenをキャンセル
            _rotateTween?.Kill();
            
            // Z軸のみで回転（固定軸）
            float targetAngle = rotateClockwise ? -rotateAngle : rotateAngle;
            
            // 現在の回転を0にリセットしてから回転開始
            transform.localEulerAngles = new Vector3(0, 0, 0);
            
            _rotateTween = transform.DOLocalRotate(
                new Vector3(0, 0, targetAngle),
                rotateDuration,
                RotateMode.FastBeyond360
            ).SetEase(rotateEase)
            .OnComplete(() => {
                // 回転完了後、0度にリセット
                transform.localEulerAngles = Vector3.zero;
            });
        }
        
        void OnDisable()
        {
            _scaleTween?.Kill();
            _rotateTween?.Kill();
            transform.localScale = _originalScale;
        }
    }
}
