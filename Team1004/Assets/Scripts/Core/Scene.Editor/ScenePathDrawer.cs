using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ScenePath))]
public sealed class ScenePathDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var path = property.FindPropertyRelative("path").stringValue;
        return IsIncluded(path)
            ? EditorGUIUtility.singleLineHeight
            : EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var pathProperty = property.FindPropertyRelative("path");
        var guidProperty = property.FindPropertyRelative("guid");
        ScenePathSync.Synchronize(pathProperty, guidProperty);

        EditorGUI.BeginProperty(position, label, property);

        var fieldRect = new Rect(
            position.x,
            position.y,
            position.width,
            EditorGUIUtility.singleLineHeight);
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathProperty.stringValue);

        EditorGUI.BeginChangeCheck();
        var selected = EditorGUI.ObjectField(fieldRect, label, sceneAsset, typeof(SceneAsset), false) as SceneAsset;
        if (EditorGUI.EndChangeCheck())
        {
            if (selected == null)
            {
                pathProperty.stringValue = string.Empty;
                guidProperty.stringValue = string.Empty;
            }
            else
            {
                pathProperty.stringValue = AssetDatabase.GetAssetPath(selected);
                guidProperty.stringValue = AssetDatabase.AssetPathToGUID(pathProperty.stringValue);
            }
        }

        if (!IsIncluded(pathProperty.stringValue))
        {
            var helpRect = new Rect(
                position.x,
                fieldRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(helpRect, "활성 Build Profile에 포함되지 않은 씬입니다.", MessageType.Warning);
        }

        EditorGUI.EndProperty();
    }

    private static bool IsIncluded(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        return EditorBuildSettings.scenes.Any(scene =>
            scene.enabled && string.Equals(scene.path, path, StringComparison.OrdinalIgnoreCase));
    }
}
