using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Repository
{
    public static class ExtensionLinq
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "OrderBy");
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "OrderByDescending");
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "ThenBy");
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "ThenByDescending");
        }

        static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName)
        {
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
                    memberInfo = typeof(T).GetProperty(prop);
                else
                {
                    var propertyInfoIntend = memberInfo as PropertyInfo;
                    memberInfo = propertyInfoIntend.PropertyType.GetProperty(prop);
                }

                if (memberExpression == null)
                    memberExpression = Expression.MakeMemberAccess(arg, memberInfo);
                else
                    memberExpression = Expression.MakeMemberAccess(memberExpression, memberInfo);
            }
            var expressions = Init();
            var body = expressions[expressionType].Invoke(memberExpression, Expression.Constant(value), null);
            var lambdaExpression =
                Expression.Lambda<Func<T, bool>>(body, "MainScope", new[] { arg });

            Log(lambdaExpression);

            var resultQuery = source.Where(lambdaExpression);

            return (IQueryable<T>)resultQuery;
        }

        private static void Log<T>(Expression<Func<T, bool>> lambdaExpression)
        {
            Console.WriteLine(string.Format("{0} => {1}", lambdaExpression.Name, lambdaExpression.Body));
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
                { ExpressionType.Power, (member, constant1, constant2) => ExpressionExtentions.ListContains(member, constant1) } 

            };
        }
    }

    public static class ExpressionExtentions
    {
        static readonly MethodInfo stringContainsMethod = typeof(string).GetMethod("Contains");
        public static Expression ListContains(this Expression member, Expression expression)
        {
           
            MethodCallExpression contains = null;
            if (expression is ConstantExpression constant && constant.Value is IList && constant.Value.GetType().IsGenericType)
            {
                var type = constant.Value.GetType();
                var containsInfo = type.GetMethod("Contains", new[] { type.GetGenericArguments()[0] });
                contains = Expression.Call(constant, containsInfo, member);
            }
            else if (expression is ConstantExpression constantString && constantString.Value is string)
            {
                var type = constantString.Value.GetType();
                var containsInfo = type.GetMethod("Contains", new Type[] { typeof(string) });
                contains = Expression.Call(member, containsInfo, constantString);
            }
            return contains ?? Expression.Call(member, stringContainsMethod, expression);
        }
    }
}
