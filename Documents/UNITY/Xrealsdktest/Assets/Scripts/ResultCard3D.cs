using UnityEngine;
using System.Collections;

/// <summary>
/// 3Dカードコンポーネント
/// 個々のカードの表示とインタラクションを管理
/// </summary>
public class ResultCard3D : MonoBehaviour
{
    public MeshRenderer imageRenderer;
    public MeshRenderer glowRenderer;

    [HideInInspector] public SearchResultItem itemData;
    [HideInInspector] public int cardIndex;

    private bool _isSelected = false;
    private FloatingCardDisplay _parentDisplay;

    /// <summary>
    /// カードを初期化
    /// </summary>
    public void Setup(SearchResultItem item, int index, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        itemData = item;
        cardIndex = index;
        _parentDisplay = GetComponentInParent<FloatingCardDisplay>();

        // ログ出力用 - 直接ImageUploaderを取得
        var uploader = FindObjectOfType<ImageUploader>();
        if (uploader != null) 
        {
            uploader.Log($"[Card{index}] Setup called");
            uploader.Log($"[Card{index}] parentDisplay={((_parentDisplay != null) ? "OK" : "NULL")}");
            uploader.Log($"[Card{index}] onImageLoaded={(onImageLoaded != null ? "OK" : "NULL")}");
        }

        // 画像ダウンロード開始
        if (onImageLoaded != null)
        {
            if (uploader != null) uploader.Log($"[Card{index}] Invoking callback NOW");
            onImageLoaded.Invoke(null, item); // ダウンロードリクエスト
            if (uploader != null) uploader.Log($"[Card{index}] Callback invoked");
        }
        else
        {
            if (uploader != null) uploader.Log($"[Card{index}] ERROR: onImageLoaded is NULL");
        }
    }

    /// <summary>
    /// ダウンロードされた画像を適用
    /// </summary>
    public void ApplyTexture(Texture2D texture)
    {
        if (texture != null && imageRenderer != null)
        {
            imageRenderer.material.mainTexture = texture;
            imageRenderer.material.color = Color.white;

            // アスペクト比を調整
            float aspect = (float)texture.width / texture.height;
            Vector3 scale = transform.localScale;
            if (aspect > 1f)
            {
                // 横長
                imageRenderer.transform.localScale = new Vector3(1f, 1f / aspect, 1f);
            }
            else
            {
                // 縦長
                imageRenderer.transform.localScale = new Vector3(aspect, 1f, 1f);
            }

            // グローも同じアスペクト比に
            if (glowRenderer != null)
            {
                glowRenderer.transform.localScale = imageRenderer.transform.localScale * 1.1f;
            }
        }
    }

    /// <summary>
    /// 選択状態を設定
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        if (glowRenderer != null)
        {
            Color glowColor = glowRenderer.material.color;
            glowColor.a = selected ? 1f : 0.5f;
            glowRenderer.material.color = glowColor;
        }
    }

    void OnMouseDown()
    {
        // クリック/タップで選択
        if (_parentDisplay != null)
        {
            _parentDisplay.SelectCard(cardIndex);
        }
    }
}
