using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
namespace ConsoleApp_00
{
    public static class ExtensionMethods
    {
        public static readonly MethodInfo trimMethod = typeof(string).GetMethod("Trim", new Type[0]);
        public static readonly MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", new Type[0]);
        public static Expression GetMemberExpression(Expression param, string propertyName)
        {
            if (propertyName.Contains("."))
            {
                int index = propertyName.IndexOf(".");
                var subParam = Expression.Property(param, propertyName.Substring(0, index));
                return GetMemberExpression(subParam, propertyName.Substring(index + 1));
            }
            return Expression.Property(param, propertyName);
        }

        public static IQueryable<T> ApplyOrderBy<T>(this IQueryable<T> source, string propertyName, out bool success) where T : class
        {
            success = false;
            var type = typeof(T);
            var parameter = Expression.Parameter(type, "p");
            var expression = GetMemberExpression(parameter, propertyName);

            if (expression == null) return source;
            var temp = Expression.Lambda(expression);

            var ss = Expression.Call(typeof(Queryable),
                "OrderBy",
                new Type[] { typeof(T), temp.ReturnType },
                source.Expression,
                temp);

            success = true;
            return source.Provider.CreateQuery<T>(ss);
        }

        public static IQueryable<T> ApplyWhere<T>(this IQueryable<T> source, string propertyName, object propertyValue, out bool success) where T : class
        {
            // 1. Retrieve member access expression
            success = false;
            var mba = PropertyAccessorCache<T>.Get(propertyName);
            if (mba == null) return source;

            // 2. Try converting value to correct type
            object value;
            try
            {
                value = Convert.ChangeType(propertyValue, mba.ReturnType);
            }
            catch (SystemException ex) when (
                ex is InvalidCastException ||
                ex is FormatException ||
                ex is OverflowException ||
                ex is ArgumentNullException)
            {
                return source;
            }

            // 3. Construct expression tree
            var eqe = Expression.Equal(
                mba.Body,
                Expression.Constant(value, mba.ReturnType));
            var expression = Expression.Lambda(eqe, mba.Parameters[0]);

            // 4. Construct new query
            success = true;
            MethodCallExpression resultExpression = Expression.Call(
                null,
                GetMethodInfo(Queryable.Where, source, (Expression<Func<T, bool>>)null),
                new Expression[] { source.Expression, Expression.Quote(expression) });
            return source.Provider.CreateQuery<T>(resultExpression);
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
        {
            return f.Method;
        }
    }

    public static class PropertyAccessorCache<T> where T : class
    {
        private static readonly IDictionary<string, LambdaExpression> _cache;

        static PropertyAccessorCache()
        {
            var storage = new Dictionary<string, LambdaExpression>();

            var t = typeof(T);
            var parameter = Expression.Parameter(t, "p");
            foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var lambdaExpression = Expression.Lambda(propertyAccess, parameter);

                var xx = property.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var propertyItem in property.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var t1 = property.PropertyType;
                    var parameter1 = Expression.Parameter(t1, "p");

                    var propertyAccess1 = Expression.MakeMemberAccess(parameter1, propertyItem);
                    var lambdaExpression1 = Expression.Lambda(propertyAccess1, parameter1);

                    Console.WriteLine(propertyItem.Name);
                    storage[string.Format("{0}.{1}", property.Name, propertyItem.Name)] = lambdaExpression1;
                }

                storage[property.Name] = lambdaExpression;
            }
            _cache = storage;
        }

        public static LambdaExpression Get(string propertyName)
        {
            LambdaExpression result;
            return _cache.TryGetValue(propertyName, out result) ? result : null;
        }
    }
}



