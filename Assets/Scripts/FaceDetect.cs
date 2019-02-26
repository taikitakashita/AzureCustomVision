using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FaceDetect : MonoBehaviour
{
    /// <summary>
    /// このクラスをシングルトンのようにふるまわせるためのインスタンス
    /// </summary>
    public static FaceDetect Instance;

    /// <summary>
    /// カメラで撮影した画像を送信するためのバイト配列
    /// </summary>
    internal byte[] imageBytes;

    /// <summary>
    /// Azure顔認識APIのエンドポイントのURL
    /// </summary>
    const string baseEndpoint = "https://westus.api.cognitive.microsoft.com/face/v1.0/";

    /// <summary>
    /// Azure顔認識APIの認証キー
    /// </summary>
    private const string key = "7d98494d93724485ba171bf303625f6f";

    /// <summary>
    /// パーソングループの名称
    /// </summary>
    private const string personGroupId = "qbslab";

    /// <summary>
    /// 撮影した写真に顔が存在するか判定するフラグ
    /// </summary>
    internal bool faceincaputure = false;

    /// <summary>
    /// Initialises this class
    /// </summary>
    private void Awake()
    {
        // このクラスをシングルトンのようにふるまわせる
        Instance = this;
    }

    /// <summary>
    /// 撮影した画像から顔を検出
    /// </summary>
    public IEnumerator DetectFacesFromImage(string imagePath)
    {
        Debug.Log("***********0");
        // WWWを利用してサーバにデータをポストするフォームを生成するヘルパークラス
        WWWForm webForm = new WWWForm();

        // 顔を検出するための顔認識APIのエンドポイントのURLの設定
        string detectFacesEndpoint = $"{baseEndpoint}detect";

        Debug.Log("***********1");

        // 撮影した画像のバイト配列を取得
        imageBytes = GetImageAsByteArray(imagePath);

        using (UnityWebRequest www =
            UnityWebRequest.Post(detectFacesEndpoint, webForm))
        {
            // HTTPヘッダを設定
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.SetRequestHeader("Content-Type", "application/octet-stream");

            // アップロードするデータを格納するハンドラの設定
            www.uploadHandler.contentType = "application/octet-stream";
            www.uploadHandler = new UploadHandlerRaw(imageBytes);

            // 受信したデータを格納するハンドラの初期化
            www.downloadHandler = new DownloadHandlerBuffer();

            // 送受信を開始し、完了するまで待つ
            yield return www.SendWebRequest();

            Debug.Log("***********2");

            // ダウンロードした内容を文字列として取得
            string jsonResponse = www.downloadHandler.text;

            Debug.Log("***********3");

            // 取得したJSON文字列を逆シリアライズ化
            Face_RootObject[] face_RootObject =
                JsonConvert.DeserializeObject<Face_RootObject[]>(jsonResponse);

            Debug.Log("***********4");

            // 取得した顔のIDを格納するリストの初期化
            List<string> facesIdList = new List<string>();

            if (face_RootObject.Length > 0)
            {
                faceincaputure = true;
                Debug.Log("*************顔を検出しました。");
            }
            else
            {
                faceincaputure = false;
                Debug.Log("*************顔を検出しませんでした");
            }

            // 取得した顔のIDの分だけ繰り返し処理
            foreach (Face_RootObject faceRO in face_RootObject)
            {
                // リストに顔のIDを登録
                facesIdList.Add(faceRO.faceId);
                Debug.Log($"検出した顔の情報:ID {faceRO.faceId},Top {faceRO.FaceRectangle.top},Left {faceRO.FaceRectangle.left},Width {faceRO.FaceRectangle.width},Height {faceRO.FaceRectangle.height}");
            }

            // 撮影した画像中に人の顔がある場合は解析を開始する。
            if (faceincaputure)
            {
                Debug.Log("*************顔を検出したため、解析処理を開始");
                StartCoroutine(CustomVisionAnalyser.Instance.AnalyseLastImageCaptured(imagePath));
            }
            // 撮影した画像中に人の顔がない場合はその旨をテキスト表示する。
            else
            {
                Debug.Log("*************顔を検出しなかったため、メッセージを表示");
                SceneOrganiser.Instance.SetNoFaceToLastLabel();
                ImageCapture.Instance.ResetImageCapture();
            }
        }
    }

    /// <summary>
    /// 画像をバイト配列に変換するメソッド
    /// </summary>
    static byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        return binaryReader.ReadBytes((int)fileStream.Length);
    }
}

/// <summary>
/// 人の顔のIDを格納するクラス
/// </summary>
public class Face_RootObject
{
    public string faceId { get; set; }
    public FaceRectangle FaceRectangle { get; set; }
}

public class FaceRectangle
{
    public int top { get; set; }
    public int left { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}