using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ArrayLayout))]
public class CustPropertyDrawer : PropertyDrawer
{
    // ROWS MUST ALWAYS BE SMALLER THAN COL
    private int fixedR = 7;
    private int fixedCol = 7;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PrefixLabel(position, label);
        Rect newPosition = position;
        newPosition.y += 18f;
        SerializedProperty data = property.FindPropertyRelative("rows");

        for (int j = 0; j < fixedCol; j++)
        {
            SerializedProperty row = data.GetArrayElementAtIndex(j).FindPropertyRelative("row");
            newPosition.height = 18f;
            if (row.arraySize != fixedCol)
            {
                row.arraySize = fixedCol;
            }
            newPosition.width = position.width / fixedCol;
            for (int i = 0; i < fixedR; i++)
            {
                EditorGUI.PropertyField(newPosition, row.GetArrayElementAtIndex(i), GUIContent.none);
                newPosition.x += newPosition.width;
            }
            newPosition.x = position.x;
            newPosition.y += 18f;

        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 18f * 8;
    }
}