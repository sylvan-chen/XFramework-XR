using UnityEditor;
using UnityEngine;
using XGame.Extensions;


namespace XGame.Editor
{

    [CustomPropertyDrawer(typeof(IntDropdownAttribute))]
    public class IntDropdownAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dropdown = attribute as IntDropdownAttribute;
            int currentIndex = System.Array.IndexOf(dropdown.Options, property.intValue);
            if (currentIndex < 0) currentIndex = 0;

            string[] displayOptions = System.Array.ConvertAll(dropdown.Options, opt => opt.ToString());
            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
            property.intValue = dropdown.Options[selectedIndex];
        }
    }
}