using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YuKoike.Editor
{
    /// <summary>
    /// YuKoikeエディタ設定
    /// プロジェクトウィンドウやシーンエディタの動作をカスタマイズ
    /// Unity 2022.3以降対応
    /// </summary>
    [InitializeOnLoad]
    public static class YuKoikeEditorPreferences
    {
        private static readonly IList AllProjectBrowsers;
        private static bool isChangingSelection = false;

        // EditorPrefsのキー
        private const string PREF_FOLDER_FIRST_ENABLED = "YuKoike.EditorPreferences.FolderFirstEnabled";
        private const string PREF_SCENE_PRIORITY_ENABLED = "YuKoike.EditorPreferences.ScenePriorityEnabled";

        // 設定値
        internal static bool FolderFirstEnabled
        {
            get => EditorPrefs.GetBool(PREF_FOLDER_FIRST_ENABLED, true);
            set => EditorPrefs.SetBool(PREF_FOLDER_FIRST_ENABLED, value);
        }

        internal static bool ScenePriorityEnabled
        {
            get => EditorPrefs.GetBool(PREF_SCENE_PRIORITY_ENABLED, true);
            set => EditorPrefs.SetBool(PREF_SCENE_PRIORITY_ENABLED, value);
        }

        static YuKoikeEditorPreferences()
        {
            if (!ProjectBrowserFacade.IsReflectionSuccessful)
            {
                Debug.LogWarning("[YuKoikeEditorPreferences] リフレクションの初期化に失敗しました。フォルダ優先機能は無効です。");
                return;
            }

            AllProjectBrowsers = ProjectBrowserFacade.GetAllProjectBrowsers();
            EditorApplication.update += OnUpdate;

            // シーンオブジェクト優先選択機能の初期化
            if (ScenePriorityEnabled)
            {
                Selection.selectionChanged += OnSelectionChanged;
            }

            Debug.Log("[YuKoikeEditorPreferences] エディタ設定を初期化しました。");
        }

        private static void OnUpdate()
        {
            if (!FolderFirstEnabled)
                return;

            try
            {
                foreach (var browser in AllProjectBrowsers)
                {
                    ProjectBrowserFacade.SetFolderFirstForProjectWindow(browser);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[YuKoikeEditorPreferences] フォルダ優先設定の適用に失敗しました。\n{e}");
                EditorApplication.update -= OnUpdate;
            }
        }

        /// <summary>
        /// 選択が変更されたときに呼ばれる
        /// プレファブが選択されている場合、同名のシーンオブジェクトがあれば自動的にそちらを選択
        /// </summary>
        internal static void OnSelectionChanged()
        {
            // 再帰呼び出しを防ぐ
            if (isChangingSelection)
            {
                Debug.Log("[YuKoikeEditorPreferences] 再帰呼び出しを検出してスキップしました。");
                return;
            }

            if (!ScenePriorityEnabled)
            {
                Debug.Log("[YuKoikeEditorPreferences] シーンオブジェクト優先選択が無効です。");
                return;
            }

            if (Selection.activeObject == null)
            {
                Debug.Log("[YuKoikeEditorPreferences] 選択オブジェクトがnullです。");
                return;
            }

            Debug.Log($"[YuKoikeEditorPreferences] 選択されたオブジェクト: {Selection.activeObject.name} (Type: {Selection.activeObject.GetType().Name})");

            // プレファブアセットが選択されている場合
            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(Selection.activeObject);
            Debug.Log($"[YuKoikeEditorPreferences] プレファブアセットかどうか: {isPrefabAsset}");

            if (isPrefabAsset)
            {
                string objectName = Selection.activeObject.name;
                Debug.Log($"[YuKoikeEditorPreferences] 同名のシーンオブジェクトを検索: '{objectName}'");

                GameObject sceneObject = FindSceneObjectByName(objectName);

                if (sceneObject != null)
                {
                    Debug.Log($"[YuKoikeEditorPreferences] シーンオブジェクトが見つかりました: {sceneObject.name}");

                    try
                    {
                        isChangingSelection = true;

                        // シーンオブジェクトを選択
                        Selection.activeGameObject = sceneObject;
                        EditorGUIUtility.PingObject(sceneObject);
                        Debug.Log($"[YuKoikeEditorPreferences] シーン内の '{objectName}' を優先選択しました。");
                    }
                    finally
                    {
                        isChangingSelection = false;
                    }
                }
                else
                {
                    Debug.Log($"[YuKoikeEditorPreferences] シーン内に '{objectName}' という名前のオブジェクトが見つかりませんでした。");
                }
            }
        }

        /// <summary>
        /// 全シーンから指定された名前のGameObjectを検索
        /// </summary>
        private static GameObject FindSceneObjectByName(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject rootObject in rootObjects)
                {
                    if (rootObject.name == name)
                        return rootObject;

                    GameObject found = FindGameObjectRecursive(rootObject.transform, name);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Transform階層から指定された名前のGameObjectを再帰的に検索
        /// </summary>
        private static GameObject FindGameObjectRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child.gameObject;

                GameObject found = FindGameObjectRecursive(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// 設定ウィンドウを開く
        /// </summary>
        [MenuItem("Tools/YuKoike/Editor Preferences")]
        private static void OpenSettingsWindow()
        {
            YuKoikeEditorPreferencesWindow.ShowWindow();
        }

        private static class ProjectBrowserFacade
        {
            public static IList GetAllProjectBrowsers() => (IList)ProjectBrowsersField.GetValue(ProjectBrowserType);

            public static bool SetFolderFirstForProjectWindow(object projectBrowser)
            {
                if (SetOneColumnFolderFirst(projectBrowser))
                    return true;

                if (SetTwoColumnFolderFirst(projectBrowser))
                    return true;

                return false;
            }

            private static bool SetOneColumnFolderFirst(object projectBrowser)
            {
                var assetTree = AssetTreeField.GetValue(projectBrowser);
                if (assetTree == null)
                {
                    return false;
                }

                var data = AssetTreeDataProperty.GetValue(assetTree);
                if (data == null)
                {
                    return false;
                }

                if (AssetTreeFoldersFirstProperty.GetValue(data) is true)
                    return true;

                AssetTreeFoldersFirstProperty.SetValue(data, true);
                TreeViewDataSourceRefreshRowsField.SetValue(data, true);
                return true;
            }

            private static bool SetTwoColumnFolderFirst(object projectBrowser)
            {
                var listArea = ListAreaField.GetValue(projectBrowser);
                if (listArea == null)
                    return false;

                if (ListAreaFoldersFirstProperty.GetValue(listArea) is true)
                    return true;

                ListAreaFoldersFirstProperty.SetValue(listArea, true);

                TopBarSearchSettingsChangedMethod.Invoke(projectBrowser, new object[] { true });

                return true;
            }

            static ProjectBrowserFacade()
            {
                ProjectBrowserType = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.ProjectBrowser");
                TopBarSearchSettingsChangedMethod = ProjectBrowserType?.GetMethod("TopBarSearchSettingsChanged",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

                ProjectBrowsersField =
                    ProjectBrowserType?.GetField("s_ProjectBrowsers", BindingFlags.Static | BindingFlags.NonPublic);

                AssetTreeField =
                    ProjectBrowserType?.GetField("m_AssetTree", BindingFlags.Instance | BindingFlags.NonPublic);

                AssetTreeDataProperty = AssetTreeField?.FieldType.GetProperty("data");
                AssetTreeFoldersFirstProperty = Assembly.GetAssembly(typeof(UnityEditor.Editor))
                                                        .GetType("UnityEditor.AssetsTreeViewDataSource")
                                                        ?.GetProperty("foldersFirst");

                ListAreaField =
                    ProjectBrowserType?.GetField("m_ListArea", BindingFlags.Instance | BindingFlags.NonPublic);

                ListAreaFoldersFirstProperty = ListAreaField?.FieldType.GetProperty("foldersFirst");
                TreeViewDataSourceRefreshRowsField = Assembly.GetAssembly(typeof(UnityEditor.Editor))
                                                             .GetType("UnityEditor.IMGUI.Controls.TreeViewDataSource")
                                                             ?.GetField("m_NeedRefreshRows",
                                                                        BindingFlags.Instance | BindingFlags.NonPublic);

                IsReflectionSuccessful = ProjectBrowsersField != null &&
                                         AssetTreeField != null &&
                                         AssetTreeDataProperty != null &&
                                         AssetTreeFoldersFirstProperty != null &&
                                         ListAreaField != null &&
                                         ListAreaFoldersFirstProperty != null &&
                                         TreeViewDataSourceRefreshRowsField != null;

                if (!IsReflectionSuccessful)
                {
                    Debug.LogWarning("[YuKoikeEditorPreferences] リフレクションの初期化に失敗しました:\n" +
                                     $"ProjectBrowserType: {ProjectBrowserType}\n" +
                                     $"TopBarSearchSettingsChangedMethod: {TopBarSearchSettingsChangedMethod}\n" +
                                     $"ProjectBrowsersField: {ProjectBrowsersField}\n" +
                                     $"AssetTreeField: {AssetTreeField}\n" +
                                     $"AssetTreeDataProperty: {AssetTreeDataProperty}\n" +
                                     $"AssetTreeFoldersFirstProperty: {AssetTreeFoldersFirstProperty}\n" +
                                     $"ListAreaField: {ListAreaField}\n" +
                                     $"ListAreaFoldersFirstProperty: {ListAreaFoldersFirstProperty}\n" +
                                     $"TreeViewDataSourceRefreshRowsField: {TreeViewDataSourceRefreshRowsField}");
                }
            }

            private static readonly Type ProjectBrowserType;

            private static readonly MethodInfo TopBarSearchSettingsChangedMethod;

            private static readonly FieldInfo ProjectBrowsersField;

            private static readonly FieldInfo AssetTreeField;

            private static readonly FieldInfo ListAreaField;

            private static readonly PropertyInfo AssetTreeDataProperty;

            private static readonly PropertyInfo AssetTreeFoldersFirstProperty;

            private static readonly FieldInfo TreeViewDataSourceRefreshRowsField;

            private static readonly PropertyInfo ListAreaFoldersFirstProperty;

            public static readonly bool IsReflectionSuccessful;
        }
    }

    /// <summary>
    /// YuKoikeエディタ設定ウィンドウ
    /// </summary>
    public class YuKoikeEditorPreferencesWindow : EditorWindow
    {
        private bool folderFirstEnabled;
        private bool scenePriorityEnabled;

        public static void ShowWindow()
        {
            YuKoikeEditorPreferencesWindow window = GetWindow<YuKoikeEditorPreferencesWindow>("YuKoike エディタ設定");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnEnable()
        {
            // 現在の設定を読み込む
            folderFirstEnabled = YuKoikeEditorPreferences.FolderFirstEnabled;
            scenePriorityEnabled = YuKoikeEditorPreferences.ScenePriorityEnabled;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("YuKoike エディタ設定", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "プロジェクトウィンドウとHierarchyウィンドウの動作をカスタマイズします。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // フォルダ優先設定
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("フォルダ優先表示", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            bool newFolderFirstEnabled = EditorGUILayout.Toggle(
                new GUIContent("有効化", "プロジェクトウィンドウでフォルダをファイルより先に表示します"),
                folderFirstEnabled);

            if (newFolderFirstEnabled != folderFirstEnabled)
            {
                folderFirstEnabled = newFolderFirstEnabled;
                YuKoikeEditorPreferences.FolderFirstEnabled = folderFirstEnabled;

                if (folderFirstEnabled)
                {
                    Debug.Log("[YuKoikeEditorPreferences] フォルダ優先表示を有効化しました。");
                }
                else
                {
                    Debug.Log("[YuKoikeEditorPreferences] フォルダ優先表示を無効化しました。");
                }
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(
                "プロジェクトウィンドウでフォルダを優先的に表示します。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // シーンオブジェクト優先選択設定
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("シーンオブジェクト優先選択", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            bool newScenePriorityEnabled = EditorGUILayout.Toggle(
                new GUIContent("有効化", "プレファブと同名のシーンオブジェクトがある場合、シーンオブジェクトを優先的に選択します"),
                scenePriorityEnabled);

            if (newScenePriorityEnabled != scenePriorityEnabled)
            {
                scenePriorityEnabled = newScenePriorityEnabled;
                YuKoikeEditorPreferences.ScenePriorityEnabled = scenePriorityEnabled;

                // イベントリスナーの登録/解除
                if (scenePriorityEnabled)
                {
                    Selection.selectionChanged -= YuKoikeEditorPreferences.OnSelectionChanged;
                    Selection.selectionChanged += YuKoikeEditorPreferences.OnSelectionChanged;
                    Debug.Log("[YuKoikeEditorPreferences] シーンオブジェクト優先選択を有効化しました。");
                }
                else
                {
                    Selection.selectionChanged -= YuKoikeEditorPreferences.OnSelectionChanged;
                    Debug.Log("[YuKoikeEditorPreferences] シーンオブジェクト優先選択を無効化しました。");
                }
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField(
                "プレファブアセットを選択したときに、同名のシーンオブジェクトがあれば自動的にそちらを選択します。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 設定をリセット
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("デフォルトに戻す", GUILayout.Width(150)))
            {
                if (EditorUtility.DisplayDialog(
                    "設定をリセット",
                    "すべての設定をデフォルト値に戻しますか？",
                    "リセット", "キャンセル"))
                {
                    folderFirstEnabled = true;
                    scenePriorityEnabled = true;
                    YuKoikeEditorPreferences.FolderFirstEnabled = folderFirstEnabled;
                    YuKoikeEditorPreferences.ScenePriorityEnabled = scenePriorityEnabled;

                    Selection.selectionChanged -= YuKoikeEditorPreferences.OnSelectionChanged;
                    Selection.selectionChanged += YuKoikeEditorPreferences.OnSelectionChanged;

                    Debug.Log("[YuKoikeEditorPreferences] 設定をデフォルトに戻しました。");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }
    }
}
