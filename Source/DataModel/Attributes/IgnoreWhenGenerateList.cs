using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoBookmart.DataLayer
{
    /// <summary>
    /// Properties or enum value which apply this attribute will be ignore when parsing the list
    /// Copyright: Trung Dang (trungdt@absoft.vn)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreWhenGenerateListAttribute : Attribute
    {

    }

    /// <summary>
    /// Properties or enum value which apply this attribute will be force to be included when parsing the list
    /// Copyright: Trung Dang (trungdt@absoft.vn)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IncludeWhenGenerateListAttribute : Attribute
    {

    }
}
