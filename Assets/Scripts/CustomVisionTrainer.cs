using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CustomVisionTrainer : MonoBehaviour {

    /// <summary>
    /// このクラスをシングルトンとしてふるまわせるためのインスタンス
    /// </summary>
    public static CustomVisionTrainer Instance;

    /// <summary>
    /// Custom Vision Serviceの学習用のエンドポイントのURLの設定
    /// </summary>
    private string url = "https://japaneast.api.cognitive.microsoft.com/customvision/v2.2/Training/projects/";

    /// <summary>
    /// 学習用のサブスクリプションキーの設定
    /// </summary>
    private string trainingKey = "83eed7ba23234464972635d8a6269105";

    /// <summary>
    /// Custom Vision ServiceのプロジェクトIDを設定
    /// </summary>
    private string projectId = "2880926f-ee4b-40d5-a025-4c62aff9aac2";

    /// <summary>
    /// 撮影した画像を送信する際のバイト配列
    /// </summary>
    internal byte[] imageBytes;

    /// <summary>
    /// タグの登録
    /// </summary>
    internal enum Tags { ヘルメット着用, ヘルメット未着用 }

    /// <summary>
    /// 学習の際の処理のプロセス内容を表示するテキスト
    /// </summary>
    private TextMesh trainingUI_TextMesh;

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        // このクラスのインスタンスをシングルトンとして利用する。
        Instance = this;
    }

    /// <summary>
    /// Runs at initialization right after Awake method
    /// </summary>
    private void Start()
    {
        // 学習の現在のステータスを表示するテキストを作成します。
        trainingUI_TextMesh = SceneOrganiser.Instance.CreateTrainingUI("TrainingUI", 0.04f, 0, 4, false);
    }

    // 学習の現在のステータスを表示し、キーワードレコナイザーを起動する。
    internal void RequestTagSelection()
    {
        trainingUI_TextMesh.gameObject.SetActive(true);
        trainingUI_TextMesh.text = $" \n以下のタグから音声で選択してください。: \nマウス\nキーボード \nもしくはキャンセル";

        VoiceRecognizer.Instance.keywordRecognizer.Start();
    }

    /// <summary>
    /// 登録されたタグがキーワードとして認識されると、Custom Vision Serviceの学習を開始
    /// </summary>
    internal void VerifyTag(string spokenTag)
    {
        if (spokenTag == Tags.ヘルメット着用.ToString() || spokenTag == Tags.ヘルメット未着用.ToString())
        {
            trainingUI_TextMesh.text = $"選択したタグ: {spokenTag}";
            VoiceRecognizer.Instance.keywordRecognizer.Stop();
            StartCoroutine(SubmitImageForTraining(ImageCapture.Instance.filePath, spokenTag));
        }
    }

    /// <summary>
    /// Custom Vision Serviceに画像を送信する。
    /// </summary>
    public IEnumerator SubmitImageForTraining(string imagePath, string tag)
    {
        yield return new WaitForSeconds(2);
        trainingUI_TextMesh.text = $"{tag} \nの画像をCustom Vision Serviceに送信します。";
        string imageId = string.Empty;
        string tagId = string.Empty;

        // Retrieving the Tag Id relative to the voice input
        string getTagIdEndpoint = string.Format("{0}{1}/tags", url, projectId);
        using (UnityWebRequest www = UnityWebRequest.Get(getTagIdEndpoint))
        {
            www.SetRequestHeader("Training-Key", trainingKey);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;
            Debug.Log("***" + jsonResponse);

            Tags_RootObject tagRootObject = new Tags_RootObject();
            tagRootObject.Tags = JsonConvert.DeserializeObject<List<TagOfProject>>(jsonResponse);

            foreach (TagOfProject tOP in tagRootObject.Tags)
            {
                if (tOP.Name == tag)
                {
                    tagId = tOP.Id;
                }
            }
        }
        
        // Creating the image object to send for training
        List<IMultipartFormSection> multipartList = new List<IMultipartFormSection>();
        MultipartObject multipartObject = new MultipartObject();
        multipartObject.contentType = "application/octet-stream";
        multipartObject.fileName = "";
        multipartObject.sectionData = GetImageAsByteArray(imagePath);
        multipartList.Add(multipartObject);

        string createImageFromDataEndpoint = string.Format("{0}{1}/images?tagIds={2}", url, projectId, tagId);

        using (UnityWebRequest www = UnityWebRequest.Post(createImageFromDataEndpoint, multipartList))
        {
            // Gets a byte array out of the saved image
            imageBytes = GetImageAsByteArray(imagePath);

            //unityWebRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            www.SetRequestHeader("Training-Key", trainingKey);

            // The upload handler will help uploading the byte array with the request
            www.uploadHandler = new UploadHandlerRaw(imageBytes);

            // The download handler will help receiving the analysis from Azure
            www.downloadHandler = new DownloadHandlerBuffer();

            // Send the request
            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;

            ImageRootObject m = JsonConvert.DeserializeObject<ImageRootObject>(jsonResponse);
            imageId = m.Images[0].Image.Id;
        }

        trainingUI_TextMesh.text = "画像をCustom Vision Serviceに送信しました。";
        StartCoroutine(TrainCustomVisionProject());
    }

    /// <summary>
    /// Custom Vision Serviceの分析モデルの学習を行う。
    /// </summary>
    public IEnumerator TrainCustomVisionProject()
    {
        yield return new WaitForSeconds(2);

        trainingUI_TextMesh.text = "Custom Vision Serviceの学習中です。";

        WWWForm webForm = new WWWForm();

        string trainProjectEndpoint = string.Format("{0}{1}/train", url, projectId);

        using (UnityWebRequest www = UnityWebRequest.Post(trainProjectEndpoint, webForm))
        {
            www.SetRequestHeader("Training-Key", trainingKey);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            string jsonResponse = www.downloadHandler.text;
            Debug.Log($"Training - JSON Response: {jsonResponse}");

            // A new iteration that has just been created and trained
            Iteration iteration = new Iteration();
            iteration = JsonConvert.DeserializeObject<Iteration>(jsonResponse);

            if (www.isDone)
            {
                trainingUI_TextMesh.text = "Custom Vision Serviceの学習が終わりました。";

                // Since the Service has a limited number of iterations available,
                // we need to set the last trained iteration as default
                // and delete all the iterations you dont need anymore
                StartCoroutine(SetDefaultIteration(iteration));
            }
        }
    }

    /// <summary>
    /// 作成した学習モデルをデフォルト利用に設定
    /// </summary>
    private IEnumerator SetDefaultIteration(Iteration iteration)
    {
        yield return new WaitForSeconds(5);
        trainingUI_TextMesh.text = "既定の学習済みモデルを設定しています。";

        // Set the last trained iteration to default
        iteration.IsDefault = true;

        // Convert the iteration object as JSON
        string iterationAsJson = JsonConvert.SerializeObject(iteration);
        byte[] bytes = Encoding.UTF8.GetBytes(iterationAsJson);

        string setDefaultIterationEndpoint = string.Format("{0}{1}/iterations/{2}",
                                                        url, projectId, iteration.Id);

        using (UnityWebRequest www = UnityWebRequest.Put(setDefaultIterationEndpoint, bytes))
        {
            www.method = "PATCH";
            www.SetRequestHeader("Training-Key", trainingKey);
            www.SetRequestHeader("Content-Type", "application/json");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;

            if (www.isDone)
            {
                trainingUI_TextMesh.text = "既定の学習済みモデルの設定が完了しました。 \n利用していない学習モデルを削除します。";
                StartCoroutine(DeletePreviousIteration(iteration));
            }
        }
    }

    /// <summary>
    /// デフォルト利用出ない不要な学習モデルを削除
    /// </summary>
    public IEnumerator DeletePreviousIteration(Iteration iteration)
    {
        yield return new WaitForSeconds(5);

        trainingUI_TextMesh.text = "利用していない学習モデルを削除します。";

        string iterationToDeleteId = string.Empty;

        string findAllIterationsEndpoint = string.Format("{0}{1}/iterations", url, projectId);

        using (UnityWebRequest www = UnityWebRequest.Get(findAllIterationsEndpoint))
        {
            www.SetRequestHeader("Training-Key", trainingKey);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;

            // The iteration that has just been trained
            List<Iteration> iterationsList = new List<Iteration>();
            iterationsList = JsonConvert.DeserializeObject<List<Iteration>>(jsonResponse);

            foreach (Iteration i in iterationsList)
            {
                if (i.IsDefault != true)
                {
                    Debug.Log($"Cleaning - Deleting iteration: {i.Name}, {i.Id}");
                    iterationToDeleteId = i.Id;
                    break;
                }
            }
        }

        string deleteEndpoint = string.Format("{0}{1}/iterations/{2}", url, projectId, iterationToDeleteId);

        using (UnityWebRequest www2 = UnityWebRequest.Delete(deleteEndpoint))
        {
            www2.SetRequestHeader("Training-Key", trainingKey);
            www2.downloadHandler = new DownloadHandlerBuffer();
            yield return www2.SendWebRequest();
            string jsonResponse = www2.downloadHandler.text;

            trainingUI_TextMesh.text = "利用していない学習モデルを削除しました。";
            yield return new WaitForSeconds(2);
            trainingUI_TextMesh.text = "準備が完了しました。";

            yield return new WaitForSeconds(2);
            trainingUI_TextMesh.text = "";
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
