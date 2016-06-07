using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.Common.Helpers;
using ServiceStack.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using PhotoBookmart.DataLayer;

namespace PhotoBookmart.Support
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Merge 2 dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="me"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others)
            where T : IDictionary<K, V>, new()
        {
            T newMap = new T();
            foreach (IDictionary<K, V> src in
                (new List<IDictionary<K, V>> { me }).Concat(others))
            {
                // ^-- echk. Not quite there type-system.
                foreach (KeyValuePair<K, V> p in src)
                {
                    newMap[p.Key] = p.Value;
                }
            }
            return newMap;
        }

        /// <summary>
        /// This Extension help easier to add EnumMaillingListTokens
        /// </summary>
        /// <param name="me"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Dictionary<string, string> AddEnum(this Dictionary<string, string> me, EnumMaillingListTokens key, string value)
        {
            me.Add(key.ToString(), value);
            return me;
        }

        /// <summary>
        /// Get the list of property or value by the property name. If the property name, then it will prefer the display attribute. It will avoid the IgnoreAttribute
        /// </summary>
        /// <param name="model"></param>
        /// <param name="getValue"></param>
        /// <param name="name_prefix">The prefix will be put before the name</param>
        /// <returns></returns>
        public static Dictionary<string, string> GetPropertiesListOrValue(this object model, bool getValue = false, string name_prefix = "")
        {
            var ret = new Dictionary<string, string>();
            var t_ignore = typeof(IgnoreAttribute);
            var t_ignore2 = typeof(IgnoreWhenGenerateListAttribute);
            var t_include = typeof(IncludeWhenGenerateListAttribute);

            var properties = model.GetType().GetProperties().ToList()
                .Where(p => p.IsDefined(t_include, false) || (!p.IsDefined(t_ignore, false) && !p.IsDefined(t_ignore2, false))).ToList();
            //.Select(p => new
            //{
            //    PropertyName = p.Name,
            //    p.GetCustomAttributes(typeof(DisplayAttribute),
            //    false).Cast<DisplayAttribute>().Single().Name
            //});

            foreach (var x in properties)
            {
                // check if the property has been defined the attribute display
                var name = "";
                if (getValue)
                {
                    if (x.CanRead)
                    {
                        try
                        {
                            name = x.GetValue(model, null).ToString();
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    var c = x.GetCustomAttributes(typeof(DisplayAttribute), false).Cast<DisplayAttribute>();
                    if (c.Count() > 0)
                    {
                        name = c.FirstOrDefault().Name;
                    }
                    else
                    {
                        name = name_prefix + x.Name;
                    }
                }

                // name / display name
                ret.Add(name_prefix + x.Name, name);
            }

            return ret;
        }

        /// <summary>
        /// Merge 2 Dictionary<string,string>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Dictionary<string, string> MergeDictionary(this Dictionary<string, string> model, Dictionary<string, string> target)
        {
            foreach (var x in target.Keys)
            {
                if (!model.Keys.Contains(x)) // avoid duplication
                {
                    model.Add(x, target[x]);
                }
            }
            return model;
        }
    }
}