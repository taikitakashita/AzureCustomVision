using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceRecognizer : MonoBehaviour {

    /// <summary>
    /// このクラスをシングルトンとしてふるまわせるためのインスタンス
    /// </summary>
    public static VoiceRecognizer Instance;

    /// <summary>
    /// キーワードレコナイザーのオブジェクト
    /// </summary>
    internal KeywordRecognizer keywordRecognizer;

    /// <summary>キーワードとそれに紐づく動作の一覧
    /// </summary>
    private Dictionary<string, Action> _keywords = new Dictionary<string, Action>();

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
    void Start()
    {
        // 登録されたタグの一覧の取得
        Array tagsArray = Enum.GetValues(typeof(CustomVisionTrainer.Tags));

        foreach (object tagWord in tagsArray)
        {
            _keywords.Add(tagWord.ToString(), () =>
            {
                // 登録されたタグがキーワードとして認識されると、Custom Vision Serviceの学習を行うアクションを登録
                CustomVisionTrainer.Instance.VerifyTag(tagWord.ToString());
            });
        }

        _keywords.Add("キャンセル", () =>
        {
            // キャンセルというキーワードが認識されると、キャプチャした写真をリセットし、キーワードレコナイザーを停止するアクションを登録
            ImageCapture.Instance.ResetImageCapture();
            keywordRecognizer.Stop();
        });

        // キーワードレコナイザーの生成
        keywordRecognizer = new KeywordRecognizer(_keywords.Keys.ToArray());

        // キーワードを認識したときのイベントハンドラを登録
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
    }

    /// <summary>
    /// キーワードが認識されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Action keywordAction;
        // 認識されたキーワードが一覧に存在する場合に、そのアクションを呼び出す。
        if (_keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }
}
