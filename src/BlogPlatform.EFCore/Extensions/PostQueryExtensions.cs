using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace BlogPlatform.EFCore.Extensions
{
    public static class PostQueryExtensions
    {
        private static readonly ConstantExpression DbFunctionsConstantExp = Expression.Constant(EF.Functions);
        private static readonly MethodInfo JsonSearchAnyMethod = typeof(MySqlJsonDbFunctionsExtensions)
            .GetMethod(nameof(MySqlJsonDbFunctionsExtensions.JsonSearchAny), BindingFlags.Static | BindingFlags.Public, [typeof(DbFunctions), typeof(object), typeof(string)])
            ?? throw new NullReferenceException();

        public static IQueryable<Post> FilterTag(this IQueryable<Post> query, IEnumerable<string> tags, TagFilterOption filterOption)
        {
            if (!tags.Any())
            {
                return query;
            }

            Func<Expression, Expression, BinaryExpression> conditionFunc = filterOption switch
            {
                TagFilterOption.All => Expression.AndAlso,
                TagFilterOption.Any => Expression.OrElse,
                _ => throw new InvalidEnumArgumentException(nameof(filterOption), (int)filterOption, typeof(TagFilterOption))
            };

            ParameterExpression parameter = Expression.Parameter(typeof(Post));
            MemberExpression tagsExpression = Expression.Property(parameter, nameof(Post.Tags));
            Expression body = Expression.Constant(filterOption == TagFilterOption.All);

            foreach (string tag in tags)
            {
                ConstantExpression tagConstant = Expression.Constant(tag);
                MethodCallExpression jsonSearchCallExp =
                    Expression.Call(JsonSearchAnyMethod, DbFunctionsConstantExp, tagsExpression, tagConstant);
                body = conditionFunc(body, jsonSearchCallExp);
            }

            Expression<Func<Post, bool>> predicate = Expression.Lambda<Func<Post, bool>>(body, parameter);
            return query.Where(predicate);
        }
    }
}
