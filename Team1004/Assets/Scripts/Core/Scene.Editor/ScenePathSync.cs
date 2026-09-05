using System;
using UnityEditor;

internal static class ScenePathSync
{
    public static bool Synchronize(SerializedProperty pathProperty, SerializedProperty guidProperty)
    {
        if (string.IsNullOrWhiteSpace(guidProperty.stringValue))
            return false;

        var resolvedPath = AssetDatabase.GUIDToAssetPath(guidProperty.stringValue);
        if (string.IsNullOrWhiteSpace(resolvedPath) ||
            string.Equals(resolvedPath, pathProperty.stringValue, StringComparison.Ordinal))
            return false;

        pathProperty.stringValue = resolvedPath;
        return true;
    }

    public static bool SynchronizeAll(SerializedObject serializedObject)
    {
        var changed = false;
        var iterator = serializedObject.GetIterator();

        while (iterator.Next(true))
        {
            if (iterator.propertyType != SerializedPropertyType.Generic)
                continue;

            var pathProperty = iterator.FindPropertyRelative("path");
            var guidProperty = iterator.FindPropertyRelative("guid");
            if (pathProperty == null || guidProperty == null)
                continue;

            if (pathProperty.propertyType != SerializedPropertyType.String ||
                guidProperty.propertyType != SerializedPropertyType.String)
                continue;

            changed |= Synchronize(pathProperty, guidProperty);
        }

        return changed;
    }
}
