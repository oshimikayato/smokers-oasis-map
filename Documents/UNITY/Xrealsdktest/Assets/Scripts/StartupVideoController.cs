using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.IO;

public class StartupVideoController : MonoBehaviour
{
    public RawImage displayImage;
    public VideoPlayer videoPlayer;
    public CanvasGroup canvasGroup;
    public GameObject mainUiRoot; // The UI to show after video finishes

    private bool _isSkipping = false;

    void Start()
    {
        // Setup Video Player
        if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.isLooping = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        
        // Setup Audio
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audioSource);

        // Path
        string videoPath = Path.Combine(Application.streamingAssetsPath, "StartupVideo.mp4");
        Debug.Log($"[StartupVideo] Loading video from: {videoPath}");

        if (File.Exists(videoPath))
        {
            videoPlayer.url = videoPath;
            videoPlayer.prepareCompleted += OnPrepareCompleted;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
            
            // Hide main UI initially if provided
            if (mainUiRoot != null) mainUiRoot.SetActive(false);
            
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogError($"[StartupVideo] File not found at {videoPath}");
            EndStartupSequence(); // Fallback
        }
    }

    void Update()
    {
        // Skip on touch/click
        if (!_isSkipping && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            Debug.Log("[StartupVideo] Skipped by user");
            _isSkipping = true;
            EndStartupSequence();
        }
    }

    void OnPrepareCompleted(VideoPlayer source)
    {
        Debug.Log("[StartupVideo] Prepared. Playing...");
        
        // Setup Texture
        displayImage.texture = source.texture;
        displayImage.color = Color.white;
        
        source.Play();
    }

    void OnVideoFinished(VideoPlayer source)
    {
        Debug.Log("[StartupVideo] Finished.");
        EndStartupSequence();
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[StartupVideo] Error: {message}");
        EndStartupSequence();
    }

    void EndStartupSequence()
    {
        StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        if (canvasGroup != null)
        {
            float duration = 0.5f;
            float startAlpha = canvasGroup.alpha;
            float t = 0;
            
            while (t < duration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
                yield return null;
            }
        }

        // Show Main UI
        if (mainUiRoot != null) mainUiRoot.SetActive(true);

        Debug.Log("[StartupVideo] Sequence complete. Destroying controller.");
        Destroy(gameObject);
    }
}
