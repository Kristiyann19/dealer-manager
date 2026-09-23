using System.Reflection;

namespace DealerManager.Common.Attributes
{
    public class SkipUpdateAttribute : Attribute
    {
        public static bool IsDeclared(PropertyInfo propertyInfo)
            => propertyInfo.GetCustomAttribute(typeof(SkipUpdateAttribute)) != null;
    }
}
