using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {
    public interface IExpressionRule {
        string TargetType { get; set; }
        Expression<Func<T, bool>>? GetPredicateExpression<T>();
        Expression<Func<T, bool>>? GetPredicateExpression<T>(ExpressionGenerator generator);
    }

    public interface ExpressionGenerator
    {
        Expression<Func<T, bool>>? GetPredicateExpression<T>(ExpressionRuleCollection policy);

        Expression<Func<T, bool>> GetPredicateExpressionForType<T>(string property, string value);
    }
}
