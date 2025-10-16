using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// YOLO判定処理を管理する静的クラス
/// どこからでも呼び出し可能
/// </summary>
public static class YOLOJudgmentManager
{
    /// <summary>
    /// YOLO判定を実行するコルーチン
    /// </summary>
    /// <param name="runYOLO">YOLO推論を行うオブジェクト</param>
    /// <param name="overlayImage">人外時に表示するオーバーレイ画像</param>
    /// <param name="seSoundManager">SE再生用AudioSource</param>
    /// <param name="apperClip">人外時のSE</param>
    /// <param name="onComplete">処理完了時のコールバック</param>
    /// <returns>コルーチン</returns>
    public static IEnumerator ExecuteYOLOJudgment(
        RunYOLO runYOLO,
        Image overlayImage = null,
        AudioSource seSoundManager = null,
        AudioClip apperClip = null,
        Action onComplete = null)
    {
        // 1秒待ってから実行
        yield return new WaitForSeconds(1f);

        // YOLOの推論を開始
        if (runYOLO != null)
            runYOLO.StartYOLO();

        // ステップ１：BoxesFound = ボックス検出数が1以上になるまで待機
        while (runYOLO != null && runYOLO.BoxesFound < 1)
        {
            Debug.Log("まず人として検出されてください");
            yield return null;
        }

        // ステップ２：一定時間連続でBoxesFoundが0なら人外と判定
        float zeroTime = 0f;
        float requiredZeroDuration = 0.1f; // 0.1秒間連続で0ならOK（人外判定）
        while (zeroTime < requiredZeroDuration)
        {
            if (runYOLO != null && runYOLO.BoxesFound == 0)
            {
                zeroTime += Time.deltaTime;
                Debug.Log($"人外判定までの時間: {zeroTime:F2}秒");
            }
            else
            {
                zeroTime = 0f;
            }
            yield return null;
        }

        Debug.Log("人外になれました！（一定時間連続で0検出）");
        
        // オーバーレイ表示
        if (overlayImage != null) 
            overlayImage.gameObject.SetActive(true);
        
        // SE再生
        if (seSoundManager != null && apperClip != null)
        {
            seSoundManager.clip = apperClip;
            seSoundManager.Play();
        }

        // 推論停止
        if (runYOLO != null) 
            runYOLO.StopYOLO();
        
        Debug.Log("YOLO判定処理完了");

        // 完了コールバック実行
        onComplete?.Invoke();
    }

    /// <summary>
    /// 簡易版：基本的なYOLO判定のみ
    /// </summary>
    /// <param name="runYOLO">YOLO推論を行うオブジェクト</param>
    /// <param name="onComplete">処理完了時のコールバック</param>
    /// <returns>コルーチン</returns>
    public static IEnumerator ExecuteSimpleYOLOJudgment(RunYOLO runYOLO, Action onComplete = null)
    {
        return ExecuteYOLOJudgment(runYOLO, null, null, null, onComplete);
    }
}