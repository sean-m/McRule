# McRule
Rule based filtering using expression trees.

## Predicate Grammar
Predicates are built using simple syntax to select comparison operators and methods for the specified properties on a supplied object of a specified type.
That is the policy specifies the type by name, property to match against and the value the property must have. 
A simple equality comparison is used by default but operators can be prefixed to a policy operand for customized behavior, as shown below.

| Property Type | Operator  | Comparison Description                                                                                                                                                                                                                                                                                                                         |
|---------------|-----------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| string        | *         | Astrisk can appear at the beginning, end or both denoting: StartsWith, EndsWith or Contains respectively.                                                                                                                                                                                                                                      |
| string        | ~         | Denotes case-insensitive comparison when used in .net, things that translate the resulting expression tree may not respect this. EF core for instance won't bind a Contains with case insensitive comparison from the string methods at all, results in a runtime error. Note: when used with wildcard, tilde operator must be first: '~*foo'. |
| string	    | !         | Negative expression. Must prefix all other operators.                                                                                                                                                                                                                                                                                          |
| IComparable   | >         | Greater-than comparison.                                                                                                                                                                                                                                                                                                                       |
| IComparable   | >=        | Greater-than or equal to comparison.                                                                                                                                                                                                                                                                                                           |
| IComparable   | <         | Less-than comparison.                                                                                                                                                                                                                                                                                                                          |
| IComparable   | <=        | Less-than or equal to comparison.                                                                                                                                                                                                                                                                                                              |
| IComparable   | <>, !=, ! | Not-equal to comparison.                                                                                                                                                                                                                                                                                                                      |

> Note: the IComparable interface is mostly used for numerical types but custom types with comparison providers may work at runtime.

### Literal Values
Literal values, as needed, use handlbar syntax: {{ value }}. Null checks are implicitly added to most expressions but sometimes you need an expression that evaluates true for null values. In that case, a null literal is represented as {{null}}.
Case sensitivity doesn't matter, nor does internal whitespace inside the braces. Values are interpretted like so:
```csharp
var matched = handlebarPattern.Match(value);
if (matched.Success) {
    switch (matched.Groups.FirstOrDefault(x => x.Name == "literal")?.Value?.Trim()?.ToLower()) {
        case "null":
            return (true, new NullValue());
            break;
    }
}
```

### Examples


## Notes

Publish nuget package:
```
dotnet nuget push McRule.0.0.5.nupkg --api-key <api key> --source https://api.nuget.org/v3/index.json
```