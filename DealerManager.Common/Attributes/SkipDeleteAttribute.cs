using System.Reflection;

namespace DealerManager.Common.Attributes
{
    public class SkipDeleteAttribute : Attribute
    {
        public static bool IsDeclared(PropertyInfo propertyInfo)
            => propertyInfo.GetCustomAttribute(typeof(SkipDeleteAttribute)) != null;
    }
}
