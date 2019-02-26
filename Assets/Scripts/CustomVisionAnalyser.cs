using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class CustomVisionAnalyser : MonoBehaviour {

    /// <summary>
    /// このクラスをシングルトンとしてふるまわせるためのインスタンス
    /// </summary>
    public static CustomVisionAnalyser Instance;

    /// <summary>
    /// 解析用のサブスクリプションキーの設定
    /// </summary>
    private string predictionKey = "44ccb45be04b4c61abeee91ba3917623";

    /// <summary>
    /// Custom Vision Serviceの解析用のエンドポイントのURLの設定
    /// </summary>
    private string predictionEndpoint = "https://japaneast.api.cognitive.microsoft.com/customvision/v2.0/Prediction/2880926f-ee4b-40d5-a025-4c62aff9aac2/image";
    //private string predictionEndpoint = "http://192.168.101.109/DllPredictService/predict";

    /// <summary>
    /// 撮影した画像を送信する際のバイト配列
    /// </summary>
    [HideInInspector] public byte[] imageBytes;

    /// <summary>
    /// Initialises this class
    /// </summary>
    private void Awake()
    {
        // このクラスのインスタンスをシングルトンとして利用する。
        Instance = this;
    }

    /// <summary>
    /// Custom Vision Serviceに画像を送信し、タグ名と信頼度を取得し、解析結果表示用のテキストに記載する。
    /// </summary>
    public IEnumerator AnalyseLastImageCaptured(string imagePath)
    {
        WWWForm webForm = new WWWForm();
        using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(predictionEndpoint, webForm))
        {
            Debug.Log("*******1");
            // Gets a byte array out of the saved image
            imageBytes = GetImageAsByteArray(imagePath);

            unityWebRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            unityWebRequest.SetRequestHeader("Prediction-Key", predictionKey);
            Debug.Log("*******2");
            // The upload handler will help uploading the byte array with the request
            unityWebRequest.uploadHandler = new UploadHandlerRaw(imageBytes);
            unityWebRequest.uploadHandler.contentType = "application/octet-stream";
            Debug.Log("*******3");
            // The download handler will help receiving the analysis from Azure
            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            Debug.Log("*******4");
            // Send the request
            yield return unityWebRequest.SendWebRequest();
            Debug.Log("*******5");
            string jsonResponse = unityWebRequest.downloadHandler.text;
            Debug.Log(jsonResponse);
            // The response will be in JSON format, therefore it needs to be deserialized    

            // The following lines refers to a class that you will build in later Chapters
            // Wait until then to uncomment these lines

            AnalysisObject analysisObject = new AnalysisObject();
            analysisObject = JsonConvert.DeserializeObject<AnalysisObject>(jsonResponse);

            Debug.Log("*******6");
            SceneOrganiser.Instance.SetTagsToLastLabel(analysisObject);
            Debug.Log("*******7");
            ImageCapture.Instance.ResetImageCapture();
        }
    }

    /// <summary>
    /// 撮影した画像の内容をバイト配列として返す。
    /// </summary>
    static byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);

        BinaryReader binaryReader = new BinaryReader(fileStream);

        return binaryReader.ReadBytes((int)fileStream.Length);
    }
}
