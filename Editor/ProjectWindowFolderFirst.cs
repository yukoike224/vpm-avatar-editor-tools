using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YuKoike.Editor
{
    /// <summary>
    /// プロジェクトウィンドウでフォルダを優先的に表示するエディタ拡張
    /// Unity 2022.3以降対応
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowFolderFirst
    {
        private static readonly IList AllProjectBrowsers;

        static ProjectWindowFolderFirst()
        {
            if (!ProjectBrowserFacade.IsReflectionSuccessful)
            {
                Debug.LogWarning("[ProjectWindowFolderFirst] リフレクションの初期化に失敗しました。フォルダ優先機能は無効です。");
                return;
            }

            AllProjectBrowsers = ProjectBrowserFacade.GetAllProjectBrowsers();
            EditorApplication.update += OnUpdate;

            Debug.Log("[ProjectWindowFolderFirst] フォルダ優先機能を有効化しました。");
        }

        private static void OnUpdate()
        {
            try
            {
                foreach (var browser in AllProjectBrowsers)
                {
                    ProjectBrowserFacade.SetFolderFirstForProjectWindow(browser);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectWindowFolderFirst] フォルダ優先設定の適用に失敗しました。\n{e}");
                EditorApplication.update -= OnUpdate;
            }
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
                    Debug.LogWarning("[ProjectWindowFolderFirst] リフレクションの初期化に失敗しました:\n" +
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
}
