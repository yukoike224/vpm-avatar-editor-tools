using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace YuKoike.Tools
{
    public class FTAvatarModifier : EditorWindow
{
    private FtAvatarList ftAvatarList;
    private Vector2 scrollPosition;
    private bool showPreview = false;
    private List<GameObject> previewResults = new List<GameObject>();
    private string customSuffix = "--FT";

    [MenuItem("Tools/Koike's Utils/FT Avatar Modifier")]
    public static void ShowWindow()
    {
        GetWindow<FTAvatarModifier>("FT Avatar Modifier");
    }

    private void OnGUI()
    {
        GUILayout.Label("FtAvatar Modifier", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // FtAvatarList選択UI
        EditorGUILayout.BeginHorizontal();
        ftAvatarList = (FtAvatarList)EditorGUILayout.ObjectField("FtAvatar List", ftAvatarList, typeof(FtAvatarList), false);

        // 自動検出ボタン
        if (GUILayout.Button("Auto Find", GUILayout.Width(80)))
        {
            AutoFindFtAvatarList();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (ftAvatarList != null)
        {
            // 検出設定表示
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("検出設定", EditorStyles.boldLabel);

            string currentSuffix = ftAvatarList.avatarSuffix;
            if (string.IsNullOrEmpty(currentSuffix))
                currentSuffix = "未設定";

            EditorGUILayout.LabelField($"サフィックス: {currentSuffix}");
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // アバター数の表示
            int validAvatarCount = ftAvatarList.ftAvatars?.Count(avatar => avatar != null) ?? 0;
            int totalCount = ftAvatarList.ftAvatars?.Count ?? 0;
            EditorGUILayout.LabelField($"Valid Avatars: {validAvatarCount} / {totalCount}");

            // プレビュー機能
            GUILayout.Space(5);
            if (GUILayout.Button("Preview Changes"))
            {
                PreviewChanges();
            }

            if (showPreview && previewResults.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Preview Results:", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                foreach (var result in previewResults)
                {
                    if (result != null)
                    {
                        EditorGUILayout.ObjectField(result, typeof(GameObject), false);
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            // 実行ボタン
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Modify FtAvatars", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("確認",
                    $"{validAvatarCount}個のアバターを変更します。この操作は元に戻せません。続行しますか？",
                    "実行", "キャンセル"))
                {
                    ModifyFtAvatars();
                }
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            EditorGUILayout.HelpBox("FtAvatarListを選択してください。", MessageType.Info);
        }

        GUILayout.Space(20);

        // 手動検索セクション
        EditorGUILayout.LabelField("手動検索", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        customSuffix = EditorGUILayout.TextField("カスタムサフィックス", customSuffix);
        if (GUILayout.Button("この条件で検索", GUILayout.Width(120)))
        {
            SearchWithCustomSuffix();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // バッチ処理オプション
        EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Process All in Project"))
        {
            ProcessAllFtAvatarListsInProject();
        }

        if (GUILayout.Button("Undo Last Changes"))
        {
            UndoLastChanges();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void AutoFindFtAvatarList()
    {
        string[] guids = AssetDatabase.FindAssets("t:FtAvatarList");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            ftAvatarList = AssetDatabase.LoadAssetAtPath<FtAvatarList>(path);

            if (guids.Length > 1)
            {
                Debug.Log($"複数のFtAvatarListが見つかりました。最初の1つを選択しました: {path}");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found", "プロジェクト内にFtAvatarListが見つかりませんでした。", "OK");
        }
    }

    private void PreviewChanges()
    {
        previewResults.Clear();

        if (ftAvatarList?.ftAvatars == null) return;

        foreach (GameObject ftAvatar in ftAvatarList.ftAvatars)
        {
            if (ftAvatar == null) continue;

            // サフィックスチェック
            if (!string.IsNullOrEmpty(ftAvatarList.avatarSuffix) &&
                ftAvatar.name.EndsWith(ftAvatarList.avatarSuffix))
            {
                previewResults.Add(ftAvatar);
            }
        }

        showPreview = true;
        Debug.Log($"プレビュー完了: {previewResults.Count}個のFtAvatarが見つかりました。");
    }

    private void ModifyFtAvatars()
    {
        if (ftAvatarList?.ftAvatars == null)
        {
            Debug.LogError("FtAvatar List is not assigned or empty.");
            return;
        }

        int processedCount = 0;
        int modifiedCount = 0;
        List<string> modifiedPrefabs = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (GameObject ftAvatar in ftAvatarList.ftAvatars)
            {
                if (ftAvatar == null)
                {
                    Debug.LogWarning("One of the FtAvatars in the list is null.");
                    continue;
                }

                processedCount++;

                // プログレスバー表示
                KoikeEditorUtility.ShowProgressBar("Processing FT Avatars",
                    $"Processing {ftAvatar.name}...",
                    (float)processedCount / ftAvatarList.ftAvatars.Count);

                if (ModifySingleAvatar(ftAvatar))
                {
                    modifiedCount++;
                    modifiedPrefabs.Add(ftAvatar.name);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            KoikeEditorUtility.ClearProgressBar();
        }

        // 結果表示
        string message = $"処理完了:\n" +
                        $"処理済み: {processedCount}個\n" +
                        $"変更済み: {modifiedCount}個";

        if (modifiedPrefabs.Count > 0)
        {
            message += "\n\n変更されたプレハブ:\n" + string.Join("\n", modifiedPrefabs);
        }

        EditorUtility.DisplayDialog("FtAvatar Modifier", message, "OK");
        Debug.Log($"FtAvatar modification complete. Modified {modifiedCount} out of {processedCount} avatars.");
    }

    private bool ModifySingleAvatar(GameObject ftAvatar)
    {
        return KoikeEditorUtility.SafeProcessPrefab(ftAvatar, (instance) =>
        {
            Transform[] allTransforms = instance.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                if (t.name == "FaceEmoPrefab")
                {
                    t.gameObject.SetActive(false);
                    t.gameObject.tag = "EditorOnly";
                    Debug.Log($"Disabled FaceEmoPrefab in {ftAvatar.name}");
                }
            }
        });
    }

    private void ProcessAllFtAvatarListsInProject()
    {
        string[] guids = AssetDatabase.FindAssets("t:FtAvatarList");

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Not Found", "プロジェクト内にFtAvatarListが見つかりませんでした。", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("確認",
            $"プロジェクト内の{guids.Length}個のFtAvatarListを全て処理します。続行しますか？",
            "実行", "キャンセル"))
        {
            return;
        }

        int totalProcessed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FtAvatarList list = AssetDatabase.LoadAssetAtPath<FtAvatarList>(path);

            if (list != null)
            {
                ftAvatarList = list;
                ModifyFtAvatars();
                totalProcessed++;
            }
        }

        Debug.Log($"バッチ処理完了: {totalProcessed}個のFtAvatarListを処理しました。");
    }

    private void UndoLastChanges()
    {
        EditorUtility.DisplayDialog("Undo機能",
            "Undo機能は現在開発中です。\n手動でプレハブを元に戻すか、バックアップから復元してください。",
            "OK");
    }

    private void SearchWithCustomSuffix()
    {
        if (string.IsNullOrEmpty(customSuffix))
        {
            EditorUtility.DisplayDialog("エラー", "サフィックスを入力してください。", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        List<GameObject> foundAvatars = new List<GameObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && prefab.name.EndsWith(customSuffix))
            {
                foundAvatars.Add(prefab);
            }
        }

        if (foundAvatars.Count > 0)
        {
            // 新しいFtAvatarListを作成するか確認
            if (EditorUtility.DisplayDialog("検索完了",
                $"{foundAvatars.Count}個のアバターが見つかりました。\n新しいFtAvatarListを作成しますか？",
                "作成", "キャンセル"))
            {
                CreateFtAvatarListFromSearch(foundAvatars, customSuffix);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("検索結果",
                $"サフィックス '{customSuffix}' を持つプレハブが見つかりませんでした。", "OK");
        }
    }

    private void CreateFtAvatarListFromSearch(List<GameObject> avatars, string suffix)
    {
        FtAvatarList newList = CreateInstance<FtAvatarList>();
        newList.ftAvatars = new List<GameObject>(avatars);
        newList.avatarSuffix = suffix;

        string path = EditorUtility.SaveFilePanelInProject(
            "Save FtAvatarList",
            $"FtAvatarList_{suffix.Replace("-", "")}",
            "asset",
            "新しいFtAvatarListを保存");

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newList, path);
            AssetDatabase.SaveAssets();

            ftAvatarList = newList;

            Debug.Log($"新しいFtAvatarListを作成しました: {path} ({avatars.Count}個のアバター)");
            EditorGUIUtility.PingObject(newList);
        }
    }

    // FtAvatarListから直接実行するためのメソッド
    public static void ModifyFromList(FtAvatarList targetList)
    {
        if (targetList == null)
        {
            Debug.LogError("FtAvatarList is null.");
            return;
        }

        FTAvatarModifier modifier = CreateInstance<FTAvatarModifier>();
        modifier.ftAvatarList = targetList;
        modifier.ModifyFtAvatars();
    }
    }
}
