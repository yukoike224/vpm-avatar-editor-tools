using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace YuKoike.Tools
{
    /// <summary>
    /// 衣装改変の初期セットアップを自動化するエディタ拡張
    /// ベースPrefabからVariantを作成し、シーンとフォルダを一括生成する
    /// </summary>
    public class OutfitSetup : EditorWindow
    {
        private const string AvatarsBasePath = "Assets/_MyWork/Avatars";
        private const string SceneTemplatePath = "Assets/_MyWork/Kaihen/BUDDYWORKS/Avatar Scene/Scene";

        /// <summary>
        /// アバターごとの情報を保持する構造体
        /// </summary>
        private class AvatarEntry
        {
            public string name;
            public string folder;
            public string outfitFolder;
            public string[] basePrefabPaths;
            public string[] basePrefabLabels;
            public string sceneTemplatePath; // SceneTemplate_{Name}.unity（存在しない場合null）
        }

        private AvatarEntry[] avatarEntries;
        private string[] avatarNames;
        private int selectedAvatarIndex = 0;
        private int selectedBaseIndex = 0;
        private string outfitName = "";
        private Vector2 scrollPosition;

        [MenuItem("Tools/Koike's Utils/Outfit Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<OutfitSetup>("Outfit Setup");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            RefreshAvatarList();
        }

        /// <summary>
        /// Avatarsフォルダ内のアバター構成を走査して一覧を構築
        /// </summary>
        private void RefreshAvatarList()
        {
            selectedBaseIndex = 0;

            if (!AssetDatabase.IsValidFolder(AvatarsBasePath))
            {
                avatarEntries = new AvatarEntry[0];
                avatarNames = new string[0];
                return;
            }

            var subFolders = AssetDatabase.GetSubFolders(AvatarsBasePath);
            var entries = new List<AvatarEntry>();

            foreach (var folder in subFolders)
            {
                string folderName = Path.GetFileName(folder);

                // _Outfits または _Outfit フォルダを探す
                string outfitFolder = FindOutfitFolder(folder, folderName);
                if (string.IsNullOrEmpty(outfitFolder))
                    continue;

                // ベースPrefab一覧を収集（複数対応）
                var basePrefabs = FindAllBasePrefabs(folder, folderName);
                if (basePrefabs.Count == 0)
                    continue;

                // シーンテンプレートを探す（SceneTemplate_{Name}.unity）
                string sceneTemplate = FindSceneTemplate(folderName);

                var entry = new AvatarEntry
                {
                    name = folderName,
                    folder = folder,
                    outfitFolder = outfitFolder,
                    basePrefabPaths = basePrefabs.Select(p => p.path).ToArray(),
                    basePrefabLabels = basePrefabs.Select(p => p.label).ToArray(),
                    sceneTemplatePath = sceneTemplate,
                };
                entries.Add(entry);
            }

            avatarEntries = entries.ToArray();
            avatarNames = entries.Select(e => e.name).ToArray();
        }

        /// <summary>
        /// アバターフォルダ内の全ベースPrefabを収集
        /// {FolderName}_Base.prefab を基本とし、{FolderName}_Base_*.prefab をバリエーションとして認識
        /// </summary>
        private List<(string path, string label)> FindAllBasePrefabs(string folder, string folderName)
        {
            var results = new List<(string path, string label)>();
            string prefix = $"{folderName}_Base";

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // 直下のファイルのみ（Outfitsフォルダ内は除外）
                if (Path.GetDirectoryName(path).Replace("\\", "/") != folder)
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith(prefix))
                    continue;

                // ラベル生成: _Base → "Base", _Base_BlendShapes → "Base (BlendShapes)"
                string label;
                if (fileName == prefix)
                {
                    label = "Base";
                }
                else if (fileName.StartsWith(prefix + "_"))
                {
                    string suffix = fileName.Substring(prefix.Length + 1);
                    label = $"Base ({suffix})";
                }
                else
                {
                    continue; // _BaseXxx のような無関係なファイルを除外
                }

                results.Add((path, label));
            }

            // "Base" を先頭にソート
            results.Sort((a, b) =>
            {
                if (a.label == "Base") return -1;
                if (b.label == "Base") return 1;
                return string.Compare(a.label, b.label, System.StringComparison.Ordinal);
            });

            return results;
        }

        /// <summary>
        /// シーンテンプレートを検索（SceneTemplate_{AvatarName}.unity）
        /// 完全一致を優先し、見つからない場合はアバター名の先頭部分で最長一致を試みる
        /// （例: ShinraKun_TBody → SceneTemplate_ShinraKun.unity を共用）
        /// </summary>
        private string FindSceneTemplate(string avatarName)
        {
            // 完全一致を最優先
            string exactPath = $"{SceneTemplatePath}/SceneTemplate_{avatarName}.unity";
            if (File.Exists(exactPath))
                return exactPath;

            // アバター名を '_' で分割し、先頭から順に短くして最長一致を探す
            string[] parts = avatarName.Split('_');
            for (int i = parts.Length - 1; i >= 1; i--)
            {
                string partial = string.Join("_", parts, 0, i);
                string path = $"{SceneTemplatePath}/SceneTemplate_{partial}.unity";
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private string FindOutfitFolder(string folder, string folderName)
        {
            // {FolderName}_Outfits を優先
            string outfitsPath = $"{folder}/{folderName}_Outfits";
            if (AssetDatabase.IsValidFolder(outfitsPath))
                return outfitsPath;

            // {FolderName}_Outfit もチェック
            string outfitPath = $"{folder}/{folderName}_Outfit";
            if (AssetDatabase.IsValidFolder(outfitPath))
                return outfitPath;

            return null;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("衣装改変セットアップ", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            if (avatarEntries == null || avatarEntries.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"アバターが見つかりません。\n{AvatarsBasePath} 以下に _Base.prefab と _Outfits フォルダを持つ構成が必要です。",
                    MessageType.Warning);

                if (GUILayout.Button("再検索"))
                    RefreshAvatarList();

                EditorGUILayout.EndScrollView();
                return;
            }

            // アバター選択
            EditorGUILayout.LabelField("アバターベース", EditorStyles.miniBoldLabel);
            int prevAvatarIndex = selectedAvatarIndex;
            selectedAvatarIndex = EditorGUILayout.Popup("アバター", selectedAvatarIndex, avatarNames);

            // アバターが切り替わったらベース選択をリセット
            if (selectedAvatarIndex != prevAvatarIndex)
                selectedBaseIndex = 0;

            var currentEntry = avatarEntries[selectedAvatarIndex];

            // ベースPrefab選択（複数ある場合のみ表示）
            if (currentEntry.basePrefabLabels.Length > 1)
            {
                selectedBaseIndex = EditorGUILayout.Popup("ベースタイプ", selectedBaseIndex, currentEntry.basePrefabLabels);
            }
            else
            {
                selectedBaseIndex = 0;
            }

            EditorGUILayout.Space(4);

            // 選択中のアバター情報を表示
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("ベースPrefab", currentEntry.basePrefabPaths[selectedBaseIndex]);
                EditorGUILayout.TextField("衣装フォルダ", currentEntry.outfitFolder);
                EditorGUILayout.TextField("シーンテンプレート",
                    !string.IsNullOrEmpty(currentEntry.sceneTemplatePath)
                        ? currentEntry.sceneTemplatePath
                        : "(なし)");
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(12);

            // 衣装名入力
            EditorGUILayout.LabelField("衣装名", EditorStyles.miniBoldLabel);
            outfitName = EditorGUILayout.TextField("衣装名", outfitName);

            // プレビュー表示
            if (!string.IsNullOrWhiteSpace(outfitName))
            {
                string avatarName = currentEntry.name;
                string cleanName = outfitName.Trim();
                string fullName = $"{avatarName}_{cleanName}";
                string targetFolder = $"{currentEntry.outfitFolder}/{fullName}";

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("作成されるファイル", EditorStyles.miniBoldLabel);

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField($"フォルダ: {targetFolder}/");
                    EditorGUILayout.LabelField($"Prefab:  {targetFolder}/{fullName}.prefab");
                    EditorGUILayout.LabelField($"シーン:  {targetFolder}/{fullName}.unity");
                }

                // 既存チェック
                if (AssetDatabase.IsValidFolder(targetFolder))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox("このフォルダは既に存在します。", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(16);

            // 実行ボタン
            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(outfitName));
            if (GUILayout.Button("セットアップ実行", GUILayout.Height(32)))
            {
                ExecuteSetup();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("アバター一覧を再検索"))
                RefreshAvatarList();

            EditorGUILayout.EndScrollView();
        }

        private void ExecuteSetup()
        {
            var currentEntry = avatarEntries[selectedAvatarIndex];
            string avatarName = currentEntry.name;
            string basePrefabPath = currentEntry.basePrefabPaths[selectedBaseIndex];
            string cleanName = outfitName.Trim();
            string fullName = $"{avatarName}_{cleanName}";
            string targetFolder = $"{currentEntry.outfitFolder}/{fullName}";
            string prefabPath = $"{targetFolder}/{fullName}.prefab";
            string scenePath = $"{targetFolder}/{fullName}.unity";

            // 既存チェック
            if (AssetDatabase.IsValidFolder(targetFolder))
            {
                if (!EditorUtility.DisplayDialog("確認",
                    $"フォルダ '{fullName}' は既に存在します。\n既存ファイルを上書きしますか？",
                    "続行", "キャンセル"))
                    return;
            }

            // 現在のシーンを保存確認
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                // 1. フォルダ作成
                if (!AssetDatabase.IsValidFolder(targetFolder))
                {
                    string parentFolder = Path.GetDirectoryName(targetFolder).Replace("\\", "/");
                    string newFolderName = Path.GetFileName(targetFolder);
                    AssetDatabase.CreateFolder(parentFolder, newFolderName);
                    Debug.Log($"フォルダを作成しました: {targetFolder}");
                }

                // 2. Prefab Variant 作成
                if (!File.Exists(prefabPath))
                {
                    GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
                    if (basePrefab == null)
                    {
                        EditorUtility.DisplayDialog("エラー", $"ベースPrefabの読み込みに失敗しました:\n{basePrefabPath}", "OK");
                        return;
                    }

                    // InstantiatePrefab でベースPrefabのインスタンスを生成し、
                    // SaveAsPrefabAsset で保存すると自動的に Prefab Variant になる
                    // （Unity 2022.3 には SaveAsPrefabAssetAsVariant が存在しないため）
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    Object.DestroyImmediate(instance);

                    Debug.Log($"Prefab Variant を作成しました: {prefabPath}");
                }
                else
                {
                    Debug.Log($"Prefab は既に存在します（スキップ）: {prefabPath}");
                }

                // 3. シーン作成（テンプレートコピー + 衣装Variant配置）
                if (!File.Exists(scenePath))
                {
                    if (!string.IsNullOrEmpty(currentEntry.sceneTemplatePath))
                    {
                        // テンプレートシーンをコピー
                        AssetDatabase.CopyAsset(currentEntry.sceneTemplatePath, scenePath);
                        var newScene = EditorSceneManager.OpenScene(scenePath);

                        // 衣装Prefab Variantを配置
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab != null)
                        {
                            PrefabUtility.InstantiatePrefab(prefab, newScene);
                        }

                        EditorSceneManager.SaveScene(newScene);
                    }
                    else
                    {
                        // テンプレートが無い場合は空シーンにフォールバック
                        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab != null)
                        {
                            PrefabUtility.InstantiatePrefab(prefab, newScene);
                        }

                        EditorSceneManager.SaveScene(newScene, scenePath);
                    }

                    Debug.Log($"シーンを作成しました: {scenePath}");
                }
                else
                {
                    Debug.Log($"シーンは既に存在します（スキップ）: {scenePath}");
                }

                // 4. シーンを開く
                EditorSceneManager.OpenScene(scenePath);

                // 5. 作成したPrefabをProjectウィンドウでハイライト
                var createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (createdPrefab != null)
                {
                    EditorGUIUtility.PingObject(createdPrefab);
                    Selection.activeObject = createdPrefab;
                }

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("完了",
                    $"衣装セットアップが完了しました。\n\n" +
                    $"ベース: {Path.GetFileNameWithoutExtension(basePrefabPath)}\n" +
                    $"フォルダ: {targetFolder}\n" +
                    $"Prefab: {fullName}.prefab\n" +
                    $"シーン: {fullName}.unity",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"衣装セットアップ中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("エラー", $"セットアップ中にエラーが発生しました:\n{e.Message}", "OK");
            }
        }
    }
}
