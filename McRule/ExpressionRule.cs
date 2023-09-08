using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using static McRule.PredicateExpressionPolicyExtensions;

namespace McRule {

    public class ExpressionRuleCollection : IExpressionRuleCollection, IExpressionPolicy {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public RuleOperator RuleOperator { get; set; } = RuleOperator.And;
        public IEnumerable<IExpressionPolicy> Rules { get; set; }
        public string TargetType { get; set; }

        public ExpressionRuleCollection() { }

        public Expression<Func<T, bool>>? GetPredicateExpression<T>() {
            var gen = PredicateExpressionPolicyExtensions.GetCoreExpressionGenerator();

            return GeneratePredicateExpression<T>(gen);
        }

        public Expression<Func<T, bool>>? GeneratePredicateExpression<T>(ExpressionGenerator generator)
        {
            var expressions = generator.GetPredicateExpression<T>(this);
            return expressions;
        }
    }

    public class ExpressionRule : IExpressionRule, IExpressionPolicy {
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
                cachedExpression = GetCoreExpressionGenerator().GetPredicateExpressionForType<T>(this.Property, this.Value);
            }

            return (Expression<Func<T, bool>>)cachedExpression;
        }

        /// <summary>
        /// Returns an expression tree targeting an object type based on policy parameters.
        /// </summary>	
        public Expression<Func<T, bool>>? GeneratePredicateExpression<T>(ExpressionGenerator generator) {
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
