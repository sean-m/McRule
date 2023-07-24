
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace McRule;

public static class PredicateExpressionPolicyExtensions {
    public enum RuleOperator {
        And,
        Or
    }

    public static ExpressionRule ToFilterRule(this (string, string, string) tuple) {
        return new ExpressionRule(tuple);
    }

    /// <summary> 
    /// Builds expressions using string member functions StartsWith, EndsWith or Contains as the comparator. 
    /// </summary> 
    public static Expression<Func<T, bool>> AddStringPropertyExpression<T>(
        Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false) 
    { 

#if DEBUG 
        if (!(filterType == "StartsWith" || filterType == "EndsWith" || filterType == "Contains" || filterType == "Equals")) 
        { 
            throw new Exception($"filterType must equal StartsWith, EndsWith or Contains. Passed {filterType}"); 
        } 

#endif
        // Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
        var notNull = Expression.NotEqual(lambda.Body, Expression.Constant(null)); 

        // Setup calls to: StartsWith, EndsWith, Contains, or Equals,
        // conditionally using character case neutral comparision.
        Expression[] expressionArgs = new[] { Expression.Constant(filter), Expression.Constant(StringComparison.CurrentCulture) }; 
        if (ignoreCase)
        {
            expressionArgs[1] = Expression.Constant(StringComparison.CurrentCultureIgnoreCase);
        }

        MethodInfo methodInfo = typeof(string).GetMethod(filterType, new[] { typeof(string), typeof(StringComparison) });
        var strPredicate = Expression.Call(lambda.Body, methodInfo, expressionArgs);

        Expression filterExpression = Expression.AndAlso(notNull, strPredicate); 

        return Expression.Lambda<Func<T, bool>>( 
            filterExpression, 
            lambda.Parameters); 
    } 

    /// <summary>
    /// Prepend the given predicate with a short circuiting null check.
    /// </summary>
    public static Expression AddNullCheck<T>(
                            Expression left,
                            Expression expression) {
        // Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
        var notNull = Expression.NotEqual(left, Expression.Constant(null));

        return Expression.AndAlso(notNull, expression);
    }

    /// <summary>
    /// Applies negative predicate to expression in a lambda.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="operand"></param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> Negate<T>(Expression<Func<T, bool>> lambda) {
        var body = lambda.Body;
        var parameters = lambda.Parameters;

        var negated = Expression.IsFalse(body);
        return Expression.Lambda<Func<T, bool>>(
            negated,
            parameters);
    }

    /// <summary>
    /// Return a binary expression based on the given filter string. Default to a
    /// standard Equals comparison.
    /// </summary>
    private static Expression GetComparer(string op, Expression left, Expression right) => op switch {
        ">" => Expression.GreaterThan(left, right),
        ">=" => Expression.GreaterThanOrEqual(left, right),
        "<" => Expression.LessThan(left, right),
        "<=" => Expression.LessThanOrEqual(left, right),
        "<>" => Expression.NotEqual(left, right),
        "!=" => Expression.NotEqual(left, right),
        "!" => Expression.NotEqual(left, right),
        _ => Expression.Equal(left, right)
    };

    // Used to test for numerical integer types when casting a float to integer.
    private static Type[] intTypes = { typeof(Int16),   typeof(Int32),   typeof(Int64),
                                       typeof(UInt16),  typeof(UInt32),  typeof(UInt64),
                                       typeof(Int16?),  typeof(Int32?),  typeof(Int64?),
                                       typeof(UInt16?), typeof(UInt32?), typeof(UInt64?)};


    /// <summary>
    /// Dynamically build an expression suitable for filtering in a Where clause
    /// </summary>
    public static Expression<Func<T, bool>> GetPredicateExpressionForType<T>(string property, string value) {
        var parameter = Expression.Parameter(typeof(T), "x");
        var opLeft = Expression.Property(parameter, property);
        var opRight = Expression.Constant(value);
        Expression? comparison = null;

        // For IComparable types on the left hand side, attempt to parse the right hand side
        // into the same type and use <,>,<=,>=,= prefixes to infer BinaryExpression type.
        // Should work with numerical or datetime values provided they parse correctly.
        // Note, a float on the right hand side will not parse into an integer type implicitly
        // so it is parsed into a decimal value first and then rounded to the nearest integral.
        var lType = opLeft.Type;
        var isNullable = false;
        Type? hasComparable = lType.GetInterface("IComparable");
        Type? hasCollection = lType.GetInterface("ICollection");
        if (hasComparable == null && opLeft.Type.IsValueType) {
            lType = Nullable.GetUnderlyingType(opLeft.Type);
            // Nullable.GetUnderlyingType only returns a non-null value if the
            // supplied type was indeed a nullable type.
            if (lType != null)
                isNullable = true;
            hasComparable = lType.GetInterface("IComparable");
        }


        // For string comparisons using wildcards, trim the wildcard characters and pass to the comparison method
        if (lType == typeof(string)) 
        {
            Expression<Func<T, bool>> result;

            // Grab the object property for use in the inner expression body
            var strParam = Expression.Lambda<Func<T, string>>(opLeft, parameter);

            // If a string match begins with !, we negate the result.
            var negateResult = false;
            if (value.StartsWith("!")) {
                negateResult = true;
                value = value.TrimStart('!');
            }

            // String comparisons which are prefixed with '~' will be evaluated ignoring case.
            // Note: when expression trees are used outside .net, such as with EF to SQL Server,
            // default case sensitivity for that environment may apply implicitly and counter to
            // filter policy intent.
            bool ignoreCase = false; 
            if (value.StartsWith('~')) { 
                ignoreCase = true; 
                value = value.TrimStart('~'); 
            }

            if (value.StartsWith("*") && value.EndsWith("*")) { 
                result = AddStringPropertyExpression<T>(strParam, value.Trim('*'), "Contains", ignoreCase); 
            } else if (value.StartsWith("*")) {
                result = AddStringPropertyExpression<T>(strParam, value.TrimStart('*'), "EndsWith", ignoreCase); 
            } else if (value.EndsWith("*")) {
                result = AddStringPropertyExpression<T>(strParam, value.TrimEnd('*'), "StartsWith", ignoreCase); 
            } else {
                result = AddStringPropertyExpression<T>(strParam, value, "Equals", ignoreCase); 
            }

            if (negateResult) {
                result = Negate<T>(result);
            }

            return result;
        } 
        else if (hasComparable == typeof(IComparable)) 
        {
            var operatorPrefix = Regex.Match(value.Trim(), @"^[!<>=]+");
            var operand = (operatorPrefix.Success ? value.Replace(operatorPrefix.Value, "") : value).Trim();

            if (!String.IsNullOrEmpty(operand)) {
                var parseMethod = lType.GetMethods().FirstOrDefault(x => x.Name == "Parse");
                if (intTypes.Contains(opLeft.Type)) {
                    operand = operand.Contains(".") ? Math.Round(decimal.Parse(operand)).ToString() : operand;
                }
                var opRightNumerical = parseMethod?.Invoke(null, new string[] { operand });

                opRight = Expression.Constant(opRightNumerical);
                Expression opLeftFinal = isNullable ? Expression.Convert(opLeft, lType) : opLeft;
                comparison = GetComparer(operatorPrefix.Value.Trim(), opLeftFinal, opRight);
            }
        } else if (hasCollection == typeof(ICollection)) {
            return GetArrayContainsExpression<T>(property, value);
        } else {
            comparison = Expression.Equal(opLeft, opRight);
        }

        // If comparison is null that means we haven't been able to infer a good comparison
        // expression for it so just defer to a false literal. 
        Expression<Func<T, bool>> falsePredicate = x => false;
        comparison = comparison == null ? falsePredicate : comparison;
        if (isNullable) {
            comparison = AddNullCheck<T>(opLeft, comparison);
        }

        return Expression.Lambda<Func<T, bool>>(comparison ?? Expression.Equal(opLeft, opRight), parameter);
    }

    static Expression<Func<T, bool>> GetArrayContainsExpression<T>(string property, object value) {
        // Bind to the property by name and make the constant value
        // we'll be passing into the Contains() call
        var parameter = Expression.Parameter(typeof(T), "x");
        var opLeft = Expression.Property(parameter, property);
        var opRight = Expression.Constant(value);

        // Create generic method which is bound with the Call Expression below
        var arrContainsRuntimeMethod = typeof(System.Linq.Enumerable).GetMethods()
            .Where(x => x.Name == "Contains")
            .Single(x => x.GetParameters().Length == 2)
            .MakeGenericMethod(value.GetType());

        //LambdaExpression
        var containsCall = Expression.Call(arrContainsRuntimeMethod, opLeft, opRight);

        var finalExpression = AddNullCheck<T>(opLeft, containsCall);

        // Wrap it up in a warm lambda snuggie
        return Expression.Lambda<Func<T, bool>>(finalExpression, false, parameter);
    }

    /// <summary>
    /// Combine a list of expressions exclusively with AndAlso predicate from 
    /// PredicateBuilder. This operator short circuits.
    /// </summary>
    public static Expression<Func<T, bool>>? CombineAnd<T>(IEnumerable<Expression<Func<T, bool>>> predicates) {
        if (predicates.Count() == 0) return null;

        var final = predicates.First();
        foreach (var next in predicates.Skip(1))
            final = PredicateBuilder.And(final, next);

        return final;
    }

    /// <summary>
    /// Combine a list of expressions inclusively with an Or predicate
    /// from PredicateBuilder.
    /// </summary>
    public static Expression<Func<T, bool>>? CombineOr<T>(IEnumerable<Expression<Func<T, bool>>> predicates) {
        if (predicates.Count() == 0) return null;

        var final = predicates.First();
        foreach (var next in predicates.Skip(1))
            final = PredicateBuilder.Or(final, next);

        return final;
    }

    /// <summary>
    /// Combine a list of expressions based on the given operator enum.
    /// </summary>
    public static Expression<Func<T, bool>>? CombinePredicates<T>(IEnumerable<Expression<Func<T, bool>>> predicates, PredicateExpressionPolicyExtensions.RuleOperator op) {
        if (predicates.Count() == 0) return null;

        if (op == RuleOperator.And) {
            return CombineAnd(predicates);
        }
        return CombineOr(predicates);
    }


    /// <summary>
    /// Generate an expression tree targeting an object type based on a given policy.
    /// </summary>
    public static Expression<Func<T, bool>>? GetPredicateExpression<T>(this ExpressionRuleCollection policy) {

        var predicates = new List<Expression<Func<T, bool>>>();
        var typeName = typeof(T).Name;
        foreach (var rule in policy.Rules.Where(x => x.TargetType != null)) {
            if (!(typeof(T).Name.Equals(rule.TargetType, StringComparison.CurrentCultureIgnoreCase))) {
                continue;
            }
            var expression = rule.GetExpression<T>();
            if (expression != null) predicates.Add(expression);
        }

        var expressions = CombinePredicates<T>(predicates, policy.RuleOperator);

        if (expressions == null) {
            System.Diagnostics.Debug.WriteLine($"No predicates available for type: <{typeof(T).Name}> in policy: {policy.Id}");
            return PredicateBuilder.False<T>();
        }

        return expressions;
    }
}