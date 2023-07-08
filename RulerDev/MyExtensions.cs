using System;
using System.Linq;
using System.Linq.Expressions;
using McRule;
using Newtonsoft.Json;

namespace RulerDev;

public static class MyExtensions {
    
    public static object Dump(this object thing, params string[]? msgs) {
        if (msgs != null && msgs.Length > 0) {
            Console.WriteLine("> {0}\n", string.Join(", ", msgs));
        }

        if (thing is String) {
            Console.WriteLine(thing);
        } else if (thing is Expression ex) {
            Console.WriteLine(ex.ToString());
        }else {
            Console.WriteLine(JsonConvert.SerializeObject(thing, Formatting.Indented));
        }
        return thing;
    }

}


public static class FilterRuleManager
{
    
    private static FilterRuleRepository _repo = new FilterRuleRepository();
    public static IEnumerable<T> WhereFilteredByPolicy<T>(this IEnumerable<T> sequence, string[] roles)
    {
        var rules = roles.Select(x => _repo.GetRule(x)?.GetPredicateExpression<T>()).Where(x => x != null);
        if (rules == null || rules.Count() == 0)
        {
            // No policy matching the specified role found so filter out everything
            // TODO log this info
            return sequence.Where(x => false);
        }
        
        var predicates = PredicateExpressionPolicyExtensions.CombinePredicates(rules, PredicateExpressionPolicyExtensions.RuleOperator.And).Compile();
        return sequence.Where(predicates);
    }
    
    public static IQueryable<T> WhereFilteredByPolicy<T>(this IQueryable<T> sequence, string[] roles)
    {
        return ((IEnumerable<T>)sequence).WhereFilteredByPolicy(roles).AsQueryable();
    }
    
    public static FilterRuleRepository Repo
    {
        get => _repo;
    }
}

public class FilterRuleRepository
{
    private Dictionary<string, FilterRuleCollection> roleRuleMap { get; } =
        new Dictionary<string, FilterRuleCollection>();

    public void AddRule(string role, FilterRuleCollection rule) => roleRuleMap.Add(role, rule);
    
    public FilterRuleCollection? GetRule(string role)
    {
        FilterRuleCollection result = null;
        roleRuleMap.TryGetValue(role, out result);
        return result;
    }
}