using System;

namespace WhiteDragon
{
    /// <summary>Describes an EnemyBehavior for search and the type chooser: one line, plus a category.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EnemyBehaviorInfoAttribute : Attribute
    {
        public readonly string Description;
        public readonly string Category;

        public EnemyBehaviorInfoAttribute(string description, string category)
        {
            Description = description;
            Category = category;
        }
    }
}
