using UnityEngine;

public class SceneOrganiser : MonoBehaviour {

    /// <summary>
    /// このクラスをシングルトンとしてふるまわせるためのインスタンス
    /// </summary>
    public static SceneOrganiser Instance;

    /// <summary>
    /// カーソルのオブジェクト
    /// </summary>
    internal GameObject cursor;

    /// <summary>
    /// 解析結果を表示するラベル
    /// </summary>
    internal GameObject label;

    /// <summary>
    /// カメラの現在のステータスを表示するオブジェクト
    /// </summary>
    internal TextMesh cameraStatusIndicator;

    /// <summary>
    /// 解析結果を表示するラベル位置
    /// </summary>
    internal Transform lastLabelPlaced;

    /// <summary>
    /// 解析結果を表示するラベルのテキスト
    /// </summary>
    internal TextMesh lastLabelPlacedText;

    /// <summary>
    /// 解析結果の閾値
    /// </summary>
    internal float probabilityThreshold = 0.5f;

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        // このクラスのインスタンスをシングルトンとして利用する。
        Instance = this;

        // このゲームオブジェクトにImageCaptureクラスを追加
        gameObject.AddComponent<ImageCapture>();

        // このゲームオブジェクトにCustomVisionAnalyserクラスを追加
        gameObject.AddComponent<CustomVisionAnalyser>();

        // このゲームオブジェクトにCustomVisionTrainerクラスを追加
        gameObject.AddComponent<CustomVisionTrainer>();

        // このゲームオブジェクトにVoiceRecognizerクラスを追加
        gameObject.AddComponent<VoiceRecognizer>();

        // このゲームオブジェクトにCustomVisionObjectsクラスを追加
        gameObject.AddComponent<CustomVisionObjects>();

        // このゲームオブジェクトにCustomVisionObjectsクラスを追加
        gameObject.AddComponent<FaceDetect>();

        // カメラのカーソルを作成する。
        cursor = CreateCameraCursor();

        // 解析結果を表示するラベルを作成する。
        label = CreateLabel();

        // カメラの現在のステータスを表示するテキストを作成します。
        cameraStatusIndicator = CreateTrainingUI("Status Indicator", 0.02f, 0.2f, 3, true);

        // カメラの現在のステータスを読み込み中にする。
        SetCameraStatus("読み込み中です");
    }

    /// <summary>
    /// カメラのカーソルを作成する。
    /// </summary>
    private GameObject CreateCameraCursor()
    {
        // カーソルとして利用するスフィアオブジェクトを作成する。
        GameObject newCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // 作成したスフィアオブジェクトをカメラオブジェクトの子にする。
        newCursor.transform.parent = gameObject.transform;

        // カーソルのサイズを設定する。
        newCursor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

        // カーソルの位置を設定する。
        newCursor.transform.localPosition = new Vector3(0, 0, 4);

        // カーソルのマテリアルを設定し、色を緑に設定する。
        newCursor.GetComponent<Renderer>().material = new Material(Shader.Find("Diffuse"));
        newCursor.GetComponent<Renderer>().material.color = Color.green;

        return newCursor;
    }

    /// <summary>
    /// 解析結果を表示するラベルを作成する。
    /// </summary>
    private GameObject CreateLabel()
    {
        // ラベル用の新しいゲームオブジェクトを作成する。
        GameObject newLabel = new GameObject();

        // ラベルのサイズを設定する。
        newLabel.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // ラベルのテキストの設定を行う。
        TextMesh t = newLabel.AddComponent<TextMesh>();
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.fontSize = 50;
        t.text = "";

        return newLabel;
    }

    /// <summary>
    /// カメラのステータスの文字列の表示と文字の色を設定する。
    /// </summary>
    /// <param name="statusText">Input string</param>
    public void SetCameraStatus(string statusText)
    {
        if (string.IsNullOrEmpty(statusText) == false)
        {
            string message = "white";

            switch (statusText.ToLower())
            {
                case "読み込み中です":
                    message = "yellow";
                    break;

                case "準備が完了しました":
                    message = "green";
                    break;

                case "画像をアップロードしています":
                    message = "red";
                    break;

                case "保存中です":
                    message = "yellow";
                    break;

                case "解析中です":
                    message = "red";
                    break;
            }

            cameraStatusIndicator.GetComponent<TextMesh>().text = $"カメラの状態:\n<color={message}>{statusText}...</color>";
        }
    }

    /// <summary>
    /// 解析結果のラベルを作成し、ラベルのテキストを取得する。
    /// </summary>
    public void PlaceAnalysisLabel()
    {
        lastLabelPlaced = Instantiate(label.transform, cursor.transform.position, transform.rotation);
        lastLabelPlacedText = lastLabelPlaced.GetComponent<TextMesh>();
    }

    /// <summary>
    /// 解析結果のデータをラベルに記載する。
    /// </summary>
    public void SetTagsToLastLabel(AnalysisObject analysisObject)
    {
        lastLabelPlacedText = lastLabelPlaced.GetComponent<TextMesh>();

        if (analysisObject.Predictions != null)
        {
            foreach (Prediction p in analysisObject.Predictions)
            {
                if (p.Probability > 0.02)
                {
                    lastLabelPlacedText.text += $"検出結果: {p.TagName} {p.Probability.ToString("0.00 \n")}";
                    Debug.Log($"Detected: {p.TagName} {p.Probability.ToString("0.00 \n")}");
                }
            }
        }
    }

    /// <summary>
    /// 写真中に顔が存在しないことをラベルに記載する
    /// </summary>
    public void SetNoFaceToLastLabel()
    {
        lastLabelPlacedText = lastLabelPlaced.GetComponent<TextMesh>();

        lastLabelPlacedText.text += $"撮影した範囲に人の顔が存在しません。";
        Debug.Log($"撮影した範囲に人の顔が存在しません。");
    }

    /// <summary>
    /// 現在のステータスを表示するテキストを作成します。
    /// </summary>
    /// <param name="name">name of object</param>
    /// <param name="scale">scale of object (i.e. 0.04f)</param>
    /// <param name="yPos">height above the cursor (i.e. 0.3f</param>
    /// <param name="zPos">distance from the camera</param>
    /// <param name="setActive">whether the text mesh should be visible when it has been created</param>
    /// <returns>Returns a 3D text mesh within the scene</returns>
    internal TextMesh CreateTrainingUI(string name, float scale, float yPos, float zPos, bool setActive)
    {
        GameObject display = new GameObject(name, typeof(TextMesh));
        display.transform.parent = Camera.main.transform;
        display.transform.localPosition = new Vector3(0, yPos, zPos);
        display.SetActive(setActive);
        display.transform.localScale = new Vector3(scale, scale, scale);
        display.transform.rotation = new Quaternion();
        TextMesh textMesh = display.GetComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        return textMesh;
    }
}
