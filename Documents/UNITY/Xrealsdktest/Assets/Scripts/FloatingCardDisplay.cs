using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Option A: フローティングカード表示
/// 検索結果を弧状に浮かぶ3Dカードとして表示
/// </summary>
public class FloatingCardDisplay : MonoBehaviour
{
    [Header("References")]
    public ARSearchResultDisplay baseDisplay;
    public GameObject cardPrefab; // 3Dカードプレハブ
    public Transform cardContainer; // カード配置用の親オブジェクト
    public ImageUploader imageUploader; // For logging to app panel

    [Header("Layout Settings")]
    public float arcRadius = 2.5f; // 弧の半径（カメラからの距離）
    public float arcAngle = 120f; // 弧の角度（度）
    public float cardSpacing = 0.4f; // カード間の間隔
    public float cardScale = 0.3f; // カードのスケール
    public int maxVisibleCards = 7; // 一度に表示する最大カード数

    [Header("Animation Settings")]
    public float fadeInDuration = 0.4f;
    public float fadeInDelay = 0.1f; // カード間の遅延
    public float hoverScale = 1.3f; // 選択時の拡大率

    [Header("Visual Settings")]
    public Color glowColor = new Color(0.3f, 0.7f, 1f, 0.8f);
    public float glowIntensity = 0.5f;

    private List<GameObject> _cards = new List<GameObject>();
    private int _selectedIndex = -1;
    private Camera _arCamera;

    void Start()
    {
        _arCamera = baseDisplay?.arCamera ?? Camera.main;
    }

    void Log(string msg)
    {
        if (imageUploader != null) imageUploader.Log($"[FloatingCard] {msg}");
        else Debug.Log($"[FloatingCard] {msg}");
    }

    /// <summary>
    /// 検索結果をフローティングカードとして表示
    /// </summary>
    public void DisplayResults(List<SearchResultItem> results, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        Log($"DisplayResults called with {results?.Count ?? 0} results");
        ClearCards();

        if (results == null || results.Count == 0)
        {
            Log("No results to display");
            return;
        }

        int cardCount = Mathf.Min(results.Count, maxVisibleCards);
        Log($"Will display {cardCount} cards (max={maxVisibleCards})");
        
        StartCoroutine(SpawnCardsWithAnimation(results, cardCount, onImageLoaded));
        Log("SpawnCardsWithAnimation coroutine started");
    }

    IEnumerator SpawnCardsWithAnimation(List<SearchResultItem> results, int cardCount, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        // カメラの取得
        if (_arCamera == null) _arCamera = Camera.main;
        if (_arCamera == null)
        {
            Log("ERROR: Camera not found!");
            yield break;
        }

        // コンテナの確認
        if (cardContainer == null)
        {
            Log("cardContainer is null, using this.transform");
            cardContainer = this.transform;
        }

        Log($"Spawning {cardCount} cards, camera at: {_arCamera.transform.position}");

        Vector3 cameraPos = _arCamera.transform.position;
        Vector3 cameraForward = _arCamera.transform.forward;
        Vector3 cameraRight = _arCamera.transform.right;

        // 弧の中心位置を計算
        Vector3 arcCenter = cameraPos + cameraForward * arcRadius;

        for (int i = 0; i < cardCount; i++)
        {
            // 弧状に配置する角度を計算
            float totalAngle = Mathf.Min(arcAngle, cardCount * 20f); // カード数に応じて角度調整
            float startAngle = -totalAngle / 2f;
            float angleStep = cardCount > 1 ? totalAngle / (cardCount - 1) : 0;
            float currentAngle = startAngle + angleStep * i;

            // 位置を計算（Y軸回転で弧を描く）
            float radians = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = cameraRight * Mathf.Sin(radians) * arcRadius * 0.5f;
            offset += cameraForward * (Mathf.Cos(radians) - 1) * arcRadius * 0.3f;
            
            Vector3 cardPosition = arcCenter + offset;
            cardPosition.y = cameraPos.y; // カメラと同じ高さに配置

            Log($"Creating card {i} at: {cardPosition}");

            // カードを生成
            GameObject card = CreateCard(cardPosition, results[i], i, onImageLoaded);
            if (card != null)
            {
                _cards.Add(card);
                Log($"Card {i} created OK");
                
                // フェードインアニメーション
                StartCoroutine(AnimateCardIn(card, fadeInDuration));
            }
            else
            {
                Log($"ERROR: Failed to create card {i}");
            }

            yield return new WaitForSeconds(fadeInDelay);
        }
        
        Log($"Finished! Total cards: {_cards.Count}");
    }

    GameObject CreateCard(Vector3 position, SearchResultItem item, int index, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        if (cardPrefab == null)
        {
            // プレハブがない場合は動的に作成
            return CreateDynamicCard(position, item, index, onImageLoaded);
        }

        GameObject card = Instantiate(cardPrefab, position, Quaternion.identity, cardContainer);
        card.transform.localScale = Vector3.one * cardScale;
        
        // カメラに向ける
        if (_arCamera != null)
        {
            Vector3 lookDir = _arCamera.transform.position - position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                card.transform.rotation = Quaternion.LookRotation(-lookDir);
            }
        }

        // カード情報を設定
        var cardComponent = card.GetComponent<ResultCard3D>();
        if (cardComponent != null)
        {
            cardComponent.Setup(item, index, onImageLoaded);
        }

        return card;
    }

    /// <summary>
    /// プレハブなしで動的にカードを作成
    /// </summary>
    GameObject CreateDynamicCard(Vector3 position, SearchResultItem item, int index, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        Log($"CreateDynamicCard START for card {index}");
        
        // ルートGameObject
        GameObject card = new GameObject($"Card_{index}");
        card.transform.position = position;
        card.transform.SetParent(cardContainer);
        card.transform.localScale = Vector3.one * cardScale;
        Log($"Card {index} root created");

        // カメラに向ける
        if (_arCamera != null)
        {
            Vector3 lookDir = _arCamera.transform.position - position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                card.transform.rotation = Quaternion.LookRotation(-lookDir);
            }
        }
        Log($"Card {index} camera facing done");

        // 背景用のQuad（グローエフェクト）
        GameObject glowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glowQuad.name = "GlowBackground";
        glowQuad.transform.SetParent(card.transform);
        glowQuad.transform.localPosition = new Vector3(0, 0, 0.01f);
        glowQuad.transform.localRotation = Quaternion.identity;
        glowQuad.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        Log($"Card {index} glow quad created");
        
        // グローマテリアル
        var glowRenderer = glowQuad.GetComponent<MeshRenderer>();
        var glowShader = Shader.Find("Unlit/Color");
        Log($"Card {index} glow shader: {(glowShader != null ? "OK" : "NULL")}");
        var glowMaterial = new Material(glowShader != null ? glowShader : Shader.Find("UI/Default"));
        glowMaterial.color = glowColor;
        glowRenderer.material = glowMaterial;
        Log($"Card {index} glow material applied");
        
        // Colliderを削除（不要）
        var glowCollider = glowQuad.GetComponent<Collider>();
        if (glowCollider != null) Destroy(glowCollider);

        // メイン画像用のQuad
        GameObject imageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imageQuad.name = "ImageQuad";
        imageQuad.transform.SetParent(card.transform);
        imageQuad.transform.localPosition = Vector3.zero;
        imageQuad.transform.localRotation = Quaternion.identity;
        imageQuad.transform.localScale = Vector3.one;
        Log($"Card {index} image quad created");

        // 画像マテリアル（初期はグレー）
        var imageRenderer = imageQuad.GetComponent<MeshRenderer>();
        var imageShader = Shader.Find("Unlit/Texture");
        Log($"Card {index} image shader: {(imageShader != null ? "OK" : "NULL")}");
        var imageMaterial = new Material(imageShader != null ? imageShader : Shader.Find("UI/Default"));
        imageMaterial.color = Color.gray;
        imageRenderer.material = imageMaterial;
        Log($"Card {index} image material applied");

        // Colliderを削除
        var imageCollider = imageQuad.GetComponent<Collider>();
        if (imageCollider != null) Destroy(imageCollider);

        // BoxColliderを追加（選択用）
        BoxCollider boxCollider = card.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(1f, 1f, 0.1f);
        Log($"Card {index} collider added");

        // ResultCard3Dコンポーネントを追加
        Log($"Adding ResultCard3D to card {index}");
        ResultCard3D cardComponent = card.AddComponent<ResultCard3D>();
        cardComponent.imageRenderer = imageRenderer;
        cardComponent.glowRenderer = glowRenderer;
        Log($"Calling Setup on card {index}, onImageLoaded={(onImageLoaded != null ? "OK" : "NULL")}");
        cardComponent.Setup(item, index, onImageLoaded);
        Log($"Setup completed for card {index}");

        // 初期状態は透明
        SetCardAlpha(card, 0f);

        return card;
    }

    IEnumerator AnimateCardIn(GameObject card, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = card.transform.localScale * 0.5f;
        Vector3 targetScale = card.transform.localScale;

        card.transform.localScale = startScale;
        SetCardAlpha(card, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic

            card.transform.localScale = Vector3.Lerp(startScale, targetScale, easeT);
            SetCardAlpha(card, easeT);

            yield return null;
        }

        card.transform.localScale = targetScale;
        SetCardAlpha(card, 1f);
    }

    void SetCardAlpha(GameObject card, float alpha)
    {
        var renderers = card.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// カードを選択（拡大表示）
    /// </summary>
    public void SelectCard(int index)
    {
        if (index < 0 || index >= _cards.Count) return;

        // 以前の選択を解除
        if (_selectedIndex >= 0 && _selectedIndex < _cards.Count)
        {
            StartCoroutine(AnimateCardScale(_cards[_selectedIndex], cardScale));
        }

        _selectedIndex = index;
        StartCoroutine(AnimateCardScale(_cards[index], cardScale * hoverScale));
    }

    /// <summary>
    /// DOTweenを使用したカードスケールアニメーション
    /// </summary>
    IEnumerator AnimateCardScale(GameObject card, float targetScale)
    {
        // DOTweenでバウンス効果付きスケールアニメーション
        card.transform.DOScale(Vector3.one * targetScale, 0.2f)
            .SetEase(Ease.OutBack);
        
        yield return new WaitForSeconds(0.2f);
    }

    /// <summary>
    /// すべてのカードをクリア
    /// </summary>
    public void ClearCards()
    {
        foreach (var card in _cards)
        {
            if (card != null) Destroy(card);
        }
        _cards.Clear();
        _selectedIndex = -1;
    }
}
