using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(CustomButton))]
public class CustomButtonEditor : ButtonEditor
{
    SerializedProperty frameImageProp;
    SerializedProperty toggleModeProp;
    SerializedProperty toggleDisplayModeProp;
    SerializedProperty targetGraphicProp;
    SerializedProperty normalColorProp;
    SerializedProperty pressedColorProp;
    SerializedProperty targetTextProp;

    protected override void OnEnable()
    {
        base.OnEnable();
        frameImageProp = serializedObject.FindProperty("frameImage");
        toggleModeProp = serializedObject.FindProperty("toggleMode");
        toggleDisplayModeProp = serializedObject.FindProperty("toggleDisplayMode");
        targetGraphicProp = serializedObject.FindProperty("targetGraphic");
        normalColorProp = serializedObject.FindProperty("normalColor");
        pressedColorProp = serializedObject.FindProperty("pressedColor");
        targetTextProp = serializedObject.FindProperty("targetText");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Button Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(frameImageProp, new GUIContent("Frame Image"));
        EditorGUILayout.PropertyField(toggleModeProp, new GUIContent("Toggle Mode"));
        EditorGUILayout.PropertyField(toggleDisplayModeProp, new GUIContent("Display Mode"));

        if ((ToggleDisplayMode)toggleDisplayModeProp.enumValueIndex == ToggleDisplayMode.Color)
        {
            EditorGUILayout.PropertyField(targetGraphicProp, new GUIContent("Target Graphic"));
        }

        EditorGUILayout.PropertyField(normalColorProp, new GUIContent("Normal Color"));
        EditorGUILayout.PropertyField(pressedColorProp, new GUIContent("Pressed Color"));

        EditorGUILayout.PropertyField(targetTextProp, new GUIContent("Target Text (Optional)"));

        serializedObject.ApplyModifiedProperties();
    }
}