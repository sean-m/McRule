using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using ConsoleTables;
using McRule;
using Newtonsoft.Json;

namespace RulerDev;

public static class MyExtensions {
    
    public static object Dump(this object thing, params string[]? msgs) {
        if (msgs != null && msgs.Length > 0) {
            Console.WriteLine("> {0}\n", string.Join(", ", msgs));
        }

        var thingType = thing.GetType();
        var isEnumerable = thingType.GetInterface("IEnumerable");
        
        if (thing is String) {
            Console.WriteLine(thing);
        } else if (thing is Expression ex) {
            Console.WriteLine(ex.ToString());
        } else if (isEnumerable != null) {
            var thingEnumerable = (IEnumerable)thing;
            var count = 0;
            foreach (var _ in thingEnumerable) count++;
            
            if ((count < 1)) goto end;


            var thingEnum = thingEnumerable.GetEnumerator();
            if (!thingEnum.MoveNext()) goto end;
            
            var first = thingEnum.Current;
            var props = first.GetType().GetProperties();

            var table = new ConsoleTable(props.Select(x => x.Name).ToArray());
            foreach (var e in thingEnumerable) {
                var row = new List<object>();
                foreach (var p in props) {
                    row.Add(FormatProperty(p.GetValue(e)));
                }
                
                table.AddRow(row.ToArray());
            }
            
            table.Write(Format.Minimal);
        } else {
            Console.WriteLine(ToKvText(thing));
        }
        
        end:
        return thing;
    }
    
    public static string ToKvText(this object entry) {
        if (entry == null) return string.Empty;

        StringBuilder sb = new StringBuilder();

        var properties = entry.GetType().GetProperties();
        var longestPropertyLength = properties.Select(x => x.Name.Length).Max();
        int indentation = longestPropertyLength + 3;

        foreach (var p in properties) {
            sb.Append(p.Name.PadRight(longestPropertyLength));
            sb.Append(" : ");
            bool multiLine = false;
            var thing = p.GetValue(entry);
            if (thing is string) {
                foreach (var value in (thing ?? "NULL").ToString().Split("\n")) {
                    var strValue = FormatProperty((value) ?? "NULL");
                    if (multiLine) {
                        sb.AppendLine((strValue).PadLeft(indentation + strValue.Length));
                    } else {
                        sb.AppendLine((strValue).PadRight(longestPropertyLength));
                        multiLine = true;
                    }
                }
            } else {
                var value = FormatProperty(thing);
                sb.AppendLine((value).PadLeft(value.Length));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format properties for being printed inside a table. For array objects with fewer than 4
    /// elements, wrap em in square brackets and comma delimit them. 4 or more, take the first
    /// three that way then append ellipsis and the number of elements. This is more for
    /// debugging than anything else. 
    /// </summary>
    /// <param name="Property"></param>
    /// <returns></returns>
    private static string FormatProperty(dynamic Property) {
        if (Property == null) return "*NULL";

        if (Property is String s) return s;
        
        if (Property.GetType().GetInterface("IEnumerable") != null) {
            if (Enumerable.Count(Property) <= 5) {
                return $"[{String.Join(", ", Property)}]";
            } else {
                var elems = new List<string>();
                var count = 0;
                foreach (var el in Property) {
                    count++;
                    elems.Add(el.ToString());
                    if (count >= 3) break;
                }
                
                return $"[{String.Join(", ", elems)}...({Enumerable.Count(Property)})]";
            }
        }
        
        return Property?.ToString();
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
        
        var predicates = PredicateExpressionPolicyExtensions.CombinePredicates(rules, RuleOperator.And).Compile();
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
    private Dictionary<string, ExpressionRuleCollection> roleRuleMap { get; } =
        new Dictionary<string, ExpressionRuleCollection>();

    public void AddRule(string role, ExpressionRuleCollection rule) => roleRuleMap.Add(role, rule);
    
    public ExpressionRuleCollection? GetRule(string role)
    {
        ExpressionRuleCollection result = null;
        roleRuleMap.TryGetValue(role, out result);
        return result;
    }
}