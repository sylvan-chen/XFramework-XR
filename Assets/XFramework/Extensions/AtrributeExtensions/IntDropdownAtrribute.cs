using UnityEngine;

namespace XGame.Extensions
{
    public class IntDropdownAttribute : PropertyAttribute
    {
        public int[] Options;

        public IntDropdownAttribute(params int[] options)
        {
            Options = options;
        }
    }
}