using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using UnityEngine.XR.WSA.WebCam;

public class ImageCapture : MonoBehaviour {

    /// <summary>
    /// このクラスをシングルトンとしてふるまわせるためのインスタンス
    /// </summary>
    public static ImageCapture Instance;

    /// <summary>
    /// 画像の名前の設定で利用するため、タップ数をカウントする。
    /// </summary>
    private int captureCount = 0;

    /// <summary>
    /// 画像用のオブジェクト
    /// </summary>
    private PhotoCapture photoCaptureObject = null;

    /// <summary>
    /// ジェスチャーレコナイザーのオブジェクト
    /// </summary>
    private GestureRecognizer recognizer;

    /// <summary>
    /// 解析の処理の感覚
    /// </summary>
    private float secondsBetweenCaptures = 10f;

    /// <summary>
    /// アプリケーションのモード
    /// </summary>
    internal enum AppModes { Analysis, Training }

    /// <summary>
    /// 現在のアプリケーションのモードのプロパティ
    /// </summary>
    internal AppModes AppMode { get; private set; }

    /// <summary>
    /// 写真の撮影中かを判定するフラグ
    /// </summary>
    internal bool captureIsActive;

    /// <summary>
    /// 現在解析中の写真のパス
    /// </summary>
    internal string filePath = string.Empty;

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        Instance = this;

        // 解析モードと学習モードを切り替えるフラグの設定
        AppMode = AppModes.Analysis;
    }

    /// <summary>
    /// Runs at initialization right after Awake method
    /// </summary>
    void Start()
    {
        // このアプリケーションで保存されているすべての写真をクリーンアップ
        DirectoryInfo info = new DirectoryInfo(Application.persistentDataPath);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                Debug.LogFormat("Cannot delete file: ", file.Name);
            }
        }

        // タップ動作を検知するためのジェスチャーレコナイザーの設定と起動
        recognizer = new GestureRecognizer();
        recognizer.SetRecognizableGestures(GestureSettings.Tap);
        recognizer.Tapped += TapHandler;
        recognizer.StartCapturingGestures();

        SceneOrganiser.Instance.SetCameraStatus("準備が完了しました");
    }

    /// <summary>
    /// タップ入力時の処理
    /// </summary>
    private void TapHandler(TappedEventArgs obj)
    {
        switch (AppMode)
        {
            // 解析モードの際の処理
            case AppModes.Analysis:
                // 撮影中のフラグがオフの場合の処理
                if (!captureIsActive)
                {
                    // 撮影中のフラグをオンにする。
                    captureIsActive = true;

                    // カーソルを赤色に変更する。
                    SceneOrganiser.Instance.cursor.GetComponent<Renderer>().material.color = Color.red;

                    // カメラのステータスを保存中ですにする。
                    SceneOrganiser.Instance.SetCameraStatus("保存中です");

                    // ExecuteImageCaptureAndAnalysisメソッドを呼び出し、secondsBetweenCapturesの間隔でリピートする。
                    // InvokeRepeating("ExecuteImageCaptureAndAnalysis", 0, secondsBetweenCaptures);

                    // ExecuteImageCaptureAndAnalysisメソッドを呼び出す。
                    ExecuteImageCaptureAndAnalysis();
                }
                // 撮影中のフラグがオンの場合の処理
                else
                {
                    // 解析プロセスを停止する。
                    // ResetImageCapture();
                }
                break;

            // 学習モードの際の処理
            case AppModes.Training:
                // 撮影中のフラグがオフの場合の処理
                if (!captureIsActive)
                {
                    // 撮影中のフラグをオンにする。
                    captureIsActive = true;

                    // ExecuteImageCaptureAndAnalysisメソッドを呼び出す。
                    ExecuteImageCaptureAndAnalysis();

                    // カーソルを赤色に変更する。
                    SceneOrganiser.Instance.cursor.GetComponent<Renderer>().material.color = Color.red;

                    // カメラのステータスをアップロード中にする。
                    SceneOrganiser.Instance.SetCameraStatus("画像をアップロードしています");
                }
                break;
        }
    }

    /// <summary>
    /// 画像キャプチャのプロセスを開始し、Azure Custom Vision Serviceに送信する。
    /// </summary>
    private void ExecuteImageCaptureAndAnalysis()
    {
        // カメラのステータスを解析中にする。
        SceneOrganiser.Instance.SetCameraStatus("解析中です");

        // 解析結果のラベルを作成し、ラベルのテキストを取得する。
        SceneOrganiser.Instance.PlaceAnalysisLabel();

        // カメラの解像度を可能な限り高く設定
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        // カメラの解像度のサイズのテクスチャの設定
        Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
        Debug.Log(cameraResolution.width + "+++++++++++" + cameraResolution.height);
        // 撮影プロセスを開始し、画像のフォーマットを設定する。
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;

            CameraParameters camParameters = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = targetTexture.width,
                cameraResolutionHeight = targetTexture.height,
                pixelFormat = CapturePixelFormat.BGRA32
            };

            // 画像を撮影して、アプリケーションの内部フォルダに保存する。
            captureObject.StartPhotoModeAsync(camParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                Debug.Log(result.success);
                string filename = string.Format(@"CapturedImage{0}.jpg", captureCount);
                filePath = Path.Combine(Application.persistentDataPath, filename);
                captureCount++;
                Debug.Log("撮影処理開始");
                Debug.Log(filePath);
                photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
            });
        });
    }

    /// <summary>
    /// 撮影が完了したら、撮影モードをストップする。
    /// </summary>
    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        Debug.Log("撮影終了処理開始");
        // Call StopPhotoMode once the image has successfully captured
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }


    /// <summary>
    ///撮影モードを停止したあと、画像解析プロセスを開始する。
    /// </summary>
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        Debug.LogFormat("撮影終了");

        // Dispose from the object in memory and request the image analysis 
        photoCaptureObject.Dispose();
        photoCaptureObject = null;

        switch (AppMode)
        {
            case AppModes.Analysis:
                Debug.Log("*************顔を検出しているか判定を開始");

                StartCoroutine(FaceDetect.Instance.DetectFacesFromImage(filePath));

                break;

            case AppModes.Training:
                // Call training using captured image
                CustomVisionTrainer.Instance.RequestTagSelection();
                break;
        }
    }

    /// <summary>
    /// 保留中のアクションを停止する。
    /// </summary>
    internal void ResetImageCapture()
    {
        captureIsActive = false;

        // カーソルを緑色にする。
        SceneOrganiser.Instance.cursor.GetComponent<Renderer>().material.color = Color.green;

        // カメラのステータスを準備完了にする。
        SceneOrganiser.Instance.SetCameraStatus("準備が完了しました");

        // 保留中のアクションを停止する。
        CancelInvoke();
    }
}
