using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YuKoike.Tools
{
    /// <summary>
    /// .unitypackage のインポート先を指定できるエディタ拡張。
    /// インポート後に新規追加されたアセットを指定フォルダへ自動移動する。
    /// </summary>
    public class PackageImporter : EditorWindow
    {
        private const string DefaultDestination = "Assets/_ImportedAssets/BOOTH";

        private string packagePath = "";
        private Object destinationFolder;
        private Vector2 scrollPosition;

        // インポート監視用
        private static PackageImporter activeInstance;
        private HashSet<string> preImportAssets;
        private string destinationPathCache;
        private bool isWaitingForImport;

        [MenuItem("Tools/Koike's Utils/Package Importer")]
        static void Init()
        {
            PackageImporter window = (PackageImporter)GetWindow(typeof(PackageImporter));
            window.titleContent = new GUIContent("Package Importer");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        void OnEnable()
        {
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
        }

        void OnDisable()
        {
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled -= OnImportPackageCancelled;
            AssetDatabase.importPackageFailed -= OnImportPackageFailed;
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Package Importer", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "インポート先フォルダを指定して .unitypackage をインポートします。\n" +
                "インポート後、新規アセットが自動的に指定フォルダへ移動されます。",
                MessageType.Info);

            GUILayout.Space(10);

            // パッケージファイル選択
            EditorGUILayout.LabelField("パッケージファイル", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            packagePath = EditorGUILayout.TextField(packagePath);
            if (GUILayout.Button("選択...", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFilePanel(
                    "UnityPackage を選択", "", "unitypackage");
                if (!string.IsNullOrEmpty(selected))
                {
                    packagePath = selected;
                }
            }
            EditorGUILayout.EndHorizontal();

            // ドラッグ＆ドロップ対応
            Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "ここに .unitypackage をドラッグ＆ドロップ", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);

            GUILayout.Space(10);

            // インポート先フォルダ
            EditorGUILayout.LabelField("インポート先フォルダ", EditorStyles.boldLabel);
            destinationFolder = EditorGUILayout.ObjectField(
                "移動先:", destinationFolder, typeof(Object), false);

            string destPath = GetDestinationPath();
            EditorGUILayout.LabelField("パス:", destPath, EditorStyles.miniLabel);

            GUILayout.Space(10);

            // インポートボタン
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(packagePath) || isWaitingForImport);
            if (GUILayout.Button("インポート", GUILayout.Height(30)))
            {
                ExecuteImport();
            }
            EditorGUI.EndDisabledGroup();

            if (isWaitingForImport)
            {
                EditorGUILayout.HelpBox("インポート中...", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0 &&
                        DragAndDrop.paths[0].EndsWith(".unitypackage"))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        evt.Use();
                    }
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                    {
                        string path = DragAndDrop.paths[0];
                        if (path.EndsWith(".unitypackage"))
                        {
                            packagePath = path;
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        private string GetDestinationPath()
        {
            if (destinationFolder != null)
            {
                string path = AssetDatabase.GetAssetPath(destinationFolder);
                if (AssetDatabase.IsValidFolder(path))
                    return path;
            }
            return DefaultDestination;
        }

        private void ExecuteImport()
        {
            if (!File.Exists(packagePath))
            {
                EditorUtility.DisplayDialog("エラー",
                    "指定されたパッケージファイルが見つかりません。", "OK");
                return;
            }

            string destPath = GetDestinationPath();

            // インポート先フォルダを確保
            EnsureFolderExists(destPath);

            // スナップショットを取得（Assets直下のエントリ一覧）
            preImportAssets = new HashSet<string>(
                AssetDatabase.GetSubFolders("Assets"));

            // Assets直下のファイルも取得
            string[] rootFiles = AssetDatabase.FindAssets("", new[] { "Assets" });
            foreach (string guid in rootFiles)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                // Assets直下のもののみ（サブフォルダ内は除外）
                if (Path.GetDirectoryName(assetPath).Replace("\\", "/") == "Assets")
                {
                    preImportAssets.Add(assetPath);
                }
            }

            destinationPathCache = destPath;
            isWaitingForImport = true;
            activeInstance = this;

            // インタラクティブモードでインポート（ユーザーが内容を確認できる）
            AssetDatabase.ImportPackage(packagePath, true);
        }

        private void OnImportPackageCompleted(string packageName)
        {
            if (!isWaitingForImport || activeInstance != this)
                return;

            isWaitingForImport = false;

            // 新規追加されたアセットを検出
            List<string> newTopLevelFolders = new List<string>();
            string[] currentFolders = AssetDatabase.GetSubFolders("Assets");

            foreach (string folder in currentFolders)
            {
                if (!preImportAssets.Contains(folder))
                {
                    newTopLevelFolders.Add(folder);
                }
            }

            // Assets直下の新規ファイルも検出
            List<string> newTopLevelFiles = new List<string>();
            string[] allRootAssets = AssetDatabase.FindAssets("", new[] { "Assets" });
            foreach (string guid in allRootAssets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(assetPath).Replace("\\", "/") == "Assets" &&
                    !preImportAssets.Contains(assetPath))
                {
                    newTopLevelFiles.Add(assetPath);
                }
            }

            if (newTopLevelFolders.Count == 0 && newTopLevelFiles.Count == 0)
            {
                // 新規トップレベルアセットなし（既存フォルダ内にインポートされた等）
                Debug.Log($"[PackageImporter] パッケージ '{packageName}' をインポートしました（移動対象なし）");
                return;
            }

            // 移動実行
            int movedCount = 0;
            List<string> errors = new List<string>();

            foreach (string folder in newTopLevelFolders)
            {
                string folderName = Path.GetFileName(folder);
                string targetPath = destinationPathCache + "/" + folderName;

                if (AssetDatabase.IsValidFolder(targetPath))
                {
                    // 同名フォルダが既に存在する場合はマージ
                    string result = MergeFolder(folder, targetPath);
                    if (result != null)
                    {
                        errors.Add(result);
                    }
                    else
                    {
                        movedCount++;
                    }
                }
                else
                {
                    string error = AssetDatabase.MoveAsset(folder, targetPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        movedCount++;
                    }
                    else
                    {
                        errors.Add($"{folder} → {targetPath}: {error}");
                    }
                }
            }

            foreach (string file in newTopLevelFiles)
            {
                string fileName = Path.GetFileName(file);
                string targetPath = destinationPathCache + "/" + fileName;

                string error = AssetDatabase.MoveAsset(file, targetPath);
                if (string.IsNullOrEmpty(error))
                {
                    movedCount++;
                }
                else
                {
                    errors.Add($"{file} → {targetPath}: {error}");
                }
            }

            // 結果表示
            int totalNew = newTopLevelFolders.Count + newTopLevelFiles.Count;
            if (errors.Count > 0)
            {
                Debug.LogWarning($"[PackageImporter] '{packageName}' インポート完了。" +
                    $"{movedCount}/{totalNew} 件移動、{errors.Count} 件エラー:\n" +
                    string.Join("\n", errors));
            }
            else
            {
                Debug.Log($"[PackageImporter] '{packageName}' をインポートし、" +
                    $"{movedCount} 件を {destinationPathCache} に移動しました。");
            }

            Repaint();
        }

        private void OnImportPackageCancelled(string packageName)
        {
            if (!isWaitingForImport || activeInstance != this)
                return;

            isWaitingForImport = false;
            Debug.Log($"[PackageImporter] パッケージ '{packageName}' のインポートがキャンセルされました。");
            Repaint();
        }

        private void OnImportPackageFailed(string packageName, string errorMessage)
        {
            if (!isWaitingForImport || activeInstance != this)
                return;

            isWaitingForImport = false;
            Debug.LogError($"[PackageImporter] パッケージ '{packageName}' のインポートに失敗しました: {errorMessage}");
            Repaint();
        }

        /// <summary>
        /// ソースフォルダの中身を既存のターゲットフォルダにマージする
        /// </summary>
        private string MergeFolder(string sourceFolder, string targetFolder)
        {
            // サブフォルダを再帰的にマージ
            string[] subFolders = AssetDatabase.GetSubFolders(sourceFolder);
            foreach (string subFolder in subFolders)
            {
                string subName = Path.GetFileName(subFolder);
                string targetSub = targetFolder + "/" + subName;

                if (AssetDatabase.IsValidFolder(targetSub))
                {
                    string result = MergeFolder(subFolder, targetSub);
                    if (result != null)
                        return result;
                }
                else
                {
                    string error = AssetDatabase.MoveAsset(subFolder, targetSub);
                    if (!string.IsNullOrEmpty(error))
                        return $"{subFolder} → {targetSub}: {error}";
                }
            }

            // ファイルを移動
            string[] guids = AssetDatabase.FindAssets("", new[] { sourceFolder });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                // ソースフォルダ直下のもののみ
                if (Path.GetDirectoryName(assetPath).Replace("\\", "/") == sourceFolder)
                {
                    string fileName = Path.GetFileName(assetPath);
                    string targetPath = targetFolder + "/" + fileName;
                    string error = AssetDatabase.MoveAsset(assetPath, targetPath);
                    if (!string.IsNullOrEmpty(error))
                        return $"{assetPath} → {targetPath}: {error}";
                }
            }

            // 空になったソースフォルダを削除
            AssetDatabase.DeleteAsset(sourceFolder);
            return null;
        }

        /// <summary>
        /// フォルダパスを再帰的に作成する
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            string folderName = Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
