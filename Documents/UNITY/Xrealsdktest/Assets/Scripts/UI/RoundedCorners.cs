using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの角丸を実現するためのマスクコンポーネント
/// Image.Filledやプロシージャル生成でソフトな角丸効果を提供
/// </summary>
[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class RoundedCorners : MonoBehaviour
{
    [Header("Corner Radius")]
    [Range(0f, 50f)]
    public float radius = 12f;
    
    [Header("Settings")]
    public bool applyOnStart = true;
    
    private Image _image;
    private Sprite _roundedSprite;
    
    void Start()
    {
        if (applyOnStart)
        {
            ApplyRoundedCorners();
        }
    }
    
    void OnValidate()
    {
        ApplyRoundedCorners();
    }
    
    public void ApplyRoundedCorners()
    {
        _image = GetComponent<Image>();
        if (_image == null) return;
        
        // 角丸スプライトを生成
        _roundedSprite = CreateRoundedSprite((int)radius);
        
        if (_roundedSprite != null)
        {
            _image.sprite = _roundedSprite;
            _image.type = Image.Type.Sliced;
            _image.pixelsPerUnitMultiplier = 1f;
        }
    }
    
    /// <summary>
    /// プロシージャルに角丸スプライトを生成
    /// </summary>
    private Sprite CreateRoundedSprite(int cornerRadius)
    {
        int size = 64;
        int r = Mathf.Clamp(cornerRadius, 1, size / 2 - 1);
        
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = GetRoundedAlpha(x, y, size, size, r);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        // 9-slice用のボーダー設定
        Vector4 border = new Vector4(r + 1, r + 1, r + 1, r + 1);
        
        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border
        );
    }
    
    /// <summary>
    /// 角丸のアルファ値を計算
    /// </summary>
    private float GetRoundedAlpha(int x, int y, int width, int height, int radius)
    {
        // 各コーナーをチェック
        int[] cornerX = { radius, width - radius - 1, width - radius - 1, radius };
        int[] cornerY = { radius, radius, height - radius - 1, height - radius - 1 };
        
        for (int i = 0; i < 4; i++)
        {
            int cx = cornerX[i];
            int cy = cornerY[i];
            
            bool inCornerRegion = false;
            
            switch (i)
            {
                case 0: // 左下
                    inCornerRegion = (x < radius && y < radius);
                    break;
                case 1: // 右下
                    inCornerRegion = (x >= width - radius && y < radius);
                    break;
                case 2: // 右上
                    inCornerRegion = (x >= width - radius && y >= height - radius);
                    break;
                case 3: // 左上
                    inCornerRegion = (x < radius && y >= height - radius);
                    break;
            }
            
            if (inCornerRegion)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (dist > radius)
                    return 0f;
                else if (dist > radius - 1)
                    return 1f - (dist - (radius - 1)); // アンチエイリアス
                else
                    return 1f;
            }
        }
        
        return 1f;
    }
}
