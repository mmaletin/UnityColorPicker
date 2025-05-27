using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(ColorPicker))]
[CanEditMultipleObjects]
public class ColorPickerEditor : Editor
{
    private SerializedProperty _colorPickerShader;

    private SerializedProperty _hueCircleInnerRadius;
    private SerializedProperty _hueSelectorInnerRadius;
    private SerializedProperty _saturationValueSquareSize;

    void OnEnable()
    {
        _colorPickerShader = serializedObject.FindProperty("_colorPickerShader");

        _hueCircleInnerRadius = serializedObject.FindProperty("_hueCircleInnerRadius");
        _hueSelectorInnerRadius = serializedObject.FindProperty("_hueSelectorInnerRadius");
        _saturationValueSquareSize = serializedObject.FindProperty("_saturationValueSquareSize");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(_colorPickerShader);
        GUI.enabled = true;

        EditorGUILayout.PropertyField(_hueCircleInnerRadius);
        EditorGUILayout.PropertyField(_hueSelectorInnerRadius);
        EditorGUILayout.PropertyField(_saturationValueSquareSize);

        foreach (var colorPicker in targets.Select(t => t as ColorPicker))
        {
            colorPicker?.ApplySizesOfElements();
        }

        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("GameObject/UI/Color Picker", false, 10)]
    private static void CreateColorPicker(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Color Picker");

        go.AddComponent<ColorPicker>();

        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }
}