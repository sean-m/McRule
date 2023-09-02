using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace McRule.EF {

    internal class CoreExtensions : CoreExtenionFunctions {
        public Expression<Func<T, bool>> AddStringPropertyExpression<T>(Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false) {
            return AddStringPropertyExpression<T>(lambda, filter, filterType, ignoreCase, false);
        }

        /// <summary> 
        /// Builds expressions using string member functions StartsWith, EndsWith or Contains as the comparator. 
        /// </summary> 
        public Expression<Func<T, bool>> AddStringPropertyExpression<T>(
            Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false, bool supportEF = false) {

#if DEBUG
        if (!(filterType == "StartsWith" || filterType == "EndsWith" || filterType == "Contains" || filterType == "Equals")) 
        { 
            throw new Exception($"filterType must equal StartsWith, EndsWith or Contains. Passed: {filterType}"); 
        }
#endif
            // Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
            var notNull = Expression.NotEqual(lambda.Body, Expression.Constant(null));


            MethodInfo methodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            string filterString = filter;
            switch (filterType) {
                case "Contains":
                    filterString = filter.Trim(new[] { ' ', '*' });
                    break;
                case "StartsWith":
                    filterString = $"%{filter.Trim(new[] { ' ', '*' })}";
                    break;
                case "EndsWith":
                    filterString = $"{filter.Trim(new[] { ' ', '*' })}%";
                    break;
                default:
                    methodInfo = typeof(string).GetMethod(filterType, new[] { typeof(string) });
                    break;
            }


            // Setup calls to: StartsWith, EndsWith, Contains, or Equals,
            // conditionally using character case neutral comparision.
            List<Expression> expressionArgs = new List<Expression>() { Expression.Constant(filterString) };

            var strPredicate = Expression.Call(lambda.Body, methodInfo, expressionArgs);

            Expression filterExpression = Expression.AndAlso(notNull, strPredicate);

            return Expression.Lambda<Func<T, bool>>(
                filterExpression,
                lambda.Parameters);
        }
    }
}
