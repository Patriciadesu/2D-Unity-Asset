using System;

namespace NaughtyAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class FoldoutAttribute : MetaAttribute, IGroupAttribute
    {
        public string Name { get; private set; }
        public bool IsBold { get; private set; } = false; // Default to bold

        public FoldoutAttribute(string name)
        {
            Name = name;
        }

        // Optional constructor to allow toggling bold
        public FoldoutAttribute(string name, bool isBold = true)
        {
            Name = name;
            IsBold = isBold;
        }
    }
}
