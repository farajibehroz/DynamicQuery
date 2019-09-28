using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
namespace Infrastructure
{
    public static class ExtensionMethods
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property = "Id desc")
        {
            string[] prop = ApplySplit<T>(property);
            string propertyPath = ApplyUpperCaseFirstCharacter(prop[0]);
            if (prop[1].ToLower().Equals("asc"))
            {
                return ApplyOrder<T>(source, propertyPath, "OrderBy");
            }
            return ApplyOrder<T>(source, propertyPath, "OrderByDescending");
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, ApplyUpperCaseFirstCharacter(property), "OrderByDescending");
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, ApplyUpperCaseFirstCharacter(property), "ThenBy");
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, ApplyUpperCaseFirstCharacter(property), "ThenByDescending");
        }

        private static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName)
        {
            if (string.IsNullOrEmpty(property))
            {
                return (IOrderedQueryable<T>)source;
            }
            string[] props = property.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }

            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            object result = typeof(Queryable).GetMethods().Single(
                method => method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetGenericArguments().Length == 2
                            && method.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), type)
                    .Invoke(null, new object[] { source, lambda });

            return (IOrderedQueryable<T>)result;
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, ExpressionType expressionType, string property, object value)
        {
            if (value == null)
            {
                return source;
            }

            string[] props = property.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type);
            Expression expr = arg;
            MemberInfo memberInfo = null;

            MemberExpression memberExpression = null;

            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;

                if (memberInfo == null)
                {
                    memberInfo = typeof(T).GetProperty(prop);
                }
                else
                {
                    PropertyInfo propertyInfoIntend = memberInfo as PropertyInfo;
                    memberInfo = propertyInfoIntend.PropertyType.GetProperty(prop);
                }

                if (memberExpression == null)
                {
                    memberExpression = Expression.MakeMemberAccess(arg, memberInfo);
                }
                else
                {
                    memberExpression = Expression.MakeMemberAccess(memberExpression, memberInfo);
                }
            }
            Dictionary<ExpressionType, Func<Expression, Expression, Expression, Expression>> Expressions = Init();
            // var convertedValue = Convert.ChangeType(value, (memberInfo as PropertyInfo).PropertyType);
            Expression body = Expressions[expressionType].Invoke(memberExpression, Expression.Constant(value), null);
            Expression<Func<T, bool>> lambdaExpression =
                Expression.Lambda<Func<T, bool>>(body, "MainScope", new[] { arg });

            IQueryable<T> resultQuery = source.Where(lambdaExpression);
            return resultQuery;
        }

        private static Dictionary<ExpressionType, Func<Expression, Expression, Expression, Expression>> Init()
        {
            return new Dictionary<ExpressionType, Func<Expression, Expression, Expression, Expression>>
            {
                { ExpressionType.Equal, (member, constant1, constant2) => Expression.Equal(member, constant1) },
                { ExpressionType.NotEqual, (member, constant1, constant2) => Expression.NotEqual(member, constant1) },
                { ExpressionType.GreaterThan, (member, constant1, constant2) => Expression.GreaterThan(member, constant1) },
                { ExpressionType.GreaterThanOrEqual, (member, constant1, constant2) => Expression.GreaterThanOrEqual(member, constant1) },
                { ExpressionType.LessThan, (member, constant1, constant2) => Expression.LessThan(member, constant1) },
                { ExpressionType.LessThanOrEqual, (member, constant1, constant2) => Expression.LessThanOrEqual(member, constant1) },
                { ExpressionType.Power, (member, constant1, constant2) => Contains(member, constant1) }
            };
        }

        private static string[] ApplySplit<T>(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                if (typeof(T).GetProperty("Id") != null)
                {
                    return new[] { "Id", "desc" };
                }
                string firstPropertyName = typeof(T).GetProperties()[0].Name;
                return new[] { firstPropertyName, "desc" };
            }
            string[] result = property.Split(' ');
            if (result.Length == 1)
            {
                return new[] { property, "desc" };
            }
            return result;
        }

        private static string ApplyUpperCaseFirstCharacter(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return null;
            }

            string propertyPath;
            char firstCharacter = property[0];
            propertyPath = property.Remove(0, 1);
            propertyPath = firstCharacter.ToString().ToUpper() + propertyPath;
            return propertyPath;
        }

        private static readonly MethodInfo stringContainsMethod = typeof(string).GetMethod("Contains");
        public static Expression Contains(this Expression member, Expression expression)
        {
            MethodCallExpression contains = null;
            if (expression is ConstantExpression constant && constant.Value is IList && constant.Value.GetType().IsGenericType)
            {
                Type type = constant.Value.GetType();
                MethodInfo containsInfo = type.GetMethod("Contains", new[] { type.GetGenericArguments()[0] });
                contains = Expression.Call(constant, containsInfo, member);
            }
            else if (expression is ConstantExpression constantString && constantString.Value is string)
            {
                Type type = constantString.Value.GetType();
                MethodInfo containsInfo = type.GetMethod("Contains", new Type[] { typeof(string) });
                contains = Expression.Call(member, containsInfo, constantString);
            }
            return contains ?? Expression.Call(member, stringContainsMethod, expression);
        }
    }


    public static class FilterQuery
    {
        public static IQueryable<TEntity> Filter<TEntity, TFilter>(this IQueryable<TEntity> query, TFilter filter)
        {
            Type filterType = typeof(TFilter);
            Type entityType = typeof(TEntity);
            var entityTypeProperties = entityType.GetProperties();

            if (ConfigureExpressionTypeDictionary == null) ConfigureExpressionType();

            foreach (var filterProperty in filterType.GetProperties())
            {
                var matchedProperty = entityTypeProperties.FirstOrDefault(t => t.Name.StartsWith(filterProperty.Name));
                if (matchedProperty == null)
                    throw new Exception(string.Format("property {0} from {1} not found in {2}", filterProperty.Name, filterType, entityType));

                query = query.Where(ConfigureExpressionTypeDictionary[filterProperty.PropertyType], matchedProperty.Name, GetValue(filter, matchedProperty.Name));
            }
            return query;
        }

        public static Dictionary<Type, ExpressionType> ConfigureExpressionTypeDictionary { get; set; }

        private static object GetValue<TFilter>(TFilter filter, string name)
        {
            return filter.GetType().GetProperty(name).GetValue(filter);
        }

        private static void ConfigureExpressionType()
        {
            ConfigureExpressionTypeDictionary = new Dictionary<Type, ExpressionType>();
            ConfigureExpressionTypeDictionary.Add(typeof(int), ExpressionType.Equal);
            ConfigureExpressionTypeDictionary.Add(typeof(int?), ExpressionType.Equal);
            ConfigureExpressionTypeDictionary.Add(typeof(decimal), ExpressionType.Equal);
            ConfigureExpressionTypeDictionary.Add(typeof(decimal?), ExpressionType.Equal);
            ConfigureExpressionTypeDictionary.Add(typeof(IList), ExpressionType.Power);
            ConfigureExpressionTypeDictionary.Add(typeof(string), ExpressionType.Power);
        }
    }
    public class AlbumFilter
    {
        public int? Quantity { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
    }
}



