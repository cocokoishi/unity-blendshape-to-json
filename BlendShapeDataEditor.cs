#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class BlendShapeDataEditor : EditorWindow
{
    // 用于序列化 JSON 的数据结构
    [Serializable]
    public class BlendShapeItem
    {
        public string name;
        public float weight;
    }

    [Serializable]
    public class BlendShapeCollection
    {
        public List<BlendShapeItem> blendShapes = new List<BlendShapeItem>();
    }

    private SkinnedMeshRenderer targetRenderer;
    private string jsonData = "";
    private Vector2 scrollPosition;

    // 在顶部菜单栏创建入口
    [MenuItem("Tools/BlendShape To Json")]
    public static void ShowWindow()
    {
        // 呼出自定义窗口
        GetWindow<BlendShapeDataEditor>("BlendShape Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("BlendShape JSON EXPORT/IMPORT", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. 拖入目标框
        targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
            "Drag into SkinnedMeshRenderer", 
            targetRenderer, 
            typeof(SkinnedMeshRenderer), 
            true);

        EditorGUILayout.Space();

        if (targetRenderer == null)
        {
            EditorGUILayout.HelpBox("Drag the character with the SkinnedMeshRenderer into the box above.", MessageType.Info);
            return;
        }

        if (targetRenderer.sharedMesh == null)
        {
            EditorGUILayout.HelpBox("No mesh data found", MessageType.Warning);
            return;
        }

        // 2. 读取/导出按钮
        if (GUILayout.Button("BlendShapes Export to JSON)", GUILayout.Height(30)))
        {
            ReadDataToJson();
        }

        EditorGUILayout.Space();

        // 3. 结构化数据文本框 (包含滚动条)
        GUILayout.Label("Structured Data");
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(350));
        
        // 使用 TextArea 显示和编辑多行文本
        jsonData = EditorGUILayout.TextArea(jsonData, GUILayout.ExpandHeight(true));
        
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 4. 反向应用按钮
        if (GUILayout.Button("⬆️ Apply Json data to mesh blendshape", GUILayout.Height(30)))
        {
            ApplyJsonToMesh();
        }
    }

    private void ReadDataToJson()
    {
        Mesh mesh = targetRenderer.sharedMesh;
        BlendShapeCollection collection = new BlendShapeCollection();

        // 遍历提取所有的 BlendShape 名字和权重
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            collection.blendShapes.Add(new BlendShapeItem
            {
                name = mesh.GetBlendShapeName(i),
                weight = targetRenderer.GetBlendShapeWeight(i)
            });
        }

        // JsonUtility.ToJson 第二个参数设为 true，以漂亮的多行缩进格式输出
        jsonData = JsonUtility.ToJson(collection, true);
        
        // 取消 UI 焦点，确保文本框能立即刷新
        GUI.FocusControl(null); 
    }

    private void ApplyJsonToMesh()
    {
        if (string.IsNullOrEmpty(jsonData)) return;

        try
        {
            // 解析框内的 JSON 文本
            BlendShapeCollection collection = JsonUtility.FromJson<BlendShapeCollection>(jsonData);
            Mesh mesh = targetRenderer.sharedMesh;

            // 注册 Undo 操作，方便你按 Ctrl+Z 撤销修改
            Undo.RecordObject(targetRenderer, "Apply BlendShape JSON");

            // 遍历 JSON 数据并应用
            foreach (var item in collection.blendShapes)
            {
                // 通过名字反查 Index，这样即使模型的 BlendShape 顺序变了也能正确应用
                int index = mesh.GetBlendShapeIndex(item.name);
                if (index != -1)
                {
                    targetRenderer.SetBlendShapeWeight(index, item.weight);
                }
                else
                {
                    Debug.LogWarning($"The BlendShape named '{item.name}' was not found and has been skipped.");
                }
            }
            
            Debug.Log("✅ BlendShape Applied！");
        }
        catch (Exception e)
        {
            // 如果你在框里手动修改 JSON 时不小心漏了逗号或引号，会在这里报错
            Debug.LogError("❌ JSON Error syntax: " + e.Message);
        }
    }
}
#endif