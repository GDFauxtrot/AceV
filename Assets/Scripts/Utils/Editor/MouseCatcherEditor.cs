#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using AceV;


[CustomEditor(typeof(MouseCatcher))]
public class MouseCatcherEditor : ButtonEditor
{
    SerializedProperty onClickDownProperty;

    protected override void OnEnable()
    {
        base.OnEnable();

        onClickDownProperty = serializedObject.FindProperty("onClickDown");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.PropertyField(onClickDownProperty);

        serializedObject.ApplyModifiedProperties();
    }
}

#endif