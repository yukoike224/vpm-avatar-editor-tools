using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace YuKoike.Tools
{
    public class GameObjectComponentCopier : EditorWindow
{
    private GameObject sourceRoot;
    private List<GameObject> targetRoots = new List<GameObject>();
    private bool copyTransform = true;
    private bool copyScaleAdjuster = true;
    private bool overwriteExistingComponents = true;
    private bool allowRootNameMismatch = false;
    private bool autoSearchArmature = true;
    private bool allowPartialMatch = true;
    private Vector2 scrollPosition;
    private List<BatchResult> batchResults = new List<BatchResult>();
    private bool showResults = false;

    private class BatchResult
    {
        public GameObject target;
        public bool success;
        public int matchedObjects;
        public int copiedComponents;
        public string message;
    }

    [MenuItem("Tools/Koike's Utils/GameObject Component Copier")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameObjectComponentCopier>("Component Copier");
        window.minSize = new Vector2(450, 400);
    }

    private void OnGUI()
    {
        GUILayout.Label("GameObject Component Copier", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("このツールは1つのソースから複数のターゲットに対して一括でコンポーネントをコピーします。\n" +
            "VRChat Avatarの素体のArmatureから複数の衣装プレファブへの一括コピーに最適です。", MessageType.Info);
        EditorGUILayout.Space();

        // ソース設定
        GUILayout.Label("ソース設定", EditorStyles.boldLabel);
        sourceRoot = EditorGUILayout.ObjectField("ソースのルートオブジェクト:", sourceRoot, typeof(GameObject), true) as GameObject;

        // シーンから選択されたオブジェクトをソースに設定するボタン
        if (GUILayout.Button("シーンの選択オブジェクトをソースに設定"))
        {
            SetSelectedObjectAsSource();
        }

        EditorGUILayout.Space();

        // ターゲット設定
        GUILayout.Label("ターゲット設定", EditorStyles.boldLabel);
        
        // シーンから選択されたオブジェクトを追加するボタン
        if (GUILayout.Button("シーンの選択オブジェクトを追加"))
        {
            AddSelectedObjectsAsTargets();
        }

        // ターゲットリスト表示
        if (targetRoots.Count > 0)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));
            
            for (int i = 0; i < targetRoots.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                targetRoots[i] = EditorGUILayout.ObjectField($"ターゲット {i + 1}:", targetRoots[i], typeof(GameObject), true) as GameObject;
                
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    targetRoots.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("全て削除", GUILayout.Width(100)))
            {
                targetRoots.Clear();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("ターゲットオブジェクトがありません。シーンで衣装プレファブを選択して「シーンの選択オブジェクトを追加」ボタンを押してください。", MessageType.Warning);
        }

        // 手動追加ボタン
        if (GUILayout.Button("手動でターゲットを追加"))
        {
            targetRoots.Add(null);
        }

        EditorGUILayout.Space();

        // コピー設定
        GUILayout.Label("コピーするコンポーネントを選択:", EditorStyles.boldLabel);
        // Transformのトグル
        EditorGUILayout.BeginHorizontal();
        var transformContent = EditorGUIUtility.IconContent("Transform Icon");
        transformContent.text = " Transform";
        copyTransform = EditorGUILayout.Toggle(transformContent, copyTransform);
        EditorGUILayout.EndHorizontal();

        // MA Scale Adjusterのトグル
        EditorGUILayout.BeginHorizontal();
        var scaleAdjusterContent = new GUIContent("⚖ MA Scale Adjuster");
        copyScaleAdjuster = EditorGUILayout.Toggle(scaleAdjusterContent, copyScaleAdjuster);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("オプション:", EditorStyles.boldLabel);
        overwriteExistingComponents = EditorGUILayout.Toggle("既存コンポーネントを上書きする", overwriteExistingComponents);
        allowRootNameMismatch = EditorGUILayout.Toggle("ルートオブジェクトの名前不一致を許容する", allowRootNameMismatch);
        
        autoSearchArmature = EditorGUILayout.Toggle("ターゲットを自動検索する", autoSearchArmature);
        
        if (autoSearchArmature)
        {
            EditorGUI.indentLevel++;
            allowPartialMatch = EditorGUILayout.Toggle("部分一致を許可", allowPartialMatch);
            EditorGUI.indentLevel--;
        }

        if (allowRootNameMismatch)
        {
            EditorGUILayout.HelpBox("このオプションを有効にすると、ルートオブジェクトの名前が異なっていても、" +
                "子階層の名前でマッチングを行います。", MessageType.Info);
        }

        if (autoSearchArmature)
        {
            string searchInfo = allowPartialMatch ? 
                "このオプションを有効にすると、以下の優先順位で自動検索します:\n" +
                "1. ソースと同じ名前のオブジェクト\n" +
                "2. ソース名を含むオブジェクト（部分一致）" :
                "このオプションを有効にすると、ソースと同じ名前のオブジェクトを検索します。";
            
            EditorGUILayout.HelpBox(searchInfo, MessageType.Info);
        }

        EditorGUILayout.Space();

        // 実行ボタン
        bool canExecute = sourceRoot != null && (copyTransform || copyScaleAdjuster) && 
                          targetRoots.Count > 0 && targetRoots.Any(t => t != null);

        GUI.enabled = canExecute;
        if (GUILayout.Button("バッチ処理を実行", GUILayout.Height(30)))
        {
            ExecuteBatchCopy();
        }
        GUI.enabled = true;

        // 結果表示
        if (showResults && batchResults.Count > 0)
        {
            EditorGUILayout.Space();
            GUILayout.Label("処理結果", EditorStyles.boldLabel);
            
            // サマリー
            int successCount = batchResults.Count(r => r.success);
            int totalCopiedComponents = batchResults.Sum(r => r.copiedComponents);
            
            EditorGUILayout.HelpBox($"処理完了: {successCount}/{batchResults.Count} 成功\n" +
                $"合計コピーコンポーネント数: {totalCopiedComponents}", 
                successCount == batchResults.Count ? MessageType.Info : MessageType.Warning);

            // 詳細結果
            EditorGUILayout.BeginVertical(GUI.skin.box);
            foreach (var result in batchResults)
            {
                EditorGUILayout.BeginHorizontal();
                
                // アイコン
                GUILayout.Label(result.success ? "✓" : "✗", GUILayout.Width(20));
                
                // ターゲット名
                EditorGUILayout.ObjectField(result.target, typeof(GameObject), true, GUILayout.Width(200));
                
                // 結果メッセージ
                GUILayout.Label(result.message);
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void SetSelectedObjectAsSource()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("警告", "シーンでオブジェクトを選択してください。", "OK");
            return;
        }

        if (selectedObjects.Length > 1)
        {
            EditorUtility.DisplayDialog("警告", "ソースには1つのオブジェクトのみ選択してください。", "OK");
            return;
        }

        sourceRoot = selectedObjects[0];
        EditorUtility.DisplayDialog("設定完了", $"ソースを '{sourceRoot.name}' に設定しました。", "OK");
    }

    private void AddSelectedObjectsAsTargets()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("警告", "シーンでオブジェクトを選択してください。", "OK");
            return;
        }

        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null && !targetRoots.Contains(obj))
            {
                targetRoots.Add(obj);
            }
        }

        EditorUtility.DisplayDialog("追加完了", $"{selectedObjects.Length}個のオブジェクトを追加しました。", "OK");
    }


    private void ExecuteBatchCopy()
    {
        batchResults.Clear();
        showResults = true;

        // 有効なターゲットのみ処理
        List<GameObject> validTargets = targetRoots.Where(t => t != null).ToList();
        
        if (validTargets.Count == 0)
        {
            EditorUtility.DisplayDialog("エラー", "有効なターゲットがありません。", "OK");
            return;
        }

        // プログレスバー表示
        KoikeEditorUtility.ShowProgressBar("バッチ処理中", "処理を開始しています...", 0f);

        try
        {
            for (int i = 0; i < validTargets.Count; i++)
            {
                GameObject target = validTargets[i];
                float progress = (float)i / validTargets.Count;
                KoikeEditorUtility.ShowProgressBar("バッチ処理中", $"処理中: {target.name} ({i + 1}/{validTargets.Count})", progress);

                BatchResult result = ProcessSingleTarget(target);
                batchResults.Add(result);
            }
        }
        finally
        {
            KoikeEditorUtility.ClearProgressBar();
        }

        // 結果サマリーのポップアップ表示
        ShowBatchResultsPopup();
    }

    private BatchResult ProcessSingleTarget(GameObject target)
    {
        BatchResult result = new BatchResult { target = target };

        try
        {
            // 自動検索が有効な場合、ターゲットを調整
            GameObject actualTarget = target;
            if (autoSearchArmature)
            {
                // まずソースと同じ名前で検索
                GameObject foundTarget = KoikeEditorUtility.FindChildByName(target, sourceRoot.name, true);
                
                // 部分一致が許可されていて、見つからない場合は、ソース名を含む名前で検索
                if (foundTarget == null && allowPartialMatch)
                {
                    foundTarget = KoikeEditorUtility.FindChildByName(target, sourceRoot.name, false);
                }
                
                if (foundTarget != null)
                {
                    actualTarget = foundTarget;
                    Debug.Log($"Target found: {foundTarget.name} in {target.name}");
                }
                else
                {
                    Debug.LogWarning($"No matching target found in {target.name}, using root object");
                }
            }

            // ソースオブジェクトの全階層を名前をキーとして格納
            Dictionary<string, GameObject> sourceObjects = new Dictionary<string, GameObject>();
            KoikeEditorUtility.CollectGameObjectsRecursively(sourceRoot, sourceObjects, "", true, allowRootNameMismatch);

            // ターゲットオブジェクトの全階層を名前をキーとして格納
            Dictionary<string, GameObject> targetObjects = new Dictionary<string, GameObject>();
            KoikeEditorUtility.CollectGameObjectsRecursively(actualTarget, targetObjects, "", true, allowRootNameMismatch);

            int copiedComponentCount = 0;
            int matchedObjectCount = 0;

            // ソースの各オブジェクトに対して処理
            foreach (var sourcePair in sourceObjects)
            {
                string objectPath = sourcePair.Key;
                GameObject sourceObj = sourcePair.Value;

                // 同じパスのターゲットオブジェクトを検索
                if (targetObjects.TryGetValue(objectPath, out GameObject targetObj))
                {
                    matchedObjectCount++;

                    // Undoに登録
                    Undo.RegisterCompleteObjectUndo(targetObj, "Batch Copy Components");

                    // 選択されたコンポーネントをコピー
                    copiedComponentCount += CopySelectedComponentsFromTo(sourceObj, targetObj);
                }
            }

            result.success = true;
            result.matchedObjects = matchedObjectCount;
            result.copiedComponents = copiedComponentCount;
            result.message = $"一致: {matchedObjectCount}個, コピー: {copiedComponentCount}個";
        }
        catch (System.Exception e)
        {
            result.success = false;
            result.message = $"エラー: {e.Message}";
        }

        return result;
    }

    private void ShowBatchResultsPopup()
    {
        int successCount = batchResults.Count(r => r.success);
        int totalTargets = batchResults.Count;
        int totalCopiedComponents = batchResults.Sum(r => r.copiedComponents);

        string title = successCount == totalTargets ? "✓ バッチ処理完了" : "⚠ バッチ処理完了（一部エラー）";
        string message = $"処理結果: {successCount}/{totalTargets} 成功\n\n";

        message += "詳細:\n";
        foreach (var result in batchResults)
        {
            string status = result.success ? "✓" : "✗";
            message += $"{status} {result.target.name}: {result.message}\n";
        }

        message += $"\n合計コピーコンポーネント数: {totalCopiedComponents}";

        EditorUtility.DisplayDialog(title, message, "OK");
    }


    private int CopySelectedComponentsFromTo(GameObject source, GameObject target)
    {
        int copiedCount = 0;

        // Transformのコピー
        if (copyTransform)
        {
            Transform sourceTransform = source.transform;
            Transform targetTransform = target.transform;

            Undo.RecordObject(targetTransform, "Copy Transform Values");
            targetTransform.localPosition = sourceTransform.localPosition;
            targetTransform.localRotation = sourceTransform.localRotation;
            targetTransform.localScale = sourceTransform.localScale;

            copiedCount++;
        }

        // MA Scale Adjusterのコピー
        if (copyScaleAdjuster)
        {
            if (KoikeEditorUtility.CopyModularAvatarScaleAdjuster(source, target, overwriteExistingComponents))
            {
                copiedCount++;
            }
        }

        return copiedCount;
    }

    }
}