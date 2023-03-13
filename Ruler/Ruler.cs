
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

// Taken from LinqKit, Joseph Albahari. Man is a legend.
// LINQKit Copyright (c) 2007-2009 Joseph Albahari, Tomas Petricek
// 
// The Expression Visitor class is based on a Microsoft sample.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> True<T>()
    {
        return f => true;
    }

    public static Expression<Func<T, bool>> False<T>()
    {
        return f => false;
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>
            (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>
            (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }
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
