# McRule
Rule based filtering using expression trees.

## Predicate Grammar
Predicates are built using simple syntax to select comparison operators and methods for the specified properties on a supplied object of a specified type.
That is the policy specifies the type by name, property to match against and the value the property must have. 
A simple equality comparison is used by default but operators can be prefixed to a policy operand for customized behavior, as shown below.

| Property Type | Operator | Comparison Description                                                                                                           |
|---------------|----------|----------------------------------------------------------------------------------------------------------------------------------|
| string        | *        | Astrisk can appear at the beginning, end or both denoting: StartsWith, EndsWith or Contains respectively.                        |
| string        | ~        | Denotes case-insensitive comparison when used in .net, things that translate the resulting expression tree may not respect this. |
| IComparable   | >        | Greater-than comparison.                                                                                                         |
| IComparable   | >=       | Greater-than or equal to comparison.                                                                                             |
| IComparable   | <        | Less-than comparison.                                                                                                            |
| IComparable   | <=       | Less-than or equal to comparison.                                                                                                |
| IComparable   | <>, !=, ! | Not-equal to comparison |

> Note: the IComparable interface is mostly used for numerical types but custom types with comparison providers may work at runtime.

### Examples
