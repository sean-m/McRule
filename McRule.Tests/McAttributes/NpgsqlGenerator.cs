﻿
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace SMM
{
	public class NpgsqlGenerator : McRule.ExpressionGeneratorBase {
		/// <summary>
		/// Builds expressions using string member functions StartsWith, EndsWith or Contains as the comparator,
		/// case-insensitive matches produce an ILike expression for Npgsql.
		/// </summary>
		public override Expression<Func<T, bool>> AddStringPropertyExpression<T>(
			Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false) {
#if DEBUG
			if (!(filterType == "StartsWith" || filterType == "EndsWith" || filterType == "Contains" ||
				  filterType == "Equals")) {
				throw new Exception($"filterType must equal StartsWith, EndsWith or Contains. Passed: {filterType}");
			}
#endif
			// If a case insensitive comparision is requested, we resolve that against the
			// Npgsql ILike extension method. EF will translate that into the proper SQL before
			// sending it to the database.
			if (ignoreCase) {
				switch (filterType) {
					case "StartsWith":
						filter = $"{filter}%";
						break;
					case "EndsWith":
						filter = $"%{filter}";
						break;
                    case "Contains":
                        filter = $"%{filter}%";
                        break;
                }

				// ILike is a virtual static extension method so needs a statically typed
				// null as the first parameter. Smart people made the type system so
				// I hope this makes sense to them.
                var nullExpr = Expression.Constant(null, typeof(DbFunctions));

                var method = typeof(NpgsqlDbFunctionsExtensions).GetMethods().Where(
                    x => (x.Name?.Equals("ILike") ?? false)
                    && x.GetParameters().Count() == 3); // There's two "ILike" versions, we want the one that takes 3 arguments.

                var likeCall = Expression.Call(method.FirstOrDefault(), nullExpr, lambda.Body, Expression.Constant(filter, typeof(string)));

                return Expression.Lambda<Func<T, bool>>(
                    likeCall,
                    lambda.Parameters);
            }

            // When case sensitive matches are fine, we can invoke the String extension methods,
            // EF will do the case sensitive stuff like normal.
            MethodInfo methodInfo = typeof(string).GetMethod(filterType, new[] { typeof(string) });
            List<Expression> expressionArgs = new List<Expression>() { Expression.Constant(filter) };

            var strPredicate = Expression.Call(lambda.Body, methodInfo, expressionArgs);

            return Expression.Lambda<Func<T, bool>>(
                strPredicate,
                lambda.Parameters);
		}
	}
}
