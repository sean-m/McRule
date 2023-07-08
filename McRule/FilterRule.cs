using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {

    public class FilterRuleCollection {
        public Guid Id { get; set; } = Guid.NewGuid();

        public FilterRuleCollection Rule { get; set; }
        public PredicateExpressionPolicyExtensions.RuleOperator RuleOperator { get; set; } = PredicateExpressionPolicyExtensions.RuleOperator.And;

        public IEnumerable<FilterRule> Rules { get; set; }
        public FilterRuleCollection() { }
    }

    public class FilterRule {
        public string TargetType { get; set; }
        public string Property { get; set; }
        public string Value { get; set; }

        Expression? cachedExpression = null;

        internal (string, string, string) Rule => (TargetType, Property, Value);

        public FilterRule() { }

        public FilterRule(string TargetType, string Property, string Value) {
            this.TargetType = TargetType;
            this.Property = Property;
            this.Value = Value;
        }

        public FilterRule((string, string, string) input) {
            TargetType = input.Item1;
            Property = input.Item2;
            Value = input.Item3;
        }
        
        /// <summary>
        /// Returns an expression tree targeting an object type based on policy parameters.
        /// </summary>	
        public Expression<Func<T, bool>>? GetFilterExpression<T>() {
            if (!(typeof(T).Name.Equals(this.TargetType, StringComparison.CurrentCultureIgnoreCase))) return null;

            if (cachedExpression == null) {
                cachedExpression = PredicateExpressionPolicyExtensions.GetPredicateExpressionForType<T>(this.Property, this.Value);
            }

            return (Expression<Func<T, bool>>)cachedExpression;
        }

        public override string ToString() {
            return $"[{TargetType}]{Property}='{Value}']";
        }

        public string GetFilterString<T>() {
            return this.GetFilterExpression<T>()?.ToString() ?? String.Empty;
        }
    }
}
