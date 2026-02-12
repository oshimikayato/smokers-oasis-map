using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NRKernal;
using DG.Tweening;

/// <summary>
/// Option C: ギャラリー回廊表示
/// 検索結果を両側の壁に2枚ずつ配置し、記憶の回廊として表現
/// ユーザーが歩いて進むように画像を閲覧できる
/// 平面検出を使用して床面に正確に配置
/// </summary>
public class TimelineCorridorDisplay : MonoBehaviour
{
    [Header("References")]
    public ARSearchResultDisplay baseDisplay;
    public Transform corridorContainer;
    public ImageUploader imageUploader;

    [Header("Layout Settings")]
    public int cardsPerSection = 4; // 1セクションあたりのカード数（左2枚+右2枚）
    public float sectionSpacing = 0.8f; // セクション間の奥行き距離（縮小）
    public float sideOffset = 0.7f; // 左右の壁からの距離
    public float verticalSpacing = 0.35f; // 同じ側の上下間隔
    public float startDistance = 0f; // 最初のセクション距離（0mスタート）
    public float cardWidth = 0.35f;
    public float cardHeight = 0.45f;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.6f;
    public float fadeInDelay = 0.1f;
    public float selectedScale = 1.3f;

    [Header("Visual Settings")]
    public Color glowColor = new Color(0.3f, 0.6f, 1f, 0.5f);
    public Color floorLineColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);

    [Header("Plane Detection")]
    public bool usePlaneDetection = true;
    public float planeSearchTimeout = 3.0f; // 平面検出のタイムアウト
    public int cardsPerRow = 2; // 1行あたりのカード数（片面配置）

    [Header("Performance Optimization")]
    public int maxVisibleCards = 50; // 同時に表示する最大カード数（増加）
    public float cardLoadDistance = 20f; // カードをロードする距離
    public float cardUnloadDistance = 50f; // カードをアンロードする距離（増加）
    public float updateInterval = 0.5f; // 更新間隔（秒）

    private List<GameObject> _cards = new List<GameObject>();
    private List<SearchResultItem> _allResults = new List<SearchResultItem>();
    private int _selectedIndex = -1;
    private Camera _arCamera;
    
    // 平面検出用
    private float _detectedFloorHeight = 0f;
    private bool _floorDetected = false;
    private bool _wallDetected = false;
    private Pose _wallPose;
    private List<NRTrackablePlane> _detectedPlanes = new List<NRTrackablePlane>();
    
    // カード詳細ビュー用
    private bool _isDetailViewOpen = false;
    private GameObject _detailPanel;
    private int _detailCardIndex = -1;
    
    // パフォーマンス最適化用
    // private float _lastUpdateTime = 0f;
    // private int _currentVisibleStart = 0;

    void Start()
    {
        _arCamera = baseDisplay?.arCamera ?? Camera.main;
    }

    void Update()
    {
        // 平面検出の更新
        if (usePlaneDetection && (!_floorDetected || !_wallDetected))
        {
            UpdatePlaneDetection();
        }
        
        // パフォーマンス最適化は無効化（すべてのカードを常に表示）
        // if (Time.time - _lastUpdateTime > updateInterval)
        // {
        //     _lastUpdateTime = Time.time;
        //     UpdateCardVisibility();
        // }
    }
    
    /// <summary>
    /// カメラ距離に応じてカードの表示/非表示を更新
    /// </summary>
    void UpdateCardVisibility()
    {
        if (_arCamera == null || _cards.Count == 0) return;
        
        Vector3 camPos = _arCamera.transform.position;
        int visibleCount = 0;
        
        for (int i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            if (card == null) continue;
            
            float distance = Vector3.Distance(camPos, card.transform.position);
            
            // 距離に応じて表示/非表示
            bool shouldBeVisible = distance < cardUnloadDistance && visibleCount < maxVisibleCards;
            
            if (shouldBeVisible && !card.activeSelf)
            {
                card.SetActive(true);
            }
            else if (!shouldBeVisible && card.activeSelf)
            {
                card.SetActive(false);
            }
            
            if (card.activeSelf)
            {
                visibleCount++;
            }
        }
    }

    void UpdatePlaneDetection()
    {
        if (NRFrame.SessionStatus != SessionState.Running) return;

        NRFrame.GetTrackables<NRTrackablePlane>(_detectedPlanes, NRTrackableQueryFilter.All);
        
        foreach (var plane in _detectedPlanes)
        {
            if (plane.GetTrackingState() == TrackingState.Tracking)
            {
                // 水平面（床）を検出
                if (!_floorDetected && plane.GetPlaneType() == TrackablePlaneType.HORIZONTAL)
                {
                    Pose centerPose = plane.GetCenterPose();
                    _detectedFloorHeight = centerPose.position.y;
                    _floorDetected = true;
                    Log($"Floor detected at height: {_detectedFloorHeight:F2}");
                }
                
                // 垂直面（壁）を検出
                if (!_wallDetected && plane.GetPlaneType() == TrackablePlaneType.VERTICAL)
                {
                    _wallPose = plane.GetCenterPose();
                    _wallDetected = true;
                    Log($"Wall detected at position: {_wallPose.position}, rotation: {_wallPose.rotation.eulerAngles}");
                }
            }
        }
    }

    /// <summary>
    /// 床の高さを取得（平面検出使用時）
    /// </summary>
    float GetFloorHeight()
    {
        if (_floorDetected)
        {
            return _detectedFloorHeight;
        }
        
        // 平面が検出されていない場合はカメラの1.5m下を使用
        if (_arCamera != null)
        {
            return _arCamera.transform.position.y - 1.5f;
        }
        
        return 0f;
    }

    void Log(string msg)
    {
        if (imageUploader != null) imageUploader.Log($"[Corridor] {msg}");
        else Debug.Log($"[TimelineCorridor] {msg}");
    }

    /// <summary>
    /// タイムスタンプを読みやすい形式にフォーマット
    /// </summary>
    string FormatTimestamp(string timestamp, string filename)
    {
        // item.timestampがある場合はそれを使用
        if (!string.IsNullOrEmpty(timestamp))
        {
            return timestamp;
        }
        
        // ファイル名から日時をパース（形式: YYYYMMDD_HHMMSS_xxx.jpg）
        if (!string.IsNullOrEmpty(filename) && filename.Length >= 15)
        {
            try
            {
                string dateStr = filename.Substring(0, 8); // YYYYMMDD
                string timeStr = filename.Substring(9, 6); // HHMMSS
                
                string year = dateStr.Substring(0, 4);
                string month = dateStr.Substring(4, 2);
                string day = dateStr.Substring(6, 2);
                string hour = timeStr.Substring(0, 2);
                string minute = timeStr.Substring(2, 2);
                
                return $"{year}/{month}/{day} {hour}:{minute}";
            }
            catch
            {
                return filename;
            }
        }
        
        return "No Date";
    }

    /// <summary>
    /// 検索結果をギャラリー回廊として表示
    /// </summary>
    public void DisplayResults(List<SearchResultItem> results, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        Log($"DisplayResults: {results?.Count ?? 0} results");
        ClearCards();

        if (results == null || results.Count == 0) return;

        // Reverse results so newest (most recent) appears first (in front of player)
        var reversedResults = new List<SearchResultItem>(results);
        reversedResults.Reverse();
        _allResults = reversedResults;
        StartCoroutine(SpawnCorridorWithAnimation(_allResults, onImageLoaded));
    }

    IEnumerator SpawnCorridorWithAnimation(List<SearchResultItem> results, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        if (_arCamera == null) _arCamera = Camera.main;

        // 平面検出を待機（床と壁の両方を探す）
        if (usePlaneDetection && (!_floorDetected || !_wallDetected))
        {
            Log("Waiting for plane detection (floor/wall)...");
            float waitTime = 0f;
            while ((!_floorDetected || !_wallDetected) && waitTime < planeSearchTimeout)
            {
                yield return new WaitForSeconds(0.2f);
                waitTime += 0.2f;
            }
            
            Log($"Detection result - Floor: {_floorDetected}, Wall: {_wallDetected}");
            if (_floorDetected)
            {
                Log($"Floor found at {_detectedFloorHeight:F2}m");
            }
            else
            {
                Log("Floor not detected, using estimated height");
            }
        }

        // コンテナをワールド空間に固定（親から切り離し）
        if (corridorContainer != null)
        {
            corridorContainer.SetParent(null);
            corridorContainer.position = Vector3.zero;
            corridorContainer.rotation = Quaternion.identity;
            corridorContainer.localScale = Vector3.one;
        }

        // 床の高さを取得
        float floorHeight = GetFloorHeight();
        float eyeHeight = _arCamera.transform.position.y - floorHeight;
        Log($"Eye height from floor: {eyeHeight:F2}m");

        // カード配置の基準を決定
        Vector3 basePosition;
        Vector3 displayRight;
        Vector3 displayForward;
        float baseY = floorHeight + 1.4f; // 床から140cm（ギャラリー標準展示高さ）

        if (_wallDetected && usePlaneDetection)
        {
            // 壁が検出された場合：壁に沿って配置
            Log("Using wall for card placement");
            basePosition = _wallPose.position;
            displayForward = _wallPose.rotation * Vector3.forward; // 壁の法線（壁から手前方向）
            displayRight = _wallPose.rotation * Vector3.right; // 壁に沿った水平方向
            
            // 壁から少し離す
            basePosition += displayForward * 0.1f;
        }
        else
        {
            // 壁が検出されない場合：カメラの前方に配置
            Log("Using camera forward for card placement");
            Vector3 cameraPos = _arCamera.transform.position;
            Vector3 cameraForward = new Vector3(_arCamera.transform.forward.x, 0, _arCamera.transform.forward.z).normalized;
            Vector3 cameraRight = new Vector3(_arCamera.transform.right.x, 0, _arCamera.transform.right.z).normalized;
            
            basePosition = cameraPos + cameraForward * startDistance;
            displayForward = -cameraForward; // カメラの方を向く
            displayRight = cameraRight;
        }

        // 片面配置：行と列でグリッド配置
        int numRows = Mathf.CeilToInt((float)results.Count / cardsPerRow);
        
        // フロアラインを作成（壁検出時はスキップ）
        if (!_wallDetected)
        {
            Vector3 cameraPos = _arCamera.transform.position;
            Vector3 cameraForward = new Vector3(_arCamera.transform.forward.x, 0, _arCamera.transform.forward.z).normalized;
            CreateFloorLines(cameraPos, cameraForward, numRows, floorHeight);
        }

        int cardIndex = 0;
        for (int row = 0; row < numRows && cardIndex < results.Count; row++)
        {
            float depth = row * sectionSpacing;

            for (int col = 0; col < cardsPerRow && cardIndex < results.Count; col++)
            {
                // 左右交互に配置（回廊スタイル）
                bool isLeftSide = (col % 2 == 0);
                float xOffset = isLeftSide ? -sideOffset : sideOffset;

                // 上下の配置（同じ側の2枚目は少し下に）
                bool isSecondOnSide = (col >= 2);
                float yOffset = isSecondOnSide ? -verticalSpacing : 0f;

                // 位置計算（カメラの横に配置）
                Vector3 cardPosition = basePosition
                    + displayRight * xOffset        // 左右にオフセット
                    + displayForward * (-depth)     // 奥に向かって配置
                    + Vector3.up * (baseY - basePosition.y + yOffset);

                // カードの向き（斜め正面 - 45度内側を向く）
                Vector3 toCamera = _arCamera.transform.position - cardPosition;
                toCamera.y = 0;
                toCamera.Normalize();
                
                // 左側のカードは右斜め前、右側のカードは左斜め前を向く
                float angleOffset = isLeftSide ? 45f : -45f;
                Vector3 cardForward = Quaternion.Euler(0, angleOffset, 0) * toCamera;

                // カード作成（isLeftSide を渡す）
                GameObject card = CreateCorridorCard(cardPosition, cardForward, results[cardIndex], cardIndex, isLeftSide, depth, onImageLoaded);
                if (card != null)
                {
                    _cards.Add(card);
                    // アニメーション無効化（テスト用）
                    // StartCoroutine(AnimateCardIn(card, fadeInDuration));
                }

                cardIndex++;
                yield return new WaitForSeconds(fadeInDelay);
            }
        }

        Log($"Created {_cards.Count} cards (wall detected: {_wallDetected})");
    }

    GameObject CreateCorridorCard(Vector3 position, Vector3 corridorForward, SearchResultItem item, int index, bool isLeftSide, float depth, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        // ルートGameObject
        GameObject card = new GameObject($"CorridorCard_{index}");
        card.transform.position = position;
        
        // corridorContainerの子にするが、ワールド座標は維持
        if (corridorContainer != null)
        {
            card.transform.SetParent(corridorContainer, true); // worldPositionStays = true
        }

        // カードを回廊の内側（中央）に向ける
        // corridorForwardは水平化されているので安定
        float inwardAngle = isLeftSide ? -80f : 80f; // 左側は右を向き、右側は左を向く
        Quaternion baseRot = Quaternion.LookRotation(-corridorForward);
        card.transform.rotation = baseRot * Quaternion.Euler(0, inwardAngle, 0);

        // 奥行きに応じたスケール（奥は少し小さく）
        float depthRatio = Mathf.Clamp01((depth - startDistance) / (sectionSpacing * 5f));
        float scaleMultiplier = Mathf.Lerp(1f, 0.85f, depthRatio);
        card.transform.localScale = Vector3.one * scaleMultiplier;

        // グローエフェクト（丸角シェーダー使用）- 最大輝度
        GameObject glowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glowQuad.name = "GlowFrame";
        glowQuad.transform.SetParent(card.transform);
        glowQuad.transform.localPosition = new Vector3(0, 0, 0.02f);
        glowQuad.transform.localRotation = Quaternion.identity;
        glowQuad.transform.localScale = new Vector3(cardWidth * 1.2f, cardHeight * 1.2f, 1f);

        var glowRenderer = glowQuad.GetComponent<MeshRenderer>();
        Shader roundedShader = Shader.Find("Custom/RoundedTexture");
        Material glowMat;
        if (roundedShader != null)
        {
            glowMat = new Material(roundedShader);
            glowMat.SetFloat("_Radius", 0.06f);
        }
        else
        {
            glowMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default"));
        }
        // 最大輝度のグロー枠（HDRカラー）
        glowMat.color = new Color(glowColor.r * 3f, glowColor.g * 3f, glowColor.b * 3f, 1f);
        glowRenderer.material = glowMat;

        var glowCollider = glowQuad.GetComponent<Collider>();
        if (glowCollider != null) Destroy(glowCollider);

        // 暗い背景パネル（コントラスト向上用）
        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.name = "Background";
        bgQuad.transform.SetParent(card.transform);
        bgQuad.transform.localPosition = new Vector3(0, 0, 0.005f);
        bgQuad.transform.localRotation = Quaternion.identity;
        bgQuad.transform.localScale = new Vector3(cardWidth * 1.02f, cardHeight * 1.02f, 1f);
        
        var bgRenderer = bgQuad.GetComponent<MeshRenderer>();
        Material bgMat;
        if (roundedShader != null)
        {
            bgMat = new Material(roundedShader);
            bgMat.SetFloat("_Radius", 0.06f);
        }
        else
        {
            bgMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default"));
        }
        bgMat.color = new Color(0, 0, 0, 1f);
        bgRenderer.material = bgMat;
        
        var bgCollider = bgQuad.GetComponent<Collider>();
        if (bgCollider != null) Destroy(bgCollider);

        // メイン画像用のQuad（丸角シェーダー使用）
        GameObject imageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imageQuad.name = "ImageQuad";
        imageQuad.transform.SetParent(card.transform);
        imageQuad.transform.localPosition = Vector3.zero;
        imageQuad.transform.localRotation = Quaternion.identity;
        imageQuad.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

        var imageRenderer = imageQuad.GetComponent<MeshRenderer>();
        Material imageMat;
        if (roundedShader != null)
        {
            imageMat = new Material(roundedShader);
            imageMat.SetFloat("_Radius", 0.06f);
        }
        else
        {
            imageMat = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("UI/Default"));
        }
        // 最大輝度で表示（HDRカラー：白より明るく）
        imageMat.color = new Color(2f, 2f, 2f, 1f);
        imageRenderer.material = imageMat;

        var imageCollider = imageQuad.GetComponent<Collider>();
        if (imageCollider != null) Destroy(imageCollider);

        // フォグ効果は無効化（輝度優先）
        // float fogAmount = depthRatio * 0.4f;
        // ApplyFogEffect(card, fogAmount);

        // タイムスタンプ表示（画像の下）
        GameObject timestampObj = new GameObject("Timestamp");
        timestampObj.transform.SetParent(card.transform);
        timestampObj.transform.localPosition = new Vector3(0, -cardHeight * 0.65f, -0.01f);
        timestampObj.transform.localRotation = Quaternion.identity;
        timestampObj.transform.localScale = Vector3.one * 0.009f; // 3倍サイズ
        
        TextMesh timestampText = timestampObj.AddComponent<TextMesh>();
        // タイムスタンプをパース（形式: "YYYYMMDD_HHMMSS_xxx.jpg" または item.timestamp）
        string displayTime = FormatTimestamp(item.timestamp, item.filename);
        timestampText.text = displayTime;
        timestampText.fontSize = 60; // フォントサイズ増加
        timestampText.color = Color.white;
        timestampText.anchor = TextAnchor.UpperCenter;
        timestampText.alignment = TextAlignment.Center;
        timestampText.characterSize = 0.8f; // 文字サイズ増加

        // BoxColliderを追加（選択用）
        BoxCollider boxCollider = card.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(cardWidth, cardHeight, 0.1f);

        // ResultCard3Dコンポーネントを追加
        ResultCard3D cardComponent = card.AddComponent<ResultCard3D>();
        cardComponent.imageRenderer = imageRenderer;
        cardComponent.glowRenderer = glowRenderer;
        cardComponent.Setup(item, index, onImageLoaded);

        // 自動で画像ダウンロードを開始
        if (imageUploader != null)
        {
            StartCoroutine(DownloadAndApplyTexture(imageRenderer, item));
        }

        // 初期状態は透明（無効化：すぐに表示）
        // SetCardAlpha(card, 0f);

        return card;
    }

    IEnumerator DownloadAndApplyTexture(MeshRenderer renderer, SearchResultItem item)
    {
        yield return StartCoroutine(imageUploader.DownloadImage(item.url, (tex) =>
        {
            if (tex != null && renderer != null)
            {
                renderer.material.mainTexture = tex;
                // 最大輝度で表示（HDRカラー）
                renderer.material.color = new Color(2f, 2f, 2f, 1f);
            }
        }));
    }

    void CreateFloorLines(Vector3 cameraPos, Vector3 cameraForward, int numSections, float floorY)
    {
        for (int i = 0; i < numSections + 1; i++)
        {
            float depth = startDistance + i * sectionSpacing - sectionSpacing * 0.5f;
            
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
            line.name = $"FloorLine_{i}";
            line.transform.SetParent(corridorContainer);
            
            // 検出した床の高さを使用
            Vector3 linePos = new Vector3(
                cameraPos.x + cameraForward.x * depth,
                floorY + 0.01f, // 床のすぐ上に配置
                cameraPos.z + cameraForward.z * depth
            );
            line.transform.position = linePos;
            line.transform.rotation = Quaternion.Euler(90, 0, 0);
            line.transform.localScale = new Vector3(sideOffset * 2.5f, 0.03f, 1f);

            var lineRenderer = line.GetComponent<MeshRenderer>();
            var lineMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default"));
            float alpha = Mathf.Lerp(0.4f, 0.1f, (float)i / (numSections + 1));
            lineMaterial.color = new Color(floorLineColor.r, floorLineColor.g, floorLineColor.b, alpha);
            lineRenderer.material = lineMaterial;

            var lineCollider = line.GetComponent<Collider>();
            if (lineCollider != null) Destroy(lineCollider);
        }
    }

    void ApplyFogEffect(GameObject card, float fogAmount)
    {
        var renderers = card.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                Color color = renderer.material.color;
                color = Color.Lerp(color, new Color(0.15f, 0.15f, 0.2f, color.a * 0.8f), fogAmount);
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// DOTweenを使用したカード登場アニメーション
    /// </summary>
    void AnimateCardInDOTween(GameObject card, float duration)
    {
        Vector3 targetScale = card.transform.localScale;
        Vector3 targetPos = card.transform.position;
        
        // 初期状態
        card.transform.localScale = targetScale * 0.3f;
        card.transform.position = targetPos + Vector3.down * 0.2f;
        SetCardAlpha(card, 0f);
        
        // DOTweenシーケンス
        Sequence seq = DOTween.Sequence();
        
        // スケールアニメーション（バウンス効果）
        seq.Append(card.transform.DOScale(targetScale, duration).SetEase(Ease.OutBack));
        
        // 位置アニメーション（同時実行）
        seq.Join(card.transform.DOMove(targetPos, duration).SetEase(Ease.OutCubic));
        
        // アルファアニメーション（マテリアルをフェードイン）
        var renderers = card.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                Color targetColor = renderer.material.color;
                renderer.material.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
                seq.Join(renderer.material.DOColor(targetColor, duration * 0.7f));
            }
        }
    }

    // 互換性のためのラッパー（コルーチンとして呼び出す場合用）
    IEnumerator AnimateCardIn(GameObject card, float duration)
    {
        AnimateCardInDOTween(card, duration);
        yield return new WaitForSeconds(duration);
    }

    void SetCardAlpha(GameObject card, float alpha)
    {
        var renderers = card.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                Color color = renderer.material.color;
                color.a *= alpha;
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// カードを選択（フラッシュバック効果）
    /// 同じカードを再度選択すると詳細ビューを表示
    /// </summary>
    public void SelectCard(int index)
    {
        if (index < 0 || index >= _cards.Count) return;
        
        // 詳細ビューが開いている場合は閉じる
        if (_isDetailViewOpen)
        {
            CloseDetailView();
            return;
        }

        // 同じカードを再度選択 → 詳細ビューを表示
        if (_selectedIndex == index)
        {
            ShowCardDetail(index);
            return;
        }

        if (_selectedIndex >= 0 && _selectedIndex < _cards.Count)
        {
            StartCoroutine(AnimateCardDeselect(_cards[_selectedIndex]));
        }

        _selectedIndex = index;
        StartCoroutine(AnimateCardSelect(_cards[index]));
    }

    /// <summary>
    /// DOTweenを使用したカード選択アニメーション
    /// </summary>
    IEnumerator AnimateCardSelect(GameObject card)
    {
        Vector3 endScale = card.transform.localScale * selectedScale;
        Vector3 endPos = card.transform.position + (_arCamera.transform.forward * -0.3f);
        
        // DOTweenでアニメーション
        Sequence seq = DOTween.Sequence();
        seq.Append(card.transform.DOScale(endScale, 0.3f).SetEase(Ease.OutBack));
        seq.Join(card.transform.DOMove(endPos, 0.3f).SetEase(Ease.OutQuad));
        
        // グロー効果を強化
        var glowRenderer = card.transform.Find("GlowFrame")?.GetComponent<MeshRenderer>();
        if (glowRenderer != null)
        {
            Color originalColor = glowRenderer.material.color;
            glowRenderer.material.DOColor(originalColor * 2f, 0.2f)
                .SetLoops(2, LoopType.Yoyo);
        }
        
        yield return seq.WaitForCompletion();
    }

    /// <summary>
    /// DOTweenを使用したカード選択解除アニメーション
    /// </summary>
    IEnumerator AnimateCardDeselect(GameObject card)
    {
        Vector3 endScale = card.transform.localScale / selectedScale;
        
        card.transform.DOScale(endScale, 0.2f).SetEase(Ease.OutQuad);
        
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
        _allResults.Clear();
        _selectedIndex = -1;

        if (corridorContainer != null)
        {
            foreach (Transform child in corridorContainer)
            {
                if (child.name.StartsWith("FloorLine_"))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // ============ カード詳細ビュー ============

    /// <summary>
    /// カード詳細ビューを表示
    /// </summary>
    public void ShowCardDetail(int index)
    {
        if (index < 0 || index >= _cards.Count || index >= _allResults.Count) return;
        if (_isDetailViewOpen) CloseDetailView();
        
        _isDetailViewOpen = true;
        _detailCardIndex = index;
        
        var card = _cards[index];
        var item = _allResults[index];
        
        // 詳細パネルを作成
        _detailPanel = new GameObject("DetailPanel");
        _detailPanel.transform.SetParent(corridorContainer);
        
        // カメラの前に配置
        Vector3 detailPos = _arCamera.transform.position + _arCamera.transform.forward * 1.0f;
        _detailPanel.transform.position = detailPos;
        _detailPanel.transform.LookAt(_arCamera.transform);
        _detailPanel.transform.Rotate(0, 180, 0); // カメラの方を向く
        
        // 背景パネル
        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.name = "Background";
        bgQuad.transform.SetParent(_detailPanel.transform);
        bgQuad.transform.localPosition = Vector3.zero;
        bgQuad.transform.localRotation = Quaternion.identity;
        bgQuad.transform.localScale = new Vector3(0.8f, 1.0f, 1f);
        
        var bgRenderer = bgQuad.GetComponent<MeshRenderer>();
        bgRenderer.material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default"));
        bgRenderer.material.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        var bgCollider = bgQuad.GetComponent<Collider>();
        if (bgCollider != null) Destroy(bgCollider);
        
        // 拡大画像
        GameObject imageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imageQuad.name = "DetailImage";
        imageQuad.transform.SetParent(_detailPanel.transform);
        imageQuad.transform.localPosition = new Vector3(0, 0.15f, -0.01f);
        imageQuad.transform.localRotation = Quaternion.identity;
        imageQuad.transform.localScale = new Vector3(0.6f, 0.5f, 1f);
        
        var imageRenderer = imageQuad.GetComponent<MeshRenderer>();
        imageRenderer.material = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("UI/Default"));
        
        // 元カードからテクスチャをコピー
        var originalRenderer = card.transform.Find("ImageQuad")?.GetComponent<MeshRenderer>();
        if (originalRenderer != null && originalRenderer.material.mainTexture != null)
        {
            imageRenderer.material.mainTexture = originalRenderer.material.mainTexture;
        }
        
        var imgCollider = imageQuad.GetComponent<Collider>();
        if (imgCollider != null) Destroy(imgCollider);
        
        // メタデータテキスト
        GameObject textObj = new GameObject("MetadataText");
        textObj.transform.SetParent(_detailPanel.transform);
        textObj.transform.localPosition = new Vector3(0, -0.3f, -0.01f);
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.005f;
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = $"📷 {item.filename}\n" +
                       $"📅 {item.timestamp}\n" +
                       $"🏷️ {string.Join(", ", item.objects ?? new List<string>())}";
        textMesh.fontSize = 40;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.UpperCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // 閉じるボタン
        GameObject closeBtn = GameObject.CreatePrimitive(PrimitiveType.Quad);
        closeBtn.name = "CloseButton";
        closeBtn.transform.SetParent(_detailPanel.transform);
        closeBtn.transform.localPosition = new Vector3(0.35f, 0.45f, -0.02f);
        closeBtn.transform.localRotation = Quaternion.identity;
        closeBtn.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
        
        var closeRenderer = closeBtn.GetComponent<MeshRenderer>();
        closeRenderer.material = new Material(Shader.Find("Unlit/Color"));
        closeRenderer.material.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        // DOTweenでフェードイン
        _detailPanel.transform.localScale = Vector3.zero;
        _detailPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        
        Log($"Detail view opened for card {index}");
    }

    /// <summary>
    /// 詳細ビューを閉じる
    /// </summary>
    public void CloseDetailView()
    {
        if (!_isDetailViewOpen || _detailPanel == null) return;
        
        // DOTweenでフェードアウト
        _detailPanel.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                if (_detailPanel != null)
                {
                    Destroy(_detailPanel);
                    _detailPanel = null;
                }
            });
        
        _isDetailViewOpen = false;
        _detailCardIndex = -1;
        
        Log("Detail view closed");
    }

    /// <summary>
    /// 詳細ビューが開いているかどうか
    /// </summary>
    public bool IsDetailViewOpen => _isDetailViewOpen;

    // ============ スライドショー機能 ============
    private bool _isSlideshowPlaying = false;
    private Coroutine _slideshowCoroutine;

    /// <summary>
    /// スライドショーを開始（画像が手前に流れてくる）
    /// </summary>
    public void StartSlideshow()
    {
        if (_isSlideshowPlaying)
        {
            StopSlideshow();
            return;
        }
        
        if (_cards.Count == 0)
        {
            Log("No cards to slideshow");
            return;
        }
        
        _isSlideshowPlaying = true;
        _slideshowCoroutine = StartCoroutine(SlideshowRoutine());
        Log("Slideshow started");
    }

    /// <summary>
    /// スライドショーを停止
    /// </summary>
    public void StopSlideshow()
    {
        if (_slideshowCoroutine != null)
        {
            StopCoroutine(_slideshowCoroutine);
            _slideshowCoroutine = null;
        }
        _isSlideshowPlaying = false;
        Log("Slideshow stopped");
    }

    /// <summary>
    /// スライドショーのコルーチン
    /// </summary>
    IEnumerator SlideshowRoutine()
    {
        float slideInterval = 2.0f; // 各画像の表示時間
        float moveDistance = 0.8f;  // カメラに近づく距離
        float moveDuration = 0.5f;  // 移動時間
        
        int currentIndex = 0;
        GameObject previousCard = null;
        Vector3 previousOriginalPos = Vector3.zero;
        Vector3 previousOriginalScale = Vector3.one;
        
        while (_isSlideshowPlaying && currentIndex < _cards.Count)
        {
            var card = _cards[currentIndex];
            if (card == null)
            {
                currentIndex++;
                continue;
            }
            
            // 前のカードを元の位置に戻す
            if (previousCard != null)
            {
                previousCard.transform.DOMove(previousOriginalPos, moveDuration * 0.5f).SetEase(Ease.InQuad);
                previousCard.transform.DOScale(previousOriginalScale, moveDuration * 0.5f).SetEase(Ease.InQuad);
            }
            
            // 現在の位置を保存
            Vector3 originalPos = card.transform.position;
            Vector3 originalScale = card.transform.localScale;
            
            // カメラの方向を計算
            Vector3 toCam = (_arCamera.transform.position - card.transform.position).normalized;
            Vector3 targetPos = card.transform.position + toCam * moveDistance;
            targetPos.y = _arCamera.transform.position.y; // 目の高さに合わせる
            
            // カードを手前に移動＆拡大
            card.transform.DOMove(targetPos, moveDuration).SetEase(Ease.OutQuad);
            card.transform.DOScale(originalScale * 1.5f, moveDuration).SetEase(Ease.OutBack);
            
            // グロー効果を強化
            var glowObj = card.transform.Find("GlowFrame");
            if (glowObj != null)
            {
                var glowRenderer = glowObj.GetComponent<MeshRenderer>();
                if (glowRenderer != null)
                {
                    Color originalColor = glowRenderer.material.color;
                    glowRenderer.material.DOColor(originalColor * 2f, moveDuration);
                }
            }
            
            previousCard = card;
            previousOriginalPos = originalPos;
            previousOriginalScale = originalScale;
            
            yield return new WaitForSeconds(slideInterval);
            currentIndex++;
        }
        
        // 最後のカードを元の位置に戻す
        if (previousCard != null)
        {
            previousCard.transform.DOMove(previousOriginalPos, moveDuration).SetEase(Ease.InQuad);
            previousCard.transform.DOScale(previousOriginalScale, moveDuration).SetEase(Ease.InQuad);
        }
        
        _isSlideshowPlaying = false;
        Log("Slideshow completed");
    }

    /// <summary>
    /// スライドショーが再生中かどうか
    /// </summary>
    public bool IsSlideshowPlaying => _isSlideshowPlaying;
}
