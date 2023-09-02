using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {

    public class ExpressionRuleCollection : IExpressionRule {
        public Guid Id { get; set; } = Guid.NewGuid();
        public PredicateExpressionPolicyExtensions.RuleOperator RuleOperator { get; set; } = PredicateExpressionPolicyExtensions.RuleOperator.And;
        public IEnumerable<IExpressionRule> Rules { get; set; }
        public string TargetType { get; set; }

        public ExpressionRuleCollection() { }

        public Expression<Func<T, bool>>? GetPredicateExpression<T>() {
            var expressions = Rules.Select(x => x.GetPredicateExpression<T>()).Where(x => x != null);

            return (RuleOperator == PredicateExpressionPolicyExtensions.RuleOperator.Or) 
                ? PredicateExpressionPolicyExtensions.CombineOr<T>(expressions) 
                : PredicateExpressionPolicyExtensions.CombineAnd<T>(expressions);
        }

        public Expression<Func<T, bool>>? GetPredicateExpression<T>(ExpressionGenerator generator)
        {
            var expressions = generator.GetPredicateExpression<T>(this);
            return expressions;
        }
    }

    public class ExpressionRule : IExpressionRule {
        public string TargetType { get; set; }
        public string Property { get; set; }
        public string Value { get; set; }

        Expression? cachedExpression = null;

        public ExpressionRule() { }

        public ExpressionRule(string TargetType, string Property, string Value) {
            this.TargetType = TargetType;
            this.Property = Property;
            this.Value = Value;
        }

        public ExpressionRule((string, string, string) input) {
            TargetType = input.Item1;
            Property = input.Item2;
            Value = input.Item3;
        }
        
        /// <summary>
        /// Returns an expression tree targeting an object type based on policy parameters.
        /// </summary>	
        public Expression<Func<T, bool>>? GetPredicateExpression<T>() {
            if (!(typeof(T).Name.Equals(this.TargetType, StringComparison.CurrentCultureIgnoreCase))) return null;

            if (cachedExpression == null) {
                cachedExpression = PredicateExpressionPolicyExtensions.GetPredicateExpressionForType<T>(this.Property, this.Value);
            }

            return (Expression<Func<T, bool>>)cachedExpression;
        }

        /// <summary>
        /// Returns an expression tree targeting an object type based on policy parameters.
        /// </summary>	
        public Expression<Func<T, bool>>? GetPredicateExpression<T>(ExpressionGenerator generator) {
            if (!(typeof(T).Name.Equals(this.TargetType, StringComparison.CurrentCultureIgnoreCase))) return null;

            return generator.GetPredicateExpressionForType<T>(this.Property, this.Value);
        }

        public override string ToString() {
            return $"[{TargetType}]{Property}='{Value}']";
        }

        public string GetFilterString<T>() {
            return GetPredicateExpression<T>()?.ToString() ?? String.Empty;
        }
    }
}
