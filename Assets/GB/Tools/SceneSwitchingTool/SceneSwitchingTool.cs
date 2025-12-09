// ============================================
// 版权：Copyright (c) 2025 GB. All rights reserved.
// 文件名称：SceneSwitchingTool.cs
// 作者：GB
// 创建时间：2025-12-07 21:55:32
// 修改时间：2025-12-07 21:55:32
// 版本：1.0.0
// 描述：场景切换工具
// Github：https://github.com/Gaobobo-C/SceneSwitchingTool.git
// ============================================


using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GB.Tools
{
    public class SceneSwitchingTool : EditorWindow
    {
        [MenuItem("GB Tools/场景切换工具/配置")]
        public static void OpenWindow()
        {
            SceneSwitchingTool window = GetWindow<SceneSwitchingTool>("场景切换工具配置");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        #region 私有字段

        /// <summary> 配置信息 </summary>
        private SceneSwitchingConfig config;

        /// <summary> 配置文件路径 </summary>
        private string configFilePath;

        /// <summary> 生成菜单脚本路径 </summary>
        private string menuScriptPath;

        /// <summary> 所有场景资源缓存 </summary>
        private Dictionary<string, SceneAsset> sceneCache = new Dictionary<string, SceneAsset>();

        /// <summary> 滚动视图位置 </summary>
        private Vector2 scrollPosition;

        /// <summary> ReorderableList实例 </summary>
        private ReorderableList reorderableList;

        /// <summary> 当前脚本所在目录 </summary>
        private string scriptDirectory;

        #endregion

        #region Unity 生命周期

        private void OnEnable()
        {
            // 先获取当前脚本所在目录
            GetCurrentScriptDirectory();
            InitializePaths();
            LoadConfiguration();
            RefreshSceneCache();
            InitializeReorderableList();
        }

        private void OnDisable()
        {
            SaveConfiguration();
        }

        public void OnGUI()
        {
            DrawHeader();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                DrawSceneList();
            }
            EditorGUILayout.EndScrollView();

            DrawActionButtons();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 获取当前脚本所在目录
        /// </summary>
        private void GetCurrentScriptDirectory()
        {
            try
            {
                // 获取当前编辑器窗口的MonoScript
                MonoScript monoScript = MonoScript.FromScriptableObject(this);
                if (monoScript != null)
                {
                    string scriptPath = AssetDatabase.GetAssetPath(monoScript);
                    scriptDirectory = Path.GetDirectoryName(scriptPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取脚本目录失败，将使用默认路径: {e.Message}");
                scriptDirectory = Path.Combine(Application.dataPath, "Editor");
            }
        }

        /// <summary>
        /// 初始化路径
        /// </summary>
        private void InitializePaths()
        {
            if (string.IsNullOrEmpty(scriptDirectory))
            {
                scriptDirectory = Path.Combine(Application.dataPath, "Editor");
            }

            // 配置文件路径：当前脚本同级下"Resources/Config/SceneSwitchingConfig.json"
            string configRelativePath = "Resources/Config";
            string configFullPath = Path.Combine(scriptDirectory, configRelativePath);
            if (!Directory.Exists(configFullPath))
            {
                Directory.CreateDirectory(configFullPath);
            }
            configFilePath = Path.Combine(configFullPath, "SceneSwitchingConfig.json");

            // 菜单脚本路径：当前脚本同级下"SceneSwitchingMenu.cs"
            menuScriptPath = Path.Combine(scriptDirectory, "SceneSwitchingMenu.cs");
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath, Encoding.UTF8);
                    config = JsonUtility.FromJson<SceneSwitchingConfig>(json);

                    if (config == null)
                    {
                        config = new SceneSwitchingConfig();
                    }
                    else if (config.SceneShortcuts == null)
                    {
                        config.SceneShortcuts = new List<SceneShortcut>();
                    }
                }
                else
                {
                    config = new SceneSwitchingConfig();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载配置文件失败: {e.Message}");
                config = new SceneSwitchingConfig();
            }
        }

        /// <summary>
        /// 刷新场景缓存
        /// </summary>
        private void RefreshSceneCache()
        {
            sceneCache.Clear();

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            if (sceneGuids == null || sceneGuids.Length == 0)
            {
                return;
            }

            foreach (string guid in sceneGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath);

                if (scene != null && !sceneCache.ContainsKey(scene.name))
                {
                    sceneCache.Add(scene.name, scene);
                }
            }
        }

        /// <summary>
        /// 初始化ReorderableList
        /// </summary>
        private void InitializeReorderableList()
        {
            reorderableList = new ReorderableList(config.SceneShortcuts, typeof(SceneShortcut), true, true, true, true);

            // 绘制列表头部
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "快捷切换列表");
            };

            // 绘制每个元素
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= config.SceneShortcuts.Count) return;

                rect.y += 2;
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing = 5f;

                // 序号显示
                Rect numberRect = new Rect(rect.x, rect.y, 30, lineHeight);
                EditorGUI.LabelField(numberRect, $"{index + 1}.");

                // 菜单标签
                Rect labelRect = new Rect(rect.x + 20, rect.y, 60, lineHeight);
                EditorGUI.LabelField(labelRect, "标签名:");

                Rect labelFieldRect = new Rect(rect.x + 70, rect.y, 200, lineHeight);
                config.SceneShortcuts[index].MenuLabel = EditorGUI.TextField(labelFieldRect, config.SceneShortcuts[index].MenuLabel);

                // 目标场景
                Rect sceneLabelRect = new Rect(rect.x + 280, rect.y, 60, lineHeight);
                EditorGUI.LabelField(sceneLabelRect, "目标场景:");

                string currentSceneName = config.SceneShortcuts[index].SceneName;
                SceneAsset currentScene = sceneCache.ContainsKey(currentSceneName)
                    ? sceneCache[currentSceneName]
                    : null;

                Rect sceneFieldRect = new Rect(rect.x + 345, rect.y, rect.width - 345, lineHeight);
                SceneAsset selectedScene = (SceneAsset)EditorGUI.ObjectField(sceneFieldRect, currentScene, typeof(SceneAsset), false);

                if (selectedScene != null && selectedScene != currentScene)
                {
                    config.SceneShortcuts[index].SceneName = selectedScene.name;
                }
                else if (selectedScene == null && currentScene != null)
                {
                    config.SceneShortcuts[index].SceneName = "";
                }
            };

            // 设置元素高度
            reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 6;

            // 添加元素回调
            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                config.SceneShortcuts.Add(new SceneShortcut
                {
                    MenuLabel = $"场景{config.SceneShortcuts.Count + 1}",
                    SceneName = ""
                });
            };

            // 删除元素回调
            reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                if (list.index >= 0 && list.index < config.SceneShortcuts.Count)
                {
                    if (EditorUtility.DisplayDialog("确认删除",
                        $"确定要删除场景快捷方式 '{config.SceneShortcuts[list.index].MenuLabel}' 吗？", "删除", "取消"))
                    {
                        config.SceneShortcuts.RemoveAt(list.index);
                    }
                }
            };
        }

        #endregion

        #region UI绘制方法

        /// <summary>
        /// 绘制头部信息
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("场景切换工具配置", EditorStyles.boldLabel, GUILayout.Height(30));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            // 场景数量统计信息
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"(项目场景总数: {sceneCache.Count}   已添加场景: {config.SceneShortcuts.Count})", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// 绘制场景列表
        /// </summary>
        private void DrawSceneList()
        {
            if (config.SceneShortcuts == null || config.SceneShortcuts.Count == 0)
            {
                // 当场景列表为空时显示提示和添加按钮
                EditorGUILayout.HelpBox("暂无场景快捷方式配置", MessageType.Info);
                EditorGUILayout.Space(10);

                // 居中添加按钮
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
                    if (GUILayout.Button("+ 添加第一个场景快捷方式", GUILayout.Width(180), GUILayout.Height(35)))
                    {
                        AddSceneShortcut();
                    }
                    GUI.backgroundColor = Color.white;

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);
                return;
            }

            // 绘制ReorderableList
            reorderableList.DoLayoutList();
        }

        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                // 只有当有场景配置时才显示保存按钮
                if (config.SceneShortcuts.Count > 0)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                    if (GUILayout.Button("保存配置", GUILayout.Width(120), GUILayout.Height(35)))
                    {
                        GenerateSceneMenu();
                    }
                    GUI.backgroundColor = Color.white;
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        #endregion

        #region 操作方法

        /// <summary>
        /// 添加快捷方式
        /// </summary>
        private void AddSceneShortcut()
        {
            if (config.SceneShortcuts == null)
            {
                config.SceneShortcuts = new List<SceneShortcut>();
            }

            var newShortcut = new SceneShortcut
            {
                MenuLabel = $"场景{config.SceneShortcuts.Count + 1}",
                SceneName = ""
            };

            config.SceneShortcuts.Add(newShortcut);
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfiguration()
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(configFilePath, json, Encoding.UTF8);

                AssetDatabase.Refresh();
                Debug.Log($"配置已保存: {configFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"保存配置失败: {e.Message}");
            }
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (config == null || config.SceneShortcuts == null)
            {
                Debug.LogError("配置数据为空");
                return false;
            }

            // 检查是否有重复的菜单标签
            var menuLabels = new HashSet<string>();
            for (int i = 0; i < config.SceneShortcuts.Count; i++)
            {
                var shortcut = config.SceneShortcuts[i];

                if (string.IsNullOrWhiteSpace(shortcut.MenuLabel))
                {
                    Debug.LogError($"第 {i + 1} 个快捷方式的菜单标签不能为空");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(shortcut.SceneName))
                {
                    Debug.LogError($"第 {i + 1} 个快捷方式的场景未选择");
                    return false;
                }

                if (!sceneCache.ContainsKey(shortcut.SceneName))
                {
                    Debug.LogError($"第 {i + 1} 个快捷方式的场景 '{shortcut.SceneName}' 不存在");
                    return false;
                }

                if (menuLabels.Contains(shortcut.MenuLabel))
                {
                    Debug.LogError($"第 {i + 1} 个快捷方式的菜单标签 '{shortcut.MenuLabel}' 重复");
                    return false;
                }

                menuLabels.Add(shortcut.MenuLabel);
            }

            return true;
        }

        /// <summary>
        /// 生成场景菜单
        /// </summary>
        private void GenerateSceneMenu()
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            try
            {
                string scriptContent = BuildMenuScript();
                File.WriteAllText(menuScriptPath, scriptContent, Encoding.UTF8);

                AssetDatabase.Refresh();
                Debug.Log($"场景切换菜单已生成: {menuScriptPath}");

                // 保存配置
                SaveConfiguration();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"生成场景菜单失败: {e.Message}");
            }
        }

        /// <summary>
        /// 构建菜单脚本
        /// </summary>
        private string BuildMenuScript()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using UnityEditor;");
            sb.AppendLine("using UnityEditor.SceneManagement;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace GB");;
            sb.AppendLine("{");
            sb.AppendLine("    public static class SceneSwitchingMenu");
            sb.AppendLine("    {");

            // 生成菜单项
            for (int i = 0; i < config.SceneShortcuts.Count; i++)
            {
                var shortcut = config.SceneShortcuts[i];

                if (sceneCache.TryGetValue(shortcut.SceneName, out SceneAsset sceneAsset))
                {
                    string scenePath = AssetDatabase.GetAssetPath(sceneAsset);

                    sb.AppendLine($"        [MenuItem(\"GB Tools/场景切换工具/快速切换/{shortcut.MenuLabel}\", priority = {100 + i})]");
                    sb.AppendLine($"        private static void SwitchTo_{i}()");
                    sb.AppendLine("        {");
                    sb.AppendLine("            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                EditorSceneManager.OpenScene(\"{scenePath}\");");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// 场景切换工具配置
    /// </summary>
    [System.Serializable]
    public class SceneSwitchingConfig
    {
        public List<SceneShortcut> SceneShortcuts = new List<SceneShortcut>();
    }

    /// <summary>
    /// 场景快捷方式
    /// </summary>
    [System.Serializable]
    public class SceneShortcut
    {
        [Tooltip("在菜单中显示的标签")]
        public string MenuLabel = "";

        [Tooltip("目标场景名称")]
        public string SceneName = "";
    }
}