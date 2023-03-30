using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {

    public class FilterRuleCollection {
        public Guid id { get; set; } = Guid.NewGuid();

        public FilterRuleCollection rule { get; set; }
        public FilterPolicyExtensions.RuleOperator ruleOperator { get; set; } = FilterPolicyExtensions.RuleOperator.And;

        public IEnumerable<FilterRule> rules { get; set; }
    }

    public class FilterRule {
        public string targetType { get; set; }
        public string property { get; set; }
        public string value { get; set; }

        Expression? cachedExpression = null;

        public (string, string, string) rule => (targetType, property, value);

        public FilterRule(string TargetType, string Property, string Value) {
            targetType = TargetType;
            property = Property;
            value = Value;
        }

        public FilterRule((string, string, string) input) {
            targetType = input.Item1;
            property = input.Item2;
            value = input.Item3;
        }

        /// <summary>
        /// Returns an expression tree targeting an object type based on policy parameters.
        /// </summary>	
        public Expression<Func<T, bool>>? GetFilterExpression<T>() {
            if (!(typeof(T).Name.Equals(this.targetType, StringComparison.CurrentCultureIgnoreCase))) return null;

            if (cachedExpression == null) {
                cachedExpression = FilterPolicyExtensions.GetFilterExpressionForType<T>(this.property, this.value);
            }

            return (Expression<Func<T, bool>>)cachedExpression;
        }

        public override string ToString() {
            return $"[{targetType}]{property}='{value}']";
        }

        public string GetFilterString<T>() {
            return this.GetFilterExpression<T>()?.ToString() ?? String.Empty;
        }
    }
}
