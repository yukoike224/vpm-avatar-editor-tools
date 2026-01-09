using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace YuKoike.Tools
{
    /// <summary>
    /// lilToonマテリアルのVRChat Custom Safety Fallbackを一括設定するツール
    /// </summary>
    public class VRCFallbackSetter : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<Material> targetMaterials = new List<Material>();
        private Dictionary<Material, bool> materialSelectionStates = new Dictionary<Material, bool>();
        private Dictionary<Material, string> materialFallbackTypes = new Dictionary<Material, string>();
        private bool selectAll = true;
        private GameObject targetAvatar = null;
        private bool ignoreEditorOnly = true;
        private int batchFallbackIndex = 0;
        private bool showAllMaterials = false;

        [MenuItem("Tools/Koike's Utils/VRChat Fallback Setter")]
        public static void ShowWindow()
        {
            GetWindow<VRCFallbackSetter>("VRChat Fallback Setter");
        }

        private void OnEnable()
        {
            ScanMaterials();
        }

        private void OnGUI()
        {
            GUILayout.Label("VRChat Custom Safety Fallback一括設定ツール", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Assets以下のlilToonマテリアルでVRChat Custom Safety Fallbackが未設定または不適切なものを検出し、一括で設定します。\n通常のlilToon→Unlit、Transparent系→Unlit/Transparent、ToonStandard、ToonStandardOutlineが選択可能です。", MessageType.Info);
            EditorGUILayout.Space();

            // アバター指定フィールド
            EditorGUILayout.LabelField("スキャン対象設定", EditorStyles.boldLabel);
            GameObject newTargetAvatar = (GameObject)EditorGUILayout.ObjectField(
                "対象アバター (オプション)",
                targetAvatar,
                typeof(GameObject),
                true
            );

            if (newTargetAvatar != targetAvatar)
            {
                targetAvatar = newTargetAvatar;
            }

            if (targetAvatar != null)
            {
                EditorGUILayout.HelpBox("指定されたアバターに使用されているマテリアルのみをスキャンします。", MessageType.Info);

                // EditorOnly無視オプション
                ignoreEditorOnly = EditorGUILayout.Toggle("EditorOnlyを無視", ignoreEditorOnly);
                if (ignoreEditorOnly)
                {
                    EditorGUILayout.HelpBox("EditorOnlyタグが付いているゲームオブジェクトのマテリアルは除外されます。", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("アバター未指定時はAssets以下のすべてのマテリアルをスキャンします。", MessageType.Info);
            }

            EditorGUILayout.Space();

            // スキャンオプション
            showAllMaterials = EditorGUILayout.Toggle("適切に設定済みのマテリアルも表示", showAllMaterials);
            if (showAllMaterials)
            {
                EditorGUILayout.HelpBox("VRCFallbackが適切に設定されているマテリアルも含めて表示します。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("VRCFallbackが未設定または不適切なマテリアルのみを表示します。", MessageType.Info);
            }
            EditorGUILayout.Space();

            // 再スキャンボタン
            if (GUILayout.Button("マテリアルを再スキャン"))
            {
                ScanMaterials();
            }
            EditorGUILayout.Space();

            // マテリアルリスト表示
            if (targetMaterials.Count > 0)
            {
                EditorGUILayout.LabelField($"VRCFallback未設定または不適切なマテリアル: {targetMaterials.Count}件", EditorStyles.boldLabel);

                // 全選択/解除チェックボックス
                EditorGUILayout.BeginHorizontal();
                bool newSelectAll = EditorGUILayout.Toggle("すべて選択", selectAll);
                if (newSelectAll != selectAll)
                {
                    selectAll = newSelectAll;
                    foreach (var mat in targetMaterials)
                    {
                        materialSelectionStates[mat] = selectAll;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                // 選択したマテリアルへの一括フォールバック設定
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("選択中のマテリアルに一括設定:", GUILayout.Width(200));
                string[] batchFallbackOptions = new string[] { "Unlit", "Unlit/Transparent", "ToonStandard", "ToonStandardOutline" };
                batchFallbackIndex = EditorGUILayout.Popup(batchFallbackIndex, batchFallbackOptions, GUILayout.Width(150));
                if (GUILayout.Button("適用", GUILayout.Width(60)))
                {
                    string selectedFallback = batchFallbackOptions[batchFallbackIndex];
                    foreach (var mat in targetMaterials)
                    {
                        if (materialSelectionStates.ContainsKey(mat) && materialSelectionStates[mat])
                        {
                            materialFallbackTypes[mat] = selectedFallback;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                // スクロールビュー
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true), GUILayout.MinHeight(200));

                foreach (var mat in targetMaterials)
                {
                    if (mat == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    // チェックボックス
                    if (!materialSelectionStates.ContainsKey(mat))
                    {
                        materialSelectionStates[mat] = true;
                    }
                    materialSelectionStates[mat] = EditorGUILayout.Toggle(materialSelectionStates[mat], GUILayout.Width(20));

                    // マテリアル名とパス
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);

                    // 個別のフォールバックタイプ設定
                    if (!materialFallbackTypes.ContainsKey(mat))
                    {
                        materialFallbackTypes[mat] = GetExpectedFallbackType(mat.shader.name);
                    }

                    // フォールバックタイプをポップアップで選択
                    string[] fallbackOptions = new string[] { "Unlit", "Unlit/Transparent", "ToonStandard", "ToonStandardOutline" };
                    int selectedIndex = System.Array.IndexOf(fallbackOptions, materialFallbackTypes[mat]);
                    if (selectedIndex < 0) selectedIndex = 0;

                    selectedIndex = EditorGUILayout.Popup(selectedIndex, fallbackOptions, GUILayout.Width(150));
                    materialFallbackTypes[mat] = fallbackOptions[selectedIndex];

                    // 現在の設定を表示
                    string currentFallback = mat.GetTag("VRCFallback", false, "");
                    if (!string.IsNullOrEmpty(currentFallback))
                    {
                        EditorGUILayout.LabelField($"(現在: {currentFallback})", GUILayout.Width(200));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("(未設定)", GUILayout.Width(200));
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space();

                // 選択されたマテリアルの数を表示
                int selectedCount = materialSelectionStates.Count(kvp => kvp.Value && targetMaterials.Contains(kvp.Key));
                EditorGUILayout.LabelField($"選択中: {selectedCount}件");
                EditorGUILayout.Space();

                // 適用ボタン
                EditorGUI.BeginDisabledGroup(selectedCount == 0);
                if (GUILayout.Button("選択したマテリアルにVRCFallbackを設定", GUILayout.Height(30)))
                {
                    ApplyFallbackToSelectedMaterials();
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("VRCFallback未設定または不適切なlilToonマテリアルが見つかりませんでした。", MessageType.Info);
            }
        }

        private void ScanMaterials()
        {
            targetMaterials.Clear();
            materialSelectionStates.Clear();
            materialFallbackTypes.Clear();

            if (targetAvatar != null)
            {
                ScanAvatarMaterials();
            }
            else
            {
                ScanAllMaterials();
            }
        }

        /// <summary>
        /// 指定されたアバターに使用されているマテリアルをスキャン
        /// </summary>
        private void ScanAvatarMaterials()
        {
            KoikeEditorUtility.ShowProgressBar("マテリアルスキャン中", "アバターのマテリアルを検索しています...", 0f);

            try
            {
                // アバターから使用されているマテリアルを取得
                HashSet<Material> avatarMaterials = new HashSet<Material>();

                // SkinnedMeshRenderer からマテリアルを取得
                SkinnedMeshRenderer[] skinnedMeshRenderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in skinnedMeshRenderers)
                {
                    // EditorOnly チェック
                    if (ignoreEditorOnly && smr.gameObject.tag == "EditorOnly")
                    {
                        continue;
                    }

                    foreach (var mat in smr.sharedMaterials)
                    {
                        if (mat != null)
                        {
                            avatarMaterials.Add(mat);
                        }
                    }
                }

                // MeshRenderer からマテリアルを取得
                MeshRenderer[] meshRenderers = targetAvatar.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in meshRenderers)
                {
                    // EditorOnly チェック
                    if (ignoreEditorOnly && mr.gameObject.tag == "EditorOnly")
                    {
                        continue;
                    }

                    foreach (var mat in mr.sharedMaterials)
                    {
                        if (mat != null)
                        {
                            avatarMaterials.Add(mat);
                        }
                    }
                }

                int totalCount = avatarMaterials.Count;
                int processedCount = 0;

                foreach (var mat in avatarMaterials)
                {
                    processedCount++;
                    KoikeEditorUtility.ShowProgressBar("マテリアルスキャン中",
                        $"処理中: {processedCount}/{totalCount}\n{mat.name}",
                        (float)processedCount / totalCount);

                    // lilToonシェーダーかチェック
                    if (mat.shader == null || !mat.shader.name.Contains("lilToon")) continue;

                    // すべて表示モードの場合は全マテリアルを追加、そうでない場合は条件付き
                    if (showAllMaterials || ShouldIncludeMaterial(mat))
                    {
                        targetMaterials.Add(mat);
                        materialSelectionStates[mat] = showAllMaterials ? false : true; // すべて表示時はデフォルト未選択
                        materialFallbackTypes[mat] = GetExpectedFallbackType(mat.shader.name);
                    }
                }

                string message = showAllMaterials
                    ? $"スキャン完了: {targetMaterials.Count}件のlilToonマテリアルが見つかりました。"
                    : $"スキャン完了: {targetMaterials.Count}件のVRCFallback未設定または不適切なマテリアルが見つかりました。";
                Debug.Log(message);
            }
            finally
            {
                KoikeEditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Assets以下のすべてのマテリアルをスキャン
        /// </summary>
        private void ScanAllMaterials()
        {
            KoikeEditorUtility.ShowProgressBar("マテリアルスキャン中", "lilToonマテリアルを検索しています...", 0f);

            try
            {
                // Assets以下のすべてのマテリアルを取得
                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
                int totalCount = guids.Length;
                int processedCount = 0;

                foreach (string guid in guids)
                {
                    processedCount++;
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    KoikeEditorUtility.ShowProgressBar("マテリアルスキャン中",
                        $"処理中: {processedCount}/{totalCount}\n{path}",
                        (float)processedCount / totalCount);

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    // lilToonシェーダーかチェック
                    if (mat.shader == null || !mat.shader.name.Contains("lilToon")) continue;

                    // すべて表示モードの場合は全マテリアルを追加、そうでない場合は条件付き
                    if (showAllMaterials || ShouldIncludeMaterial(mat))
                    {
                        targetMaterials.Add(mat);
                        materialSelectionStates[mat] = showAllMaterials ? false : true; // すべて表示時はデフォルト未選択
                        materialFallbackTypes[mat] = GetExpectedFallbackType(mat.shader.name);
                    }
                }

                string message = showAllMaterials
                    ? $"スキャン完了: {targetMaterials.Count}件のlilToonマテリアルが見つかりました。"
                    : $"スキャン完了: {targetMaterials.Count}件のVRCFallback未設定または不適切なマテリアルが見つかりました。";
                Debug.Log(message);
            }
            finally
            {
                KoikeEditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// マテリアルをスキャン対象に含めるかどうかを判定
        /// </summary>
        private bool ShouldIncludeMaterial(Material mat)
        {
            // VRCFallbackタグがすでに設定されているかチェック
            string currentFallback = mat.GetTag("VRCFallback", false, "");

            // 未設定または、OpaqueとTransparentの不一致をチェック
            if (string.IsNullOrEmpty(currentFallback))
            {
                return true;
            }

            // UnlitDoubleSidedが設定されている場合は常に適切とみなす
            if (currentFallback == "UnlitDoubleSided")
            {
                return false;
            }

            // シェーダー名に基づいて期待されるフォールバックタイプを判定
            string expectedFallback = GetExpectedFallbackType(mat.shader.name);
            bool isCurrentTransparent = currentFallback.ToLower().Contains("transparent") ||
                                        currentFallback.ToLower().Contains("cutout");
            bool isExpectedTransparent = expectedFallback.ToLower().Contains("transparent");

            // OpaqueとTransparentが一致しない場合も対象に含める
            return isCurrentTransparent != isExpectedTransparent;
        }

        /// <summary>
        /// シェーダー名に基づいて適切なフォールバックタイプを判定
        /// </summary>
        private string GetExpectedFallbackType(string shaderName)
        {
            // Transparent系のシェーダーの場合
            if (shaderName.Contains("Transparent") || 
                shaderName.Contains("Cutout") || 
                shaderName.Contains("OnePassTransparent") ||
                shaderName.Contains("TwoPassTransparent"))
            {
                return "Unlit/Transparent";
            }
            
            // その他のOpaque系シェーダー
            return "Unlit";
        }

        private void ApplyFallbackToSelectedMaterials()
        {

            List<Material> selectedMaterials = targetMaterials
                .Where(mat => mat != null && materialSelectionStates.ContainsKey(mat) && materialSelectionStates[mat])
                .ToList();

            if (selectedMaterials.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "マテリアルが選択されていません。", "OK");
                return;
            }

            int successCount = 0;

            KoikeEditorUtility.ShowProgressBar("VRCFallback設定中", "処理を開始しています...", 0f);

            try
            {
                Undo.RecordObjects(selectedMaterials.ToArray(), "Set VRCFallback");

                for (int i = 0; i < selectedMaterials.Count; i++)
                {
                    Material mat = selectedMaterials[i];
                    string path = AssetDatabase.GetAssetPath(mat);

                    KoikeEditorUtility.ShowProgressBar("VRCFallback設定中",
                        $"処理中: {i + 1}/{selectedMaterials.Count}\n{mat.name}",
                        (float)(i + 1) / selectedMaterials.Count);

                    try
                    {
                        // マテリアルごとのフォールバックタイプを取得
                        string materialSpecificFallback = materialFallbackTypes.ContainsKey(mat) 
                            ? materialFallbackTypes[mat] 
                            : GetExpectedFallbackType(mat.shader.name);
                        
                        // VRCFallbackタグを設定
                        mat.SetOverrideTag("VRCFallback", materialSpecificFallback);

                        // 変更を保存
                        EditorUtility.SetDirty(mat);

                        successCount++;
                        Debug.Log($"VRCFallbackを設定しました: {mat.name} ({path}) -> {materialSpecificFallback}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"VRCFallback設定失敗: {mat.name} ({path})\n{e.Message}");
                    }
                }

                // アセットデータベースをリフレッシュ
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 結果表示
                KoikeEditorUtility.ShowResultDialog(
                    successCount > 0,
                    "VRCFallback設定完了",
                    $"VRCFallbackの設定が完了しました。",
                    selectedMaterials.Count,
                    successCount
                );

                // 成功したら再スキャン
                if (successCount > 0)
                {
                    ScanMaterials();
                }
            }
            finally
            {
                KoikeEditorUtility.ClearProgressBar();
            }
        }
    }
}