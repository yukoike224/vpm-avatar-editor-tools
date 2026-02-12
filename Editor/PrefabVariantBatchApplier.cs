using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace YuKoike.Tools
{
    /// <summary>
    /// カラバリプレファブ間でオーバーライドをバッチ適用するエディタ拡張
    /// </summary>
    public class PrefabVariantBatchApplier : EditorWindow
    {
        private GameObject sourcePrefab;
        private List<GameObject> targetPrefabs = new List<GameObject>();

        // コピーする要素の選択
        private bool copyMAShapeChanger = true;
        private bool copyMAObjectToggle = true;
        private bool copyMAScaleAdjuster = true;
        private bool copyMAMenuInstaller = true;
        private bool copyMAMenuItem = true;
        private bool copyAvatarToggleMenu = true;
        private bool copyAvatarChooseMenu = true;
        private bool copyAvatarRadialMenu = true;
        private bool copyAvatarToggleMenuCreator = true;
        private bool copyAvatarChooseMenuCreator = true;
        private bool copyAvatarRadialMenuCreator = true;
        private bool copyAvatarParametersPresets = true;
        private bool copyTransform = true;
        private bool copyVRCPhysBoneCollider = true;
        private bool copyActiveState = true;
        private bool copyTag = true;
        private bool copyNewObjects = true;

        // オプション
        private bool overwriteExisting = true;
        private bool allowRootNameMismatch = false;

        private Vector2 scrollPosition;
        private Vector2 resultScrollPosition;
        private List<BatchResult> batchResults = new List<BatchResult>();
        private bool showResults = false;

        private class BatchResult
        {
            public GameObject target;
            public bool success;
            public int matchedObjects;
            public int copiedComponents;
            public int addedObjects;
            public string message;
            public List<string> details = new List<string>();
        }

        [MenuItem("Tools/Koike's Utils/Prefab Variant Batch Applier")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabVariantBatchApplier>("Prefab Batch Applier");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            GUILayout.Label("Prefab Variant Batch Applier", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("同じ構造のカラバリプレファブ間で、オーバーライドをバッチ適用します。\n" +
                "1つのプレファブの変更を他のカラバリプレファブにまとめて反映できます。", MessageType.Info);
            EditorGUILayout.Space();

            // ソース設定
            GUILayout.Label("ソース設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("変更を適用済みのプレファブを指定してください。", MessageType.None);
            sourcePrefab = EditorGUILayout.ObjectField("ソースプレファブ:", sourcePrefab, typeof(GameObject), true) as GameObject;

            // シーンから選択されたオブジェクトをソースに設定するボタン
            if (GUILayout.Button("シーンの選択オブジェクトをソースに設定"))
            {
                SetSelectedObjectAsSource();
            }

            EditorGUILayout.Space();

            // ターゲット設定
            GUILayout.Label("ターゲット設定", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("変更を適用したい他のカラバリプレファブを追加してください。", MessageType.None);

            if (GUILayout.Button("シーンの選択オブジェクトを追加"))
            {
                AddSelectedObjectsAsTargets();
            }

            // ターゲットリスト表示
            if (targetPrefabs.Count > 0)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(150));

                for (int i = 0; i < targetPrefabs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    targetPrefabs[i] = EditorGUILayout.ObjectField($"ターゲット {i + 1}:", targetPrefabs[i], typeof(GameObject), true) as GameObject;

                    if (GUILayout.Button("削除", GUILayout.Width(50)))
                    {
                        targetPrefabs.RemoveAt(i);
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
                    targetPrefabs.Clear();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("ターゲットプレファブがありません。", MessageType.Warning);
            }

            if (GUILayout.Button("手動でターゲットを追加"))
            {
                targetPrefabs.Add(null);
            }

            EditorGUILayout.Space();

            // コピー設定
            GUILayout.Label("適用する要素を選択:", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Modular Avatarコンポーネント", EditorStyles.miniBoldLabel);
            copyMAShapeChanger = EditorGUILayout.Toggle("MA Shape Changer", copyMAShapeChanger);
            copyMAObjectToggle = EditorGUILayout.Toggle("MA Object Toggle", copyMAObjectToggle);
            copyMAScaleAdjuster = EditorGUILayout.Toggle("MA Scale Adjuster", copyMAScaleAdjuster);
            copyMAMenuInstaller = EditorGUILayout.Toggle("MA Menu Installer", copyMAMenuInstaller);
            copyMAMenuItem = EditorGUILayout.Toggle("MA Menu Item", copyMAMenuItem);

            EditorGUILayout.Space(5);
            GUILayout.Label("Avatar Menu Creator for MAコンポーネント", EditorStyles.miniBoldLabel);
            copyAvatarToggleMenu = EditorGUILayout.Toggle("Avatar Toggle Menu", copyAvatarToggleMenu);
            copyAvatarChooseMenu = EditorGUILayout.Toggle("Avatar Choose Menu", copyAvatarChooseMenu);
            copyAvatarRadialMenu = EditorGUILayout.Toggle("Avatar Radial Menu", copyAvatarRadialMenu);
            copyAvatarToggleMenuCreator = EditorGUILayout.Toggle("Avatar Toggle Menu Creator", copyAvatarToggleMenuCreator);
            copyAvatarChooseMenuCreator = EditorGUILayout.Toggle("Avatar Choose Menu Creator", copyAvatarChooseMenuCreator);
            copyAvatarRadialMenuCreator = EditorGUILayout.Toggle("Avatar Radial Menu Creator", copyAvatarRadialMenuCreator);

            EditorGUILayout.Space(5);
            GUILayout.Label("Avatar Parameters関連コンポーネント", EditorStyles.miniBoldLabel);
            copyAvatarParametersPresets = EditorGUILayout.Toggle("Avatar Parameters Presets", copyAvatarParametersPresets);

            EditorGUILayout.Space(5);
            GUILayout.Label("VRChatコンポーネント", EditorStyles.miniBoldLabel);
            copyVRCPhysBoneCollider = EditorGUILayout.Toggle("VRC Phys Bone Collider", copyVRCPhysBoneCollider);

            EditorGUILayout.Space(5);
            GUILayout.Label("基本要素", EditorStyles.miniBoldLabel);
            copyTransform = EditorGUILayout.Toggle("Transform", copyTransform);
            copyActiveState = EditorGUILayout.Toggle("アクティブ状態", copyActiveState);
            copyTag = EditorGUILayout.Toggle("タグ", copyTag);

            EditorGUILayout.Space(5);
            GUILayout.Label("階層構造", EditorStyles.miniBoldLabel);
            copyNewObjects = EditorGUILayout.Toggle("新規オブジェクトの追加", copyNewObjects);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // オプション
            GUILayout.Label("オプション:", EditorStyles.boldLabel);
            overwriteExisting = EditorGUILayout.Toggle("既存を上書きする", overwriteExisting);
            allowRootNameMismatch = EditorGUILayout.Toggle("ルート名の不一致を許容", allowRootNameMismatch);

            if (allowRootNameMismatch)
            {
                EditorGUILayout.HelpBox("ルート名が異なる場合でも、子階層の名前でマッチングを行います。", MessageType.Info);
            }

            EditorGUILayout.Space();

            // 実行ボタン
            bool canExecute = sourcePrefab != null &&
                              targetPrefabs.Count > 0 &&
                              targetPrefabs.Any(t => t != null) &&
                              (copyMAShapeChanger || copyMAObjectToggle || copyMAScaleAdjuster ||
                               copyMAMenuInstaller || copyMAMenuItem || copyAvatarToggleMenu ||
                               copyAvatarChooseMenu || copyAvatarRadialMenu || copyAvatarToggleMenuCreator ||
                               copyAvatarChooseMenuCreator || copyAvatarRadialMenuCreator ||
                               copyAvatarParametersPresets || copyTransform || copyVRCPhysBoneCollider ||
                               copyActiveState || copyTag || copyNewObjects);

            GUI.enabled = canExecute;
            if (GUILayout.Button("バッチ適用を実行", GUILayout.Height(35)))
            {
                ExecuteBatchApply();
            }
            GUI.enabled = true;

            // 結果表示
            if (showResults && batchResults.Count > 0)
            {
                EditorGUILayout.Space();
                GUILayout.Label("処理結果", EditorStyles.boldLabel);

                int successCount = batchResults.Count(r => r.success);
                int totalCopiedComponents = batchResults.Sum(r => r.copiedComponents);
                int totalAddedObjects = batchResults.Sum(r => r.addedObjects);

                EditorGUILayout.HelpBox($"処理完了: {successCount}/{batchResults.Count} 成功\n" +
                    $"コピーしたコンポーネント数: {totalCopiedComponents}\n" +
                    $"追加したオブジェクト数: {totalAddedObjects}",
                    successCount == batchResults.Count ? MessageType.Info : MessageType.Warning);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                resultScrollPosition = EditorGUILayout.BeginScrollView(resultScrollPosition, GUILayout.MaxHeight(200));

                foreach (var result in batchResults)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(result.success ? "✓" : "✗", GUILayout.Width(20));
                    EditorGUILayout.ObjectField(result.target, typeof(GameObject), true, GUILayout.Width(200));
                    GUILayout.Label(result.message);

                    EditorGUILayout.EndHorizontal();

                    // 詳細表示
                    if (result.details.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var detail in result.details)
                        {
                            EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndScrollView();
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

            sourcePrefab = selectedObjects[0];
            EditorUtility.DisplayDialog("設定完了", $"ソースを '{sourcePrefab.name}' に設定しました。", "OK");
        }

        private void AddSelectedObjectsAsTargets()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("警告", "シーンでオブジェクトを選択してください。", "OK");
                return;
            }

            int addedCount = 0;
            foreach (GameObject obj in selectedObjects)
            {
                if (obj != null && !targetPrefabs.Contains(obj) && obj != sourcePrefab)
                {
                    targetPrefabs.Add(obj);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.DisplayDialog("追加完了", $"{addedCount}個のオブジェクトを追加しました。", "OK");
            }
        }

        private void ExecuteBatchApply()
        {
            batchResults.Clear();
            showResults = true;

            List<GameObject> validTargets = targetPrefabs.Where(t => t != null).ToList();

            if (validTargets.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "有効なターゲットがありません。", "OK");
                return;
            }

            KoikeEditorUtility.ShowProgressBar("バッチ適用中", "処理を開始しています...", 0f);

            try
            {
                for (int i = 0; i < validTargets.Count; i++)
                {
                    GameObject target = validTargets[i];
                    float progress = (float)i / validTargets.Count;
                    KoikeEditorUtility.ShowProgressBar("バッチ適用中", $"処理中: {target.name} ({i + 1}/{validTargets.Count})", progress);

                    BatchResult result = ProcessSingleTarget(target);
                    batchResults.Add(result);
                }
            }
            finally
            {
                KoikeEditorUtility.ClearProgressBar();
            }

            ShowBatchResultsPopup();
        }

        private BatchResult ProcessSingleTarget(GameObject target)
        {
            BatchResult result = new BatchResult { target = target };

            try
            {
                // ソースとターゲットの階層を収集
                Dictionary<string, GameObject> sourceObjects = new Dictionary<string, GameObject>();
                KoikeEditorUtility.CollectGameObjectsRecursively(sourcePrefab, sourceObjects, "", true, allowRootNameMismatch);

                Dictionary<string, GameObject> targetObjects = new Dictionary<string, GameObject>();
                KoikeEditorUtility.CollectGameObjectsRecursively(target, targetObjects, "", true, allowRootNameMismatch);

                int copiedComponentCount = 0;
                int matchedObjectCount = 0;
                int addedObjectCount = 0;

                // 既存オブジェクトの処理
                foreach (var sourcePair in sourceObjects)
                {
                    string objectPath = sourcePair.Key;
                    GameObject sourceObj = sourcePair.Value;

                    if (targetObjects.TryGetValue(objectPath, out GameObject targetObj))
                    {
                        matchedObjectCount++;
                        Undo.RegisterCompleteObjectUndo(targetObj, "Batch Apply Overrides");

                        int copied = ApplyOverrides(sourceObj, targetObj);
                        copiedComponentCount += copied;

                        if (copied > 0)
                        {
                            result.details.Add($"  {objectPath}: {copied}個のコンポーネントをコピー");
                        }
                    }
                    else if (copyNewObjects)
                    {
                        // 新規オブジェクトの追加
                        GameObject newObj = CreateNewObject(sourceObj, targetObj, objectPath, target);
                        if (newObj != null)
                        {
                            addedObjectCount++;
                            result.details.Add($"  新規追加: {objectPath}");
                        }
                    }
                }

                result.success = true;
                result.matchedObjects = matchedObjectCount;
                result.copiedComponents = copiedComponentCount;
                result.addedObjects = addedObjectCount;
                result.message = $"一致: {matchedObjectCount}, コピー: {copiedComponentCount}, 追加: {addedObjectCount}";
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"エラー: {e.Message}";
                Debug.LogError($"Error processing {target.name}: {e}");
            }

            return result;
        }

        private GameObject CreateNewObject(GameObject sourceObj, GameObject parentTargetObj, string objectPath, GameObject targetRoot)
        {
            // パスから親を特定
            string parentPath = "";
            int lastSlashIndex = objectPath.LastIndexOf('/');
            if (lastSlashIndex > 0)
            {
                parentPath = objectPath.Substring(0, lastSlashIndex);
            }

            Dictionary<string, GameObject> targetObjects = new Dictionary<string, GameObject>();
            KoikeEditorUtility.CollectGameObjectsRecursively(targetRoot, targetObjects, "", true, allowRootNameMismatch);

            GameObject parentObj = string.IsNullOrEmpty(parentPath) ? targetRoot :
                                    (targetObjects.ContainsKey(parentPath) ? targetObjects[parentPath] : null);

            if (parentObj == null)
            {
                Debug.LogWarning($"親オブジェクトが見つかりません: {parentPath}");
                return null;
            }

            // 新規オブジェクトを作成
            GameObject newObj = new GameObject(sourceObj.name);
            Undo.RegisterCreatedObjectUndo(newObj, "Create New Object");
            newObj.transform.SetParent(parentObj.transform, false);

            // オーバーライドを適用
            ApplyOverrides(sourceObj, newObj);

            return newObj;
        }

        private int ApplyOverrides(GameObject source, GameObject target)
        {
            int copiedCount = 0;

            // Transform
            if (copyTransform)
            {
                Transform sourceTransform = source.transform;
                Transform targetTransform = target.transform;

                Undo.RecordObject(targetTransform, "Copy Transform");
                targetTransform.localPosition = sourceTransform.localPosition;
                targetTransform.localRotation = sourceTransform.localRotation;
                targetTransform.localScale = sourceTransform.localScale;

                copiedCount++;
            }

            // アクティブ状態
            if (copyActiveState && source.activeSelf != target.activeSelf)
            {
                Undo.RecordObject(target, "Copy Active State");
                target.SetActive(source.activeSelf);
                copiedCount++;
            }

            // タグ
            if (copyTag && source.tag != target.tag)
            {
                Undo.RecordObject(target, "Copy Tag");
                target.tag = source.tag;
                copiedCount++;
            }

            // MA Shape Changer
            if (copyMAShapeChanger)
            {
                if (CopyComponentByTypeName(source, target, "ModularAvatarShapeChanger"))
                {
                    copiedCount++;
                }
            }

            // MA Object Toggle
            if (copyMAObjectToggle)
            {
                if (CopyComponentByTypeName(source, target, "ModularAvatarObjectToggle"))
                {
                    copiedCount++;
                }
            }

            // MA Scale Adjuster
            if (copyMAScaleAdjuster)
            {
                if (KoikeEditorUtility.CopyModularAvatarScaleAdjuster(source, target, overwriteExisting))
                {
                    copiedCount++;
                }
            }

            // MA Menu Installer
            if (copyMAMenuInstaller)
            {
                if (CopyComponentByTypeName(source, target, "ModularAvatarMenuInstaller"))
                {
                    copiedCount++;
                }
            }

            // MA Menu Item
            if (copyMAMenuItem)
            {
                if (CopyComponentByTypeName(source, target, "ModularAvatarMenuItem"))
                {
                    copiedCount++;
                }
            }

            // Avatar Toggle Menu
            if (copyAvatarToggleMenu)
            {
                if (CopyComponentByTypeName(source, target, "AvatarToggleMenu"))
                {
                    copiedCount++;
                }
            }

            // Avatar Choose Menu
            if (copyAvatarChooseMenu)
            {
                if (CopyComponentByTypeName(source, target, "AvatarChooseMenu"))
                {
                    copiedCount++;
                }
            }

            // Avatar Radial Menu
            if (copyAvatarRadialMenu)
            {
                if (CopyComponentByTypeName(source, target, "AvatarRadialMenu"))
                {
                    copiedCount++;
                }
            }

            // Avatar Toggle Menu Creator
            if (copyAvatarToggleMenuCreator)
            {
                if (CopyComponentByTypeName(source, target, "AvatarToggleMenuCreator"))
                {
                    copiedCount++;
                }
            }

            // Avatar Choose Menu Creator
            if (copyAvatarChooseMenuCreator)
            {
                if (CopyComponentByTypeName(source, target, "AvatarChooseMenuCreator"))
                {
                    copiedCount++;
                }
            }

            // Avatar Radial Menu Creator
            if (copyAvatarRadialMenuCreator)
            {
                if (CopyComponentByTypeName(source, target, "AvatarRadialMenuCreator"))
                {
                    copiedCount++;
                }
            }

            // Avatar Parameters Presets
            if (copyAvatarParametersPresets)
            {
                if (CopyComponentByTypeName(source, target, "AvatarParametersPresets"))
                {
                    copiedCount++;
                }
            }

            // VRC Phys Bone Collider
            if (copyVRCPhysBoneCollider)
            {
                if (CopyComponentByTypeName(source, target, "VRCPhysBoneCollider"))
                {
                    copiedCount++;
                }
            }

            return copiedCount;
        }

        private bool CopyComponentByTypeName(GameObject source, GameObject target, string typeName)
        {
            Component sourceComponent = source.GetComponent(typeName);
            if (sourceComponent == null)
            {
                // 名前空間付きで再検索
                foreach (var component in source.GetComponents<Component>())
                {
                    if (component != null && component.GetType().Name == typeName)
                    {
                        sourceComponent = component;
                        break;
                    }
                }
            }

            if (sourceComponent == null)
                return false;

            System.Type componentType = sourceComponent.GetType();
            Component targetComponent = target.GetComponent(componentType);

            if (targetComponent != null)
            {
                if (overwriteExisting)
                {
                    Undo.RecordObject(targetComponent, $"Overwrite {typeName}");
                    EditorUtility.CopySerialized(sourceComponent, targetComponent);
                    return true;
                }
                return false;
            }
            else
            {
                Undo.AddComponent(target, componentType);
                targetComponent = target.GetComponent(componentType);
                EditorUtility.CopySerialized(sourceComponent, targetComponent);
                return true;
            }
        }

        private void ShowBatchResultsPopup()
        {
            int successCount = batchResults.Count(r => r.success);
            int totalTargets = batchResults.Count;
            int totalCopiedComponents = batchResults.Sum(r => r.copiedComponents);
            int totalAddedObjects = batchResults.Sum(r => r.addedObjects);

            string title = successCount == totalTargets ? "✓ バッチ適用完了" : "⚠ バッチ適用完了（一部エラー）";
            string message = $"処理結果: {successCount}/{totalTargets} 成功\n\n";

            message += "詳細:\n";
            foreach (var result in batchResults)
            {
                string status = result.success ? "✓" : "✗";
                message += $"{status} {result.target.name}: {result.message}\n";
            }

            message += $"\n合計コピーコンポーネント数: {totalCopiedComponents}";
            message += $"\n合計追加オブジェクト数: {totalAddedObjects}";

            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
