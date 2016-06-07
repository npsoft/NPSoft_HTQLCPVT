using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Linq.Expressions;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite
{
    public enum SQLJoinType
    {
        Inner,
        Left,
        Right
    }
    /// Copyright 2013, Trung Dang (trungdt@absoft.vn)
    // This class is written to support Join in Postgresql Mode
    public static class IDbConnectionExtensions
    {
        /// <summary>
        /// This function will help to excutes functions from PostgreSQL
        /// </summary>
        public static List<T> SqlListProcedure<T>(this IDbConnection thisObj, string function_name, string param = "", string where = "", string orderby = "", string limit = "")
        {
            Random r = new Random();
            var name = "AB" + r.Next(30000).ToString();

            List<T> y = new List<T>();
            using (IDbTransaction dbTrans = thisObj.OpenTransaction())
            {
                if (param != "")
                    param = "," + param;

                var x = thisObj.SqlScalar<string>("SELECT \"" + function_name + "\"('" + name + "' " + param + ") " + where + " " + orderby + " " + limit);
                y = thisObj.SqlList<T>("FETCH ALL IN \"" + name + "\";");
                dbTrans.Commit();
            }
            return y;
        }

        public static List<T> Join2Tables<T>(this IDbConnection thisObj, SQLJoinType join_type, List<string> colsA, List<string> colsB, string tableA, string tableB, string keyA, string keyB, string where = null, string orderby = null, int limit = -1, int offset = -1)
        {
            try
            {
                string sql = @"SELECT ";

                if (colsA != null)
                {
                    for (int i = 0; i < colsA.Count; i++)
                    {
                        sql += " Tba." + colsA[i] + ",";
                    }
                }

                if (colsB != null)
                {
                    for (int i = 0; i < colsB.Count; i++)
                    {
                        sql += " Tbb." + colsB[i] + ",";
                    }
                }

                // Remove last ,
                sql = sql.Substring(0, sql.Length - 1);

                string jt = join_type.ToString();
                sql += " FROM " + tableA + " Tba " + jt + " JOIN " + tableB + " Tbb ON Tba.\"" + keyA + "\" = Tbb.\"" + keyB + "\" ";


                if (where != null)
                {
                    sql += " WHERE " + where + " ";
                }

                if (orderby != null)
                {
                    sql += "ORDER BY " + orderby + " ";
                }

                if (limit > 0)
                {
                    sql += " LIMIT " + limit.ToString();
                }

                if (offset > 0)
                {
                    sql += " OFFSET " + offset.ToString();
                }

                sql += ";";
                return thisObj.SqlList<T>(sql);
            }
            catch
            {
                return null;
            }
        }

        public static Expression<T> AndAlso<T>(this Expression<T> left, Expression<T> right)
        {
            return Expression.Lambda<T>(Expression.AndAlso(left, right), left.Parameters);
        }

        public static TSource TakeFirst<TSource>(this IEnumerable<TSource> source)
        {
            return source.Take(1).FirstOrDefault();
        }

        /// <summary>
        /// Get the class attribute name
        /// How to use: string name = typeof(MyClass).GetAttributeValue((DomainNameAttribute dna) => dna.Name);
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="type"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        public static TValue GetAttributeValue<TAttribute, TValue>(this Type type, Func<TAttribute, TValue> valueSelector)
        where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(
                typeof(TAttribute), false
            ).FirstOrDefault() as TAttribute;
            if (att != null)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }

        /// <summary>
        /// Count the records in table by sql where
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Db"></param>
        /// <param name="sql_where"></param>
        /// <returns></returns>
        public static int CountByWhere<T>(this IDbConnection Db, string sql_where)
        {
            // get table name
            var t = typeof(T);
            var table_name = t.GetAttributeValue<AliasAttribute, string>(x => x.Name);
            var schema_name = t.GetAttributeValue<SchemaAttribute, string>(x => x.Name);
            if (!string.IsNullOrEmpty(schema_name))
            {
                schema_name = string.Format("[{0}]", schema_name) + ".";
            }
            else{
                schema_name="";
            }
            if (string.IsNullOrEmpty(table_name))
            {
                table_name = "[" + t.Name + "]";
            }
            else
            {
                table_name = string.Format("[{0}]", table_name);
            }
            var sql = string.Format("SELECT Count([Id]) FROM {0}{1} WHERE {2}", schema_name, table_name, sql_where);
            return Db.SqlScalar<int>(sql);
        }
    }
}