
using System.Linq;
using System.Linq.Expressions;

namespace Ruler;


public record FilterPolicy(
	string name, 
	string[] properties, 
	IEnumerable<(string, string)> scope, 
	FilterPolicyExtensions.RuleOperator ruleOperator=FilterPolicyExtensions.RuleOperator.And
	);

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
	public static Expression<Func<T, bool>> True<T>() { return f => true; }
	public static Expression<Func<T, bool>> False<T>() { return f => false; }

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
	public enum RuleOperator {
		And,
		Or
	}

	// Dynamicall build an expression suitable for filtering in a Where clause
	public static Expression<Func<T, bool>> GetFilterForType<T>(string property, string value)
	{
		var parameter = Expression.Parameter(typeof(T), "x");
		var opLeft = Expression.Property(parameter, property);

		var opRight = Expression.Constant(value);
		var comparison = Expression.Equal(opLeft, opRight);

		return Expression.Lambda<Func<T, bool>>(comparison, parameter);
	}

	// Combine a list of expressions inclusively
	public static Expression<Func<T, bool>> CombineAnd<T>(IEnumerable<Expression<Func<T, bool>>> predicates)
	{
		if (predicates.Count() == 0) return null;

		var final = predicates.First();
		foreach (var next in predicates.Skip(1))
			final = PredicateBuilder.And(final, next);
	
		return final;
	}


	// Combine a list of expressions inclusively
	public static Expression<Func<T, bool>> CombineOr<T>(IEnumerable<Expression<Func<T, bool>>> predicates)
	{
		if (predicates.Count() == 0) return null;

		var final = predicates.First();
		foreach (var next in predicates.Skip(1))
			final = PredicateBuilder.Or(final, next);

		return final;
	}


	// Combine a list of expressions inclusively
	public static Expression<Func<T, bool>> CombinePredicates<T>(IEnumerable<Expression<Func<T, bool>>> predicates, FilterPolicyExtensions.RuleOperator op)
	{
		if (predicates.Count() == 0) return null;

		if (op == RuleOperator.And)
		{
			return CombineAnd(predicates);
		}
		return CombineOr(predicates);
	}


	public static Expression<Func<T, bool>> GetFilterExpression<T>(this FilterPolicy policy)
	{
		var predicates = new List<Expression<Func<T, bool>>>();
		foreach (var constraints in policy.scope)
		{
			predicates.Add(GetFilterForType<T>(constraints.Item1, constraints.Item2));
		}
		
		return CombinePredicates<T>(predicates, policy.ruleOperator);
	}
}