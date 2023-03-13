
using System.Linq.Expressions;

namespace Ruler;

public class FilterPolicy
{
    public Guid id { get; set; } = Guid.NewGuid(); 
    public string name { get; set; }
    public string[] properties { get; set; }
    public IEnumerable<(string, string)> scope { get; set; }
    public FilterPolicyExtensions.RuleOperator ruleOperator { get; set; } = FilterPolicyExtensions.RuleOperator.And;
}

public static class FilterPolicyExtensions
{
    public enum RuleOperator
    {
        And,
        Or
    }

    public static Expression<Func<T, bool>> AddFilterToStringProperty<T>(
        Expression<Func<T, string>> expression, string filter, string filterType)
    {

#if DEBUG
        if (!(filterType == "StartsWith" || filterType == "EndsWith" || filterType == "Contains"))
        {
            throw new Exception($"filterType must equal StartsWith, EndsWith or Contains. Passed {filterType}");
        }

#endif
        // Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
        var notNull = Expression.NotEqual(expression.Body, Expression.Constant(null));

        // Setup calls to EtartsWith, EndsWith, or Contains
        // TODO expressionArgs was used to pass multiple values for case insensitive compare, wasn't
        // mapping the method correctly when used with EF so need to revisit that
        var expressionArgs = new Expression[] { Expression.Constant(filter) };
        var strPredicate = Expression.Call(expression.Body, filterType, null, expressionArgs);

        var filterExpression = Expression.AndAlso(notNull, strPredicate);

        return Expression.Lambda<Func<T, bool>>(
            filterExpression,
            expression.Parameters);
    }

    // Dynamically build an expression suitable for filtering in a Where clause
    public static Expression<Func<T, bool>> GetFilterForType<T>(string property, string value)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var opLeft = Expression.Property(parameter, property);
        var opRight = Expression.Constant(value);
        var comparison = Expression.Equal(opLeft, opRight);

        // For string comparisons using wildcards, trim the wildcard characters and pass to the comparison method
        if (opLeft.Type == typeof(string))
        {
            // Grab the object property for use in the inner expression body
            var strParam = Expression.Lambda<Func<T, string>>(opLeft, parameter);

            if (value.StartsWith("*") && value.EndsWith("*"))
            {
                return AddFilterToStringProperty<T>(strParam, value.Trim('*'), "Contains");
            }
            else if (value.StartsWith("*"))
            {
                return AddFilterToStringProperty<T>(strParam, value.TrimStart('*'), "EndsWith");
            }
            else if (value.EndsWith("*"))
            {
                return AddFilterToStringProperty<T>(strParam, value.TrimEnd('*'), "StartsWith");
            }
            else
            {
                return Expression.Lambda<Func<T, bool>>(comparison, parameter);
            }
        }

        return Expression.Lambda<Func<T, bool>>(comparison, parameter);
    }

    // Combine a list of expressions inclusively
    public static Expression<Func<T, bool>>? CombineAnd<T>(IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        if (predicates.Count() == 0) return null;

        var final = predicates.First();
        foreach (var next in predicates.Skip(1))
            final = PredicateBuilder.And(final, next);

        return final;
    }


    // Combine a list of expressions inclusively
    public static Expression<Func<T, bool>>? CombineOr<T>(IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        if (predicates.Count() == 0) return null;

        var final = predicates.First();
        foreach (var next in predicates.Skip(1))
            final = PredicateBuilder.Or(final, next);

        return final;
    }


    // Combine a list of expressions inclusively
    public static Expression<Func<T, bool>>? CombinePredicates<T>(IEnumerable<Expression<Func<T, bool>>> predicates,
        FilterPolicyExtensions.RuleOperator op)
    {
        if (predicates.Count() == 0) return null;

        if (op == RuleOperator.And)
        {
            return CombineAnd(predicates);
        }

        return CombineOr(predicates);
    }


    public static Expression<Func<T, bool>>? GetFilterExpression<T>(this FilterPolicy policy)
    {
        var predicates = new List<Expression<Func<T, bool>>>();
        foreach (var constraints in policy.scope)
        {
            predicates.Add(GetFilterForType<T>(constraints.Item1, constraints.Item2));
        }

        return CombinePredicates<T>(predicates, policy.ruleOperator);
    }
}
