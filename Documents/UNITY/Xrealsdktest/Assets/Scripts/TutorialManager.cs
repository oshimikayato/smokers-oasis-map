using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

namespace NRKernal
{
    /// <summary>
    /// チュートリアルマネージャー - 初回起動時にチュートリアルを表示
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject tutorialPanel;
        public Text titleText;
        public Text contentText;
        public Button nextButton;
        public Button skipButton;
        public Text pageIndicator;
        public Image tutorialImage;
        public List<Sprite> tutorialPageImages = new List<Sprite>(); // InspectorまたはSetupToolから設定する画像リスト

        [Header("Settings")]
        public bool showOnlyOnFirstLaunch = false; // 展示用：毎回表示
        public string playerPrefsKey = "TutorialCompleted";

        private int _currentPage = 0;
        private List<TutorialPage> _pages = new List<TutorialPage>();
        
        /// <summary>
        /// チュートリアルが表示中かどうか
        /// </summary>
        public bool IsShowing => tutorialPanel != null && tutorialPanel.activeSelf;

        [System.Serializable]
        public class TutorialPage
        {
            public string title;
            [TextArea(3, 5)]
            public string content;
            public Sprite image;
        }

        void Start()
        {
            // デフォルトのチュートリアルページを設定
            SetupDefaultPages();

            // 初回起動チェック
            if (showOnlyOnFirstLaunch && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
            {
                // 既にチュートリアル完了済み
                if (tutorialPanel != null)
                    tutorialPanel.SetActive(false);
                return;
            }

            // チュートリアルを表示
            ShowTutorial();
        }

        void SetupDefaultPages()
        {
            _pages.Clear();

            // 画像があれば取得（なければnull）
            Sprite GetImage(int index) => (tutorialPageImages != null && index < tutorialPageImages.Count) ? tutorialPageImages[index] : null;

            // Load university logo from Resources
            Sprite universityLogo = Resources.Load<Sprite>("UniversityLogo");
            
            _pages.Add(new TutorialPage
            {
                title = "Flashback Memoryへようこそ！",
                content = "このアプリはARグラスで見たものを自動記録し、後から検索できます。\n\nあなたの「記憶」をサポートするアプリです。\n\n— Created by 押見草土 —",
                image = universityLogo != null ? universityLogo : GetImage(0)
            });

            _pages.Add(new TutorialPage
            {
                title = "自動記録機能",
                content = "アプリを起動している間、ARグラスのカメラで見たものを自動的にサーバーに送信し、AIが物体を検出して保存します。\n\n特別な操作は不要です。",
                image = GetImage(1)
            });

            _pages.Add(new TutorialPage
            {
                title = "検索機能の使い方",
                content = "「SEARCH」ボタンをタップすると、カテゴリ選択画面が表示されます。\n\n探したいカテゴリ（例：bottle, laptop）を選択し、「Search」ボタンで検索を実行します。",
                image = GetImage(2)
            });

            _pages.Add(new TutorialPage
            {
                title = "検索結果の表示",
                content = "AR空間に検索結果の画像がカードとして表示されます。\n\n左右にスワイプして過去の記録を閲覧できます。",
                image = GetImage(3)
            });


            _pages.Add(new TutorialPage
            {
                title = "物体登録機能",
                content = "「OBJECT」ボタンで物体認識モードに切り替え。\n\n物体にカメラを向けて名前を入力すると、30枚の画像で特徴を学習します。\n登録した物体は後から認識されます。",
                image = GetImage(5)
            });

            _pages.Add(new TutorialPage
            {
                title = "ビーコン機能",
                content = "登録した物体を認識すると、その場所に📍ビーコンが自動配置されます。\n\n離れた場所からでも物体の位置がわかります。\n設定でオン/オフできます。",
                image = GetImage(6)
            });

            _pages.Add(new TutorialPage
            {
                title = "登録リスト",
                content = "設定パネルの「📋 List」ボタンで、登録済みの物体一覧を確認できます。\n\n「🗑 Beacons」ボタンで配置されたビーコンを全てクリアできます。",
                image = GetImage(7)
            });

            _pages.Add(new TutorialPage
            {
                title = "設定",
                content = "歯車アイコンで設定パネルを開きます。\n\n・AR表示モードの切替\n・入力方式（コントローラー/ハンド）の切替\n・ビーコンのオン/オフ",
                image = GetImage(8)
            });

            _pages.Add(new TutorialPage
            {
                title = "準備完了！",
                content = "以上で基本的な使い方の説明は終了です。\n\nさあ、ARグラスで日常を記録してみましょう！\n\n何か困ったときは設定パネルから確認できます。\n\nCreated by 押見草土",
                image = GetImage(9)
            });
        }

        public void ShowTutorial()
        {
            Debug.Log("[TutorialManager] ShowTutorial called");
            
            if (tutorialPanel == null)
            {
                Debug.LogError("[TutorialManager] tutorialPanel is NULL!");
                return;
            }

            Debug.Log($"[TutorialManager] Showing panel: {tutorialPanel.name}");
            _currentPage = 0;
            tutorialPanel.SetActive(true);

            // アニメーション（DOTweenが動作しない場合に備えてフォールバック）
            tutorialPanel.transform.localScale = Vector3.one; // 即座に表示
            // DOTweenが必要なら以下をコメント解除
            // tutorialPanel.transform.localScale = Vector3.zero;
            // tutorialPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            UpdatePage();

            // ボタンイベント設定
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);
                
            Debug.Log("[TutorialManager] Tutorial panel shown!");
        }

        void UpdatePage()
        {
            if (_currentPage >= _pages.Count) return;

            TutorialPage page = _pages[_currentPage];

            if (titleText != null)
                titleText.text = page.title;

            if (contentText != null)
                contentText.text = page.content;

            if (tutorialImage != null)
            {
                if (page.image != null)
                {
                    tutorialImage.sprite = page.image;
                }
                // 画像がnullの場合は何もしない（デフォルトの背景/アニメーションを表示し続ける）
                // tutorialImage.gameObject.SetActive(false); // ← これがパネル全体を非表示にしていた原因
                tutorialImage.gameObject.SetActive(true); // 常に表示
            }

            // ページインジケーター更新
            if (pageIndicator != null)
                pageIndicator.text = $"{_currentPage + 1} / {_pages.Count}";

            // 最後のページではボタンテキストを変更
            if (nextButton != null)
            {
                Text btnText = nextButton.GetComponentInChildren<Text>();
                if (btnText != null)
                    btnText.text = _currentPage == _pages.Count - 1 ? "完了" : "次へ";
            }
        }

        void OnNextClicked()
        {
            _currentPage++;

            if (_currentPage >= _pages.Count)
            {
                // チュートリアル完了
                CompleteTutorial();
            }
            else
            {
                // 次のページへアニメーション
                if (contentText != null)
                {
                    contentText.DOFade(0, 0.15f).OnComplete(() =>
                    {
                        UpdatePage();
                        contentText.DOFade(1, 0.15f);
                    });
                }
                else
                {
                    UpdatePage();
                }
            }
        }

        void OnSkipClicked()
        {
            CompleteTutorial();
        }

        void CompleteTutorial()
        {
            // 完了フラグを保存
            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();

            // パネルを閉じる
            if (tutorialPanel != null)
            {
                tutorialPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    tutorialPanel.SetActive(false);
                });
            }

            Debug.Log("[Tutorial] Tutorial completed!");
        }

        /// <summary>
        /// チュートリアルをリセット（デバッグ用）
        /// </summary>
        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[Tutorial] Tutorial reset. Will show on next launch.");
        }
    }
}
