using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace YuKoike.Tools
{
    public class SkyboxChanger : EditorWindow
{
    private List<SceneAsset> sceneList = new List<SceneAsset>();
    private Material skyboxMaterial;
    private Vector2 scrollPosition;
    private Rect dropArea;

    [MenuItem("Tools/Koike's Utils/Skybox Changer")]
    public static void ShowWindow()
    {
        GetWindow<SkyboxChanger>("Skybox Changer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Skybox Changer", EditorStyles.boldLabel);

        // 新しいスカイボックスマテリアルを選択
        EditorGUILayout.Space();
        skyboxMaterial = EditorGUILayout.ObjectField("New Skybox Material", skyboxMaterial, typeof(Material), false) as Material;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene List", EditorStyles.boldLabel);

        // ドラッグ&ドロップエリアの定義
        GUILayout.Box("Drag & Drop Scenes Here", GUILayout.ExpandWidth(true), GUILayout.Height(50));
        dropArea = GUILayoutUtility.GetLastRect();

        // ドラッグ&ドロップの処理
        HandleDragAndDrop();

        // スクロールビューで複数のシーンを表示
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // シーンリストの各要素を表示・管理
        for (int i = 0; i < sceneList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sceneList[i] = EditorGUILayout.ObjectField(sceneList[i], typeof(SceneAsset), false) as SceneAsset;

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                sceneList.RemoveAt(i);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 「Add Scene」ボタン
        if (GUILayout.Button("Add Scene"))
        {
            sceneList.Add(null);
        }

        EditorGUILayout.Space();

        // 警告メッセージ
        if (skyboxMaterial == null && sceneList.Count > 0)
        {
            EditorGUILayout.HelpBox("Please select a skybox material.", MessageType.Warning);
        }

        // 「Apply Skybox」ボタン
        EditorGUI.BeginDisabledGroup(skyboxMaterial == null || sceneList.Count == 0);
        if (GUILayout.Button("Apply Skybox to All Scenes"))
        {
            ApplySkyboxToScenes();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is SceneAsset sceneAsset)
                        {
                            // 重複チェック
                            if (!sceneList.Contains(sceneAsset))
                            {
                                sceneList.Add(sceneAsset);
                            }
                        }
                    }
                }

                evt.Use();
                break;
        }
    }

    private void ApplySkyboxToScenes()
    {
        // 現在のシーンパスを保存
        string currentScenePath = EditorSceneManager.GetActiveScene().path;

        // 未保存の変更があるか確認
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes",
                "Current scene has unsaved changes. Save before proceeding?",
                "Save", "Cancel"))
            {
                EditorSceneManager.SaveOpenScenes();
            }
            else
            {
                return; // キャンセルされた場合は処理を中止
            }
        }

        int successCount = 0;

        // 各シーンを処理
        foreach (var sceneAsset in sceneList)
        {
            if (sceneAsset == null) continue;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);

            // シーンを開く
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 前のSkyboxを記録（ログ用）
            Material oldSkybox = RenderSettings.skybox;

            // Skyboxを変更
            RenderSettings.skybox = skyboxMaterial;

            // ここが重要: 変更をマークする
            EditorSceneManager.MarkSceneDirty(scene);

            // シーンが「Dirty」になっているか確認
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                // シーンを保存
                bool saveSuccess = EditorSceneManager.SaveScene(scene);

                if (saveSuccess)
                {
                    Debug.Log($"Applied skybox to scene: {scenePath} (Changed from {oldSkybox?.name ?? "None"} to {skyboxMaterial.name})");
                    successCount++;
                }
                else
                {
                    Debug.LogError($"Failed to save scene: {scenePath}");
                }
            }
            else
            {
                Debug.LogWarning($"Scene was not marked as dirty: {scenePath}. Skybox may not have been changed.");

                // 強制的に保存を試みる
                bool forceSaveSuccess = EditorSceneManager.SaveScene(scene);
                if (forceSaveSuccess)
                {
                    Debug.Log($"Force saved scene: {scenePath}");
                    successCount++;
                }
            }
        }

        // 元のシーンに戻る
        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath);
        }

        string resultMessage = $"Skybox has been applied to {successCount} out of {sceneList.Count} scenes.";
        if (successCount < sceneList.Count)
        {
            resultMessage += " Some scenes might not have been updated properly. Check the console for details.";
        }

        EditorUtility.DisplayDialog("Skybox Changer", resultMessage, "OK");
    }
    }
}
