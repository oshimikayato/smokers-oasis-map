using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 無限スクロール形式のカードカルーセル
/// 右から新しいカードがフェードイン、左でフェードアウトして次の画像に切り替わる
/// タッチ/ドラッグでスクロールを制御可能
/// </summary>
public class InfiniteCardCarousel : MonoBehaviour
{
    [Header("References")]
    public ARSearchResultDisplay baseDisplay;
    public Transform cardContainer;
    public ImageUploader imageUploader;

    [Header("Layout Settings")]
    public int visibleCards = 5;
    public float cardWidth = 0.4f;
    public float cardSpacing = 0.1f;
    public float distanceFromCamera = 2.0f;
    public float depthMultiplier = 0.2f;
    public float rotationMultiplier = 25f;

    [Header("Animation Settings")]
    public float autoScrollSpeed = 0.3f;
    public float fadeEdge = 0.3f;
    public bool autoScrollEnabled = true;

    [Header("Drag Control Settings")]
    public float dragSensitivity = 2.0f;
    public float momentumDecay = 0.95f;
    public float minMomentum = 0.01f;

    [Header("Visual Settings")]
    public Color glowColor = new Color(0.3f, 0.7f, 1f, 0.8f);

    [Header("Slideshow Settings")]
    public bool enableSlideshow = false; // デフォルト無効（パフォーマンス優先）
    public float slideshowInterval = 2.0f; // 画像切り替え間隔（秒）
    public int timeGroupingMinutes = 5; // 同時刻帯とみなす分数

    private List<SearchResultItem> _allResults = new List<SearchResultItem>();
    private List<List<SearchResultItem>> _timeGroups = new List<List<SearchResultItem>>(); // 時刻グループ
    private List<CardSlot> _cardSlots = new List<CardSlot>();
    private int _direction = 1;
    private bool _isPaused = false;
    private Camera _arCamera;

    // ドラッグ制御用
    private bool _isDragging = false;
    private Vector3 _lastInputPosition;
    private float _dragVelocity = 0f;
    private float _momentum = 0f;
    private float _manualScrollOffset = 0f;

    private class CardSlot
    {
        public GameObject cardObject;
        public MeshRenderer imageRenderer;
        public MeshRenderer glowRenderer;
        public TextMesh statusText; // ダウンロード状態表示用
        public int groupIndex; // 時刻グループのインデックス
        public int imageIndexInGroup; // グループ内の現在の画像インデックス
        public float position;
        public List<Texture2D> textures; // グループ内の全テクスチャ
        public float slideshowTimer;
        public bool isDownloading; // ダウンロード中フラグ
        public int downloadedCount; // ダウンロード完了数
        public int totalInGroup; // グループ内の総数
    }

    void Start()
    {
        _arCamera = baseDisplay?.arCamera ?? Camera.main;
    }

    void Update()
    {
        if (_timeGroups.Count == 0 || _cardSlots.Count == 0) return;

        HandleInput();

        // スクロール処理
        if (_isDragging)
        {
            // ドラッグ中は手動スクロール
        }
        else if (Mathf.Abs(_momentum) > minMomentum)
        {
            // 慣性スクロール
            _manualScrollOffset += _momentum * Time.deltaTime;
            _momentum *= momentumDecay;
        }
        else if (autoScrollEnabled && !_isPaused)
        {
            // 自動スクロール
            _manualScrollOffset += autoScrollSpeed * _direction * Time.deltaTime;
        }

        // カード位置を更新
        UpdateCardPositions();

        // スライドショー更新
        if (enableSlideshow)
        {
            UpdateSlideshow();
        }
    }

    void UpdateSlideshow()
    {
        foreach (var slot in _cardSlots)
        {
            if (slot.textures == null || slot.textures.Count <= 1) continue;

            slot.slideshowTimer += Time.deltaTime;
            if (slot.slideshowTimer >= slideshowInterval)
            {
                slot.slideshowTimer = 0f;
                slot.imageIndexInGroup = (slot.imageIndexInGroup + 1) % slot.textures.Count;
                
                // 次の画像に切り替え
                if (slot.imageRenderer != null && slot.textures[slot.imageIndexInGroup] != null)
                {
                    slot.imageRenderer.material.mainTexture = slot.textures[slot.imageIndexInGroup];
                }
            }
        }
    }

    void HandleInput()
    {
        // タッチ入力
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartDrag(touch.position);
                    break;
                case TouchPhase.Moved:
                    UpdateDrag(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndDrag();
                    break;
            }
        }
        // マウス入力（エディタ用）
        else if (Input.GetMouseButtonDown(0))
        {
            StartDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && _isDragging)
        {
            UpdateDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    void StartDrag(Vector3 inputPosition)
    {
        _isDragging = true;
        _lastInputPosition = inputPosition;
        _dragVelocity = 0f;
        _momentum = 0f;
        Log("Drag started");
    }

    void UpdateDrag(Vector3 inputPosition)
    {
        if (!_isDragging) return;

        float deltaX = inputPosition.x - _lastInputPosition.x;
        float scrollAmount = deltaX * dragSensitivity * 0.001f;
        
        _manualScrollOffset -= scrollAmount; // 左にドラッグで前進
        _dragVelocity = -scrollAmount / Time.deltaTime;
        
        _lastInputPosition = inputPosition;
    }

    void EndDrag()
    {
        if (_isDragging)
        {
            _momentum = _dragVelocity * 0.5f; // 慣性を設定
            _isDragging = false;
            Log($"Drag ended, momentum: {_momentum:F2}");
        }
    }

    void Log(string msg)
    {
        if (imageUploader != null) imageUploader.Log($"[Carousel] {msg}");
        else Debug.Log($"[InfiniteCarousel] {msg}");
    }

    /// <summary>
    /// 検索結果を表示
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
        
        // 時刻でグループ化
        GroupByTime(_allResults);
        Log($"Grouped into {_timeGroups.Count} time groups (newest first)");
        
        // 中央に最新画像（インデックス0）を表示するためのオフセット
        float centerOffset = (visibleCards - 1) / 2f;
        _manualScrollOffset = -centerOffset;

        // 表示するカード数を決定（グループ数に基づく）
        int cardCount = Mathf.Min(visibleCards, _timeGroups.Count);
        
        // カードスロットを作成
        for (int i = 0; i < cardCount; i++)
        {
            CreateCardSlot(i, onImageLoaded);
        }

        Log($"Created {_cardSlots.Count} card slots, center offset: {centerOffset}");
    }

    /// <summary>
    /// ファイル名のタイムスタンプで画像をグループ化
    /// </summary>
    void GroupByTime(List<SearchResultItem> results)
    {
        _timeGroups.Clear();
        
        foreach (var item in results)
        {
            // ファイル名からタイムスタンプを抽出 (YYYYMMDD_HHMMSS形式)
            string timestamp = ExtractTimestamp(item.filename);
            
            // 既存グループと比較
            bool addedToGroup = false;
            foreach (var group in _timeGroups)
            {
                if (group.Count > 0)
                {
                    string groupTimestamp = ExtractTimestamp(group[0].filename);
                    if (AreTimestampsClose(timestamp, groupTimestamp))
                    {
                        group.Add(item);
                        addedToGroup = true;
                        break;
                    }
                }
            }
            
            // 新しいグループを作成
            if (!addedToGroup)
            {
                var newGroup = new List<SearchResultItem> { item };
                _timeGroups.Add(newGroup);
            }
        }
    }

    string ExtractTimestamp(string filename)
    {
        // ファイル名形式: YYYYMMDD_HHMMSS_objects.jpg
        if (string.IsNullOrEmpty(filename)) return "";
        
        string name = System.IO.Path.GetFileNameWithoutExtension(filename);
        string[] parts = name.Split('_');
        
        if (parts.Length >= 2)
        {
            return parts[0] + "_" + parts[1]; // YYYYMMDD_HHMMSS
        }
        return name;
    }

    bool AreTimestampsClose(string ts1, string ts2)
    {
        // タイムスタンプ形式: YYYYMMDD_HHMMSS
        if (ts1.Length < 15 || ts2.Length < 15) return false;
        
        try
        {
            // 時間部分を抽出して比較
            string time1 = ts1.Substring(9, 6); // HHMMSS
            string time2 = ts2.Substring(9, 6);
            
            int h1 = int.Parse(time1.Substring(0, 2));
            int m1 = int.Parse(time1.Substring(2, 2));
            int s1 = int.Parse(time1.Substring(4, 2));
            
            int h2 = int.Parse(time2.Substring(0, 2));
            int m2 = int.Parse(time2.Substring(2, 2));
            int s2 = int.Parse(time2.Substring(4, 2));
            
            int seconds1 = h1 * 3600 + m1 * 60 + s1;
            int seconds2 = h2 * 3600 + m2 * 60 + s2;
            
            // 日付も同じかチェック
            string date1 = ts1.Substring(0, 8);
            string date2 = ts2.Substring(0, 8);
            
            if (date1 != date2) return false;
            
            return Mathf.Abs(seconds1 - seconds2) <= timeGroupingMinutes * 60;
        }
        catch
        {
            return false;
        }
    }

    void CreateCardSlot(int slotIndex, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        int dataIndex = slotIndex % _allResults.Count;
        
        // カードオブジェクトを作成
        GameObject card = new GameObject($"Card_{slotIndex}");
        card.transform.SetParent(cardContainer);

        // グロー背景
        GameObject glowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glowQuad.name = "Glow";
        glowQuad.transform.SetParent(card.transform);
        glowQuad.transform.localPosition = new Vector3(0, 0, 0.02f);
        glowQuad.transform.localScale = new Vector3(cardWidth * 1.15f, cardWidth * 1.4f * 1.15f, 1f);
        
        var glowRenderer = glowQuad.GetComponent<MeshRenderer>();
        // 丸角シェーダーを使用（なければフォールバック）
        Shader roundedGlowShader = Shader.Find("Custom/RoundedTexture");
        Material glowMat;
        if (roundedGlowShader != null)
        {
            glowMat = new Material(roundedGlowShader);
            glowMat.SetFloat("_Radius", 0.08f);
        }
        else
        {
            glowMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default"));
        }
        // グローをより明るく鮮やかに
        glowMat.color = new Color(glowColor.r * 1.5f, glowColor.g * 1.5f, glowColor.b * 1.5f, 0.9f);
        glowRenderer.material = glowMat;
        
        // コライダー削除
        var glowCol = glowQuad.GetComponent<Collider>();
        if (glowCol != null) Destroy(glowCol);

        // 暗い背景パネル（コントラスト向上用）
        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.name = "Background";
        bgQuad.transform.SetParent(card.transform);
        bgQuad.transform.localPosition = new Vector3(0, 0, 0.005f);
        bgQuad.transform.localScale = new Vector3(cardWidth * 1.02f, cardWidth * 1.4f * 1.02f, 1f);
        
        var bgRenderer = bgQuad.GetComponent<MeshRenderer>();
        Material bgMat;
        if (roundedGlowShader != null)
        {
            bgMat = new Material(roundedGlowShader);
            bgMat.SetFloat("_Radius", 0.08f);
        }
        else
        {
            bgMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default"));
        }
        // 真っ黒な背景
        bgMat.color = new Color(0, 0, 0, 1f);
        bgRenderer.material = bgMat;
        
        var bgCol = bgQuad.GetComponent<Collider>();
        if (bgCol != null) Destroy(bgCol);

        // 画像クアッド
        GameObject imageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imageQuad.name = "Image";
        imageQuad.transform.SetParent(card.transform);
        imageQuad.transform.localPosition = Vector3.zero;
        imageQuad.transform.localScale = new Vector3(cardWidth, cardWidth * 1.4f, 1f);
        
        var imageRenderer = imageQuad.GetComponent<MeshRenderer>();
        // 丸角シェーダーを使用（なければフォールバック）
        Shader roundedShader = Shader.Find("Custom/RoundedTexture");
        Material imageMat;
        if (roundedShader != null)
        {
            imageMat = new Material(roundedShader);
            imageMat.SetFloat("_Radius", 0.08f); // 角の丸み
        }
        else
        {
            imageMat = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("UI/Default"));
        }
        imageMat.color = Color.gray;
        imageRenderer.material = imageMat;
        
        // コライダー削除
        var imageCol = imageQuad.GetComponent<Collider>();
        if (imageCol != null) Destroy(imageCol);

        // ステータステキスト（Loading表示用）
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(card.transform);
        statusObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        statusObj.transform.localRotation = Quaternion.identity;
        TextMesh statusText = statusObj.AddComponent<TextMesh>();
        statusText.text = "Loading...";
        statusText.fontSize = 20;
        statusText.characterSize = 0.02f;
        statusText.anchor = TextAnchor.MiddleCenter;
        statusText.alignment = TextAlignment.Center;
        statusText.color = Color.white;

        // グループインデックスを取得
        int groupIndex = slotIndex % _timeGroups.Count;
        var group = _timeGroups[groupIndex];

        // スロット情報を保存
        CardSlot slot = new CardSlot
        {
            cardObject = card,
            imageRenderer = imageRenderer,
            glowRenderer = glowRenderer,
            statusText = statusText,
            groupIndex = groupIndex,
            imageIndexInGroup = 0,
            position = slotIndex,
            textures = new List<Texture2D>(),
            slideshowTimer = slotIndex * 0.5f,
            isDownloading = true,
            downloadedCount = 0,
            totalInGroup = group.Count
        };
        _cardSlots.Add(slot);

        // 最初の画像のみをすぐにダウンロード（残りは後で順次）
        if (group.Count > 0)
        {
            StartCoroutine(DownloadFirstImage(slot, group[0]));
        }

        // 初期位置を設定
        UpdateCardStyle(slot);
    }

    IEnumerator DownloadFirstImage(CardSlot slot, SearchResultItem item)
    {
        if (imageUploader == null)
        {
            if (slot.statusText != null) slot.statusText.text = "Error";
            slot.isDownloading = false;
            yield break;
        }

        yield return StartCoroutine(imageUploader.DownloadImage(item.url, (tex) =>
        {
            if (tex != null && slot.textures != null)
            {
                slot.textures.Add(tex);
                slot.downloadedCount++;
                
                // 最初の画像を表示（最大輝度）
                if (slot.imageRenderer != null)
                {
                    slot.imageRenderer.material.mainTexture = tex;
                    slot.imageRenderer.material.color = new Color(2f, 2f, 2f, 1f); // HDR: 最大輝度
                }
                
                // ステータステキストを非表示
                if (slot.statusText != null)
                {
                    slot.statusText.gameObject.SetActive(false);
                }
                
                slot.isDownloading = false;
            }
            else
            {
                if (slot.statusText != null) slot.statusText.text = "Failed";
                slot.isDownloading = false;
            }
        }));
    }

    IEnumerator DownloadGroupImage(CardSlot slot, SearchResultItem item)
    {
        if (imageUploader == null) yield break;

        yield return StartCoroutine(imageUploader.DownloadImage(item.url, (tex) =>
        {
            if (tex != null && slot.textures != null)
            {
                slot.textures.Add(tex);
                
                // 最初の画像ならすぐに表示
                if (slot.textures.Count == 1 && slot.imageRenderer != null)
                {
                    slot.imageRenderer.material.mainTexture = tex;
                    slot.imageRenderer.material.color = Color.white;
                }
            }
        }));
    }

    void UpdateCardPositions()
    {
        if (_cardSlots.Count == 0 || _timeGroups.Count == 0) return;

        // 各カードスロットの位置を直接計算
        for (int i = 0; i < _cardSlots.Count; i++)
        {
            var slot = _cardSlots[i];
            
            // スロットの基本位置（0からvisibleCards-1）にスクロールオフセットを適用
            slot.position = i - (_manualScrollOffset % visibleCards);
            
            // 位置を0〜visibleCards の範囲に正規化
            while (slot.position < -0.5f) slot.position += visibleCards;
            while (slot.position >= visibleCards - 0.5f) slot.position -= visibleCards;

            // グループインデックスを計算（スクロール量から）
            int scrolledIndex = Mathf.FloorToInt(_manualScrollOffset);
            int newGroupIndex = (i + scrolledIndex) % _timeGroups.Count;
            if (newGroupIndex < 0) newGroupIndex += _timeGroups.Count;

            // グループインデックスが変わったら画像を更新
            if (slot.groupIndex != newGroupIndex)
            {
                slot.groupIndex = newGroupIndex;
                slot.imageIndexInGroup = 0;
                slot.slideshowTimer = 0f;
                slot.textures.Clear();
                slot.isDownloading = true;
                slot.downloadedCount = 0;
                
                // 画像をリセット
                if (slot.imageRenderer != null)
                {
                    slot.imageRenderer.material.color = Color.gray;
                    slot.imageRenderer.material.mainTexture = null;
                }
                
                // ステータステキストを表示
                if (slot.statusText != null)
                {
                    slot.statusText.gameObject.SetActive(true);
                    slot.statusText.text = "Loading...";
                }
                
                // 新しいグループの最初の画像のみをダウンロード
                var group = _timeGroups[newGroupIndex];
                slot.totalInGroup = group.Count;
                if (group.Count > 0)
                {
                    StartCoroutine(DownloadFirstImage(slot, group[0]));
                }
            }

            UpdateCardStyle(slot);
        }
    }

    void UpdateCardStyle(CardSlot slot)
    {
        if (slot.cardObject == null || _arCamera == null) return;

        float centerOffset = (visibleCards - 1) / 2f;
        float relativePos = slot.position - centerOffset;

        // X位置（カメラ基準で左右に配置）
        float x = relativePos * (cardWidth + cardSpacing);

        // Z位置（中央が手前、端が奥）
        float z = distanceFromCamera - Mathf.Abs(relativePos) * depthMultiplier;

        // Y軸回転
        float rotateY = relativePos * rotationMultiplier;

        // 透明度
        float opacity;
        float absPos = Mathf.Abs(relativePos);
        if (slot.position < 0 || slot.position >= visibleCards)
        {
            opacity = 0f;
        }
        else if (absPos >= 2f)
        {
            opacity = fadeEdge;
        }
        else if (absPos >= 1f)
        {
            opacity = Mathf.Lerp(1f, fadeEdge, (absPos - 1f));
        }
        else
        {
            opacity = 1f;
        }

        // スケール（中央が大きい）
        float scale = 1f - absPos * 0.1f;

        // カメラ基準で位置を設定
        Vector3 cameraPos = _arCamera.transform.position;
        Vector3 cameraForward = _arCamera.transform.forward;
        Vector3 cameraRight = _arCamera.transform.right;

        Vector3 worldPos = cameraPos + cameraForward * z + cameraRight * x;
        worldPos.y = cameraPos.y - 0.5f; // 地面に近い位置（カメラより0.5m下）

        slot.cardObject.transform.position = worldPos;
        slot.cardObject.transform.localScale = Vector3.one * scale;

        // カメラの方を向く + Y軸回転
        slot.cardObject.transform.LookAt(cameraPos);
        slot.cardObject.transform.Rotate(0, 180 + rotateY, 0);

        // 透明度を適用
        SetCardAlpha(slot, opacity);
    }

    void SetCardAlpha(CardSlot slot, float alpha)
    {
        if (slot.imageRenderer != null)
        {
            Color c = slot.imageRenderer.material.color;
            c.a = alpha;
            slot.imageRenderer.material.color = c;
        }
        if (slot.glowRenderer != null)
        {
            Color c = slot.glowRenderer.material.color;
            c.a = alpha * 0.6f;
            slot.glowRenderer.material.color = c;
        }
    }

    /// <summary>
    /// スクロール方向を切り替え
    /// </summary>
    public void SetDirection(int dir)
    {
        _direction = dir;
        Log($"Direction set to: {(dir == 1 ? "Forward" : "Reverse")}");
    }

    /// <summary>
    /// スクロールを一時停止/再開
    /// </summary>
    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }

    /// <summary>
    /// スクロール速度を設定
    /// </summary>
    public void SetSpeed(float speed)
    {
        autoScrollSpeed = speed;
    }

    /// <summary>
    /// 自動スクロールのON/OFF
    /// </summary>
    public void SetAutoScroll(bool enabled)
    {
        autoScrollEnabled = enabled;
    }

    /// <summary>
    /// カードをクリア
    /// </summary>
    public void ClearCards()
    {
        foreach (var slot in _cardSlots)
        {
            if (slot.cardObject != null)
            {
                Destroy(slot.cardObject);
            }
        }
        _cardSlots.Clear();
        _allResults.Clear();
        _timeGroups.Clear();
    }

    void OnDestroy()
    {
        ClearCards();
    }
}
