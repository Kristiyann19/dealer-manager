using System.Reflection;

namespace DealerManager.Common.Attributes
{
    public class SkipAttribute : Attribute
    {
        public static bool IsDeclared(PropertyInfo propertyInfo)
            => propertyInfo.GetCustomAttribute(typeof(SkipAttribute)) != null;
    }
}
