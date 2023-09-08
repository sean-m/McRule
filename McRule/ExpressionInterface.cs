using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {

    public interface IExpressionRuleCollection {
        public Guid Id { get; }
        public IEnumerable<IExpressionPolicy> Rules { get; }
        public RuleOperator RuleOperator { get; }
    }

    public interface IExpressionRule {
        string TargetType { get; set; }
        string Property { get; set; }
        string Value { get; set; }

    }

    public interface IExpressionPolicy {

        Expression<Func<T, bool>>? GetPredicateExpression<T>();
        Expression<Func<T, bool>>? GeneratePredicateExpression<T>(ExpressionGenerator generator);
    }

    public interface ExpressionGenerator
    {
        Expression<Func<T, bool>>? GetPredicateExpressionOrFalse<T>(IExpressionPolicy policy);

        Expression<Func<T, bool>>? GetPredicateExpression<T>(IExpressionRule policy);
        Expression<Func<T, bool>>? GetPredicateExpression<T>(IExpressionRuleCollection policy);
        Expression<Func<T, bool>> GetPredicateExpressionForType<T>(string property, string value);
    }
}
