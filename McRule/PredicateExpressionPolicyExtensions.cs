using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using static McRule.PredicateExpressionPolicyExtensions;

namespace McRule;

public enum RuleOperator {
    And,
    Or
}

public static partial class PredicateExpressionPolicyExtensions
{
    public static ExpressionRule ToFilterRule(this (string, string, string) tuple)
    {
        return new ExpressionRule(tuple);
    }

    /// <summary>
    /// Prepend the given predicate with a short circuiting null check.
    /// </summary>
    internal static Expression AddNotNullCheck<T>(
        Expression left,
        Expression expression)
    {
        // Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
        var notNull = Expression.NotEqual(left, Expression.Constant(null));

        return Expression.AndAlso(notNull, expression);
    }

    /// <summary>
    /// Returns an expression which executes a ContainsKey method call on IDictionary types
    /// and prepends it to a given expression with an AndAlso operator. Note, this comparison
    /// short circuits so the right hand side will not execute when a key is not found.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="dictKey"></param>
    /// <returns></returns>
    internal static Expression AddContainsKeyCheck<T>(
        Expression left,
        string dictKey,
        Expression<Func<T, bool>> right) {

        // Create generic method which is bound with the Call Expression below
        var containsKeyRuntimeMethod = left.Type.GetMethod("ContainsKey");

        var containsKeyCall = Expression.Call(left, containsKeyRuntimeMethod, Expression.Constant(dictKey));
        var methodExpression = Expression.Lambda<Func<T, bool>>(containsKeyCall, false, right.Parameters);
        return PredicateBuilder.And<T>(methodExpression, right);
    }

    /// <summary>
    /// Test for null value. This is used to test for null literals.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    internal static Expression IsNull<T>(Expression expression) {
        return Expression.Equal(expression, Expression.Constant(null));
    }

    /// <summary>
    /// Applies negative predicate to expression in a lambda.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="operand"></param>
    /// <returns></returns>
    internal static Expression<Func<T, bool>> Negate<T>(Expression<Func<T, bool>> lambda)
    {
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
    internal static Expression GetComparer(string op, Expression left, Expression right) => op switch
    {
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
    internal static Type[] intTypes =
    {
        typeof(Int16), typeof(Int32), typeof(Int64),
        typeof(UInt16), typeof(UInt32), typeof(UInt64),
        typeof(Int16?), typeof(Int32?), typeof(Int64?),
        typeof(UInt16?), typeof(UInt32?), typeof(UInt64?)
    };

    internal static Expression<Func<T, bool>> GetArrayContainsExpression<T>(string property, object value)
    {
        // Bind to the property by name and make the constant value
        // we'll be passing into the Contains() call
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression opLeft = parameter;
        foreach (string p in property.Split(".")) opLeft = Expression.PropertyOrField(opLeft, p);

        var opRight = Expression.Constant(value);

        // Create generic method which is bound with the Call Expression below
        var arrContainsRuntimeMethod = typeof(Enumerable).GetMethods()
            .Where(x => x.Name == "Contains")
            .Single(x => x.GetParameters().Length == 2)
            .MakeGenericMethod(value.GetType());

        //LambdaExpression
        var containsCall = Expression.Call(arrContainsRuntimeMethod, opLeft, opRight);

        var finalExpression = AddNotNullCheck<T>(opLeft, containsCall);

        // Wrap it up in a warm lambda snuggie
        return Expression.Lambda<Func<T, bool>>(finalExpression, false, parameter);
    }

    /// <summary>
    /// Combine a list of expressions exclusively with AndAlso predicate from
    /// PredicateBuilder. This operator short circuits.
    /// </summary>
    public static Expression<Func<T, bool>>? CombineAnd<T>(IEnumerable<Expression<Func<T, bool>>> predicates)
    {
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
    public static Expression<Func<T, bool>>? CombineOr<T>(IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        if (predicates.Count() == 0) return null;

        var final = predicates.First();
        foreach (var next in predicates.Skip(1))
            final = PredicateBuilder.Or(final, next);

        return final;
    }

    /// <summary>
    /// Combine a list of expressions based on the given operator enum.
    /// </summary>
    public static Expression<Func<T, bool>>? CombinePredicates<T>(IEnumerable<Expression<Func<T, bool>>> predicates,
        RuleOperator op)
    {
        if (predicates.Count() == 0) return null;

        if (op == RuleOperator.And)
        {
            return CombineAnd(predicates);
        }

        return CombineOr(predicates);
    }

    /// <summary>
    /// Builds expressions using string member functions StartsWith, EndsWith or Contains as the comparator.
    /// </summary>
    public static Expression<Func<T, bool>> AddStringPropertyExpression<T>(
        Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false)
    {
#if DEBUG
        if (!(filterType == "StartsWith" || filterType == "EndsWith" || filterType == "Contains" ||
              filterType == "Equals"))
        {
            throw new Exception($"filterType must equal StartsWith, EndsWith or Contains. Passed: {filterType}");
        }
#endif
        // Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
        var notNull = Expression.NotEqual(lambda.Body, Expression.Constant(null));

        // Setup calls to: StartsWith, EndsWith, Contains, or Equals,
        // conditionally using character case neutral comparision.
        List<Expression> expressionArgs = new List<Expression>() { Expression.Constant(filter) };

        if (ignoreCase)
        {
            expressionArgs.Add(Expression.Constant(StringComparison.CurrentCultureIgnoreCase));
        }
        else
        {
            expressionArgs.Add(Expression.Constant(StringComparison.CurrentCulture));
        }

        MethodInfo methodInfo =
            typeof(string).GetMethod(filterType, new[] { typeof(string), typeof(StringComparison) });
        var strPredicate = Expression.Call(lambda.Body, methodInfo, expressionArgs);

        Expression filterExpression = Expression.AndAlso(notNull, strPredicate);

        return Expression.Lambda<Func<T, bool>>(
            filterExpression,
            lambda.Parameters);
    }

    static Regex handlebarPattern = new Regex(@"^(\{\{)(?<literal>.+)(\}\})", RegexOptions.ExplicitCapture
                                                                        | RegexOptions.Compiled);
    internal static (bool,LiteralValue?) GetStringValueLiteral(string value) {

        /* Literal values are contained within handlebar syntax {{ literal }}
         * so values should be added to the switch statement below. Matching
         * cases should return early with their literal value.
         * */
        var matched = handlebarPattern.Match(value);
        if (matched.Success) {
            switch (matched.Groups.FirstOrDefault(x => x.Name == "literal")?.Value?.Trim()?.ToLower()) {
                case "null":
                    return (true, new NullValue());
                    break;
            }
        }

        // No literals found.
        return (false, null);
    }

    public static Expression<Func<T, dynamic>> SelectPropertyOrField<T>(string propertyName) {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression opLeft = parameter;
        foreach (var p in propertyName.Split(".")) opLeft = Expression.PropertyOrField(opLeft, p);

        return Expression.Lambda<Func<T, dynamic>>(opLeft, parameter);
    }

    public static ExpressionGenerator GetCoreExpressionGenerator() => new PolicyToExpressionGenerator();

    public static ExpressionGenerator GetEfExpressionGenerator() => new PolicyToEFExpressionGenerator();
}

public class PolicyToExpressionGenerator : ExpressionGeneratorBase
{
    /// <summary>
    /// Builds expressions using string member functions StartsWith, EndsWith or Contains as the comparator.
    /// </summary>
    public override Expression<Func<T, bool>> AddStringPropertyExpression<T>(
        Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false)
    {
        return PredicateExpressionPolicyExtensions.AddStringPropertyExpression(lambda, filter, filterType, ignoreCase);
    }
}

public class PolicyToEFExpressionGenerator : ExpressionGeneratorBase
{
    /// <summary>
    /// Builds expressions using string member functions StartsWith, EndsWith or Contains as the comparator.
    /// </summary>
    public override Expression<Func<T, bool>> AddStringPropertyExpression<T>(
        Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false)
    {
#if DEBUG
        if (!(filterType == "StartsWith" || filterType == "EndsWith" || filterType == "Contains" ||
              filterType == "Equals"))
        {
            throw new Exception($"filterType must equal StartsWith, EndsWith or Contains. Passed: {filterType}");
        }
#endif
        // Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
        var notNull = Expression.NotEqual(lambda.Body, Expression.Constant(null));
        MethodInfo methodInfo = typeof(string).GetMethod(filterType, new[] { typeof(string) });


        // Setup calls to: StartsWith, EndsWith, Contains, or Equals,
        // conditionally using character case neutral comparision.
        List<Expression> expressionArgs = new List<Expression>() { Expression.Constant(filter) };

        var strPredicate = Expression.Call(lambda.Body, methodInfo, expressionArgs);

        Expression filterExpression = Expression.AndAlso(notNull, strPredicate);

        return Expression.Lambda<Func<T, bool>>(
            filterExpression,
            lambda.Parameters);
    }
}



public abstract class ExpressionGeneratorBase : ExpressionGenerator {


    public virtual Expression<Func<T, bool>> AddStringPropertyExpression<T>(
        Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false) {
        throw new NotImplementedException("Must override the AddStringPropertyExpression<T> method in a child class. This one is virtual and shouldn't ever be called.");
    }


    private class MemberResolveResult<T> {
        internal List<Expression<Func<T, bool>>> PreChecks { get; set; } = new List<Expression<Func<T, bool>>>();

        internal Expression Member { get; set; }

        internal bool LOpIsDict { get; set; } = false;
        internal Expression LOp { get; set; }
        internal string DictKey { get; set;  }

        internal void AddNewPreCheck(Expression<Func<T, bool>> lambda) {
            PreChecks.Add(lambda);
        }

        internal Expression<Func<T, bool>> GetPrecheckFunc() {
            if (PreChecks.Count == 0) {
                return default(Expression<Func<T, bool>>);
            }

            return CombineAnd<T>(PreChecks);
        }
    }

    private MemberResolveResult<T> GetMemberByNameForType<T>(string propertyName, ParameterExpression parameter) {

        var result = new MemberResolveResult<T>();

        Expression opLeft = parameter;

        foreach (string p in propertyName.Split(".")) {
            result.LOpIsDict = false;

            if (opLeft.Type.GetInterfaces().Contains(typeof(IDictionary))) {
                result.LOpIsDict = true;
                result.DictKey = p;
                result.LOp = opLeft;

                var dictKey = Expression.Constant(p);

                opLeft = Expression.Property(opLeft, "Item", dictKey);

            } else {
                opLeft = Expression.PropertyOrField(opLeft, p);
            }
        }
        result.Member = opLeft;

        return result;
    }


    /// <summary>
    /// Dynamically build an expression suitable for filtering in a Where clause
    /// </summary>
    public Expression<Func<T, bool>> GetPredicateExpressionForType<T>(string property, string value)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var resolvedMember = GetMemberByNameForType<T>(property, parameter);
        Expression opLeft = resolvedMember.Member;


        (bool literalFound, LiteralValue? processedValue) = GetStringValueLiteral(value);
        var opRight = Expression.Constant(value);
        Expression? comparison = resolvedMember.GetPrecheckFunc();

        // For IComparable types on the left hand side, attempt to parse the right hand side
        // into the same type and use <,>,<=,>=,= prefixes to infer BinaryExpression type.
        // Should work with numerical or datetime values provided they parse correctly.
        // Note, a float on the right hand side will not parse into an integer type implicitly
        // so it is parsed into a decimal value first and then rounded to the nearest integral.
        var lType = opLeft.Type;
        var isNullableValueType = false;
        Type? hasComparable = lType.GetInterface("IComparable");
        Type? hasCollection = lType.GetInterface("ICollection");
        if (hasComparable == null && lType.IsValueType) {
            lType = Nullable.GetUnderlyingType(opLeft.Type);
            // Nullable.GetUnderlyingType only returns a non-null value if the
            // supplied type was indeed a nullable type.
            if (lType != null)
                isNullableValueType = true;
            hasComparable = lType.GetInterface("IComparable");
        }

        if (literalFound) {
            if (processedValue == null) {
                return Expression.Lambda<Func<T, bool>>(IsNull<T>(opLeft), parameter);
            }
        }


        // For string comparisons using wildcards, trim the wildcard characters and pass to the comparison method
        if (lType == typeof(string))
        {
            // Grab the object property for use in the inner expression body
            var strParam = Expression.Lambda<Func<T, string>>(opLeft, parameter);

            // If a string match begins with !, we negate the result.
            var negateResult = false;
            if (value.StartsWith("!"))
            {
                negateResult = true;
                value = value.TrimStart('!');
            }

            // String comparisons which are prefixed with '~' will be evaluated ignoring case.
            // Note: when expression trees are used outside .net, such as with EF to SQL Server,
            // default case sensitivity for that environment may apply implicitly and counter to
            // filter policy intent.
            bool ignoreCase = false;
            if (value.StartsWith('~'))
            {
                ignoreCase = true;
                value = value.TrimStart('~');
            }

            if (value.StartsWith("*") && value.EndsWith("*"))
            {
                comparison = AddStringPropertyExpression<T>(strParam, value.Trim('*'), "Contains", ignoreCase);
            }
            else if (value.StartsWith("*"))
            {
                comparison = AddStringPropertyExpression<T>(strParam, value.TrimStart('*'), "EndsWith", ignoreCase);

            }
            else if (value.EndsWith("*"))
            {
                comparison = AddStringPropertyExpression<T>(strParam, value.TrimEnd('*'), "StartsWith", ignoreCase);
            }
            else
            {
                comparison = AddStringPropertyExpression<T>(strParam, value, "Equals", ignoreCase);
            }

            if (negateResult)
            {
                comparison = Negate<T>((Expression<Func<T,bool>>)comparison);
            }

        }
        else if (hasComparable == typeof(IComparable))
        {
            var operatorPrefix = Regex.Match(value.Trim(), @"^[!<>=]+");
            var operand = (operatorPrefix.Success ? value.Replace(operatorPrefix.Value, "") : value).Trim();

            if (!String.IsNullOrEmpty(operand))
            {
                var parseMethod = lType.GetMethods().FirstOrDefault(x => x.Name == "Parse");
                if (intTypes.Contains(opLeft.Type))
                {
                    operand = operand.Contains(".") ? Math.Round(decimal.Parse(operand)).ToString() : operand;
                }

                var opRightNumerical = parseMethod?.Invoke(null, new string[] { operand });

                opRight = Expression.Constant(opRightNumerical);
                Expression opLeftFinal = isNullableValueType ? Expression.Convert(opLeft, lType) : opLeft;
                comparison = GetComparer(operatorPrefix.Value.Trim(), opLeftFinal, opRight);
            }
        }
        else if (hasCollection == typeof(ICollection))
        {
            return GetArrayContainsExpression<T>(property, value);
        }
        else
        {
            comparison = Expression.Equal(opLeft, opRight);
        }

        // If comparison is null that means we haven't been able to infer a good comparison
        // expression for it so just defer to a false literal.
        Expression<Func<T, bool>> falsePredicate = x => false;
        comparison = comparison == null ? falsePredicate : comparison;
        if (isNullableValueType)
        {
            comparison = AddNotNullCheck<T>(opLeft, comparison);
        }

        // When the left hand side of the comparision implements IDictionary we need to add a ContainsKey
        // method call to assert there's a value to compare against before actually retrieving it by name.
        // A missing key evaluates to false.
        // TODO: use ~ operator to return true for a comparison predicate where a key is missing.
        if (resolvedMember.LOpIsDict) {
            comparison = AddContainsKeyCheck<T>(resolvedMember.LOp, resolvedMember.DictKey, (Expression<Func<T, bool>> )comparison);
        }

        // The value may have the right type and should just be returned.
        Expression<Func<T, bool>> result = default(Expression<Func<T, bool>>);
        if (comparison is Expression<Func<T, bool>> checkedResult && checkedResult != default(Expression<Func<T, bool>>)) {
            result = checkedResult;
        }
        else {
            result = Expression.Lambda<Func<T, bool>>(comparison ?? Expression.Equal(opLeft, opRight), parameter);
        }

        return result;
    }


    /// <summary>
    /// Generate an expression tree targeting an object type based on a given policy.
    /// </summary>
    public Expression<Func<T, bool>>? GetPredicateExpression<T>(IExpressionRuleCollection policy)
    {
        Expression<Func<T, bool>>? expressions = PredicateBuilder.False<T>();

        var predicates = new List<Expression<Func<T, bool>>>();
        var selectedType = typeof(T);
        foreach (var rule in policy.Rules)
        {
            var expression = GetPredicateExpression<T>(rule);
            if (expression != null) { predicates.Add(expression); }
        }

        expressions = CombinePredicates<T>(predicates, policy.RuleOperator);

        if (expressions == null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"No predicates available for type: <{selectedType.Name}> in policy: {policy.Id}");
            throw new ExpressionGeneratorException($"No filter expressions found for type: {selectedType.Name}  FullName: {selectedType.FullName}");
        }

        return expressions;
    }

    public Expression<Func<T, bool>>? GetPredicateExpression<T>(IExpressionPolicy policy) {

        return policy.GeneratePredicateExpression<T>(this);
    }

    public Expression<Func<T, bool>>? GetPredicateExpression<T>(IExpressionRule rule) {
        if (!string.Equals(typeof(T).Name, rule.TargetType, StringComparison.CurrentCultureIgnoreCase)) return null;

        return GetPredicateExpressionForType<T>(rule.Property, rule.Value);
    }

    public Expression<Func<T, bool>>? GetPredicateExpressionOrFalse<T>(IExpressionPolicy policy) {
        var expression = PredicateBuilder.False<T>();
        try { expression = GetPredicateExpression<T>(policy); }
        catch { /* Yeah, that didn't work. */ }
        return expression;
    }
}