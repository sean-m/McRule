
using McRule;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

using static RulerDev.MyExtensions;

var users = new List<User> {
    new User("Sean","McArdle","971-900-7335","503-555-1245", "SDC", "DAS", new string[] {"IT", "Admin"}),
    new User("Brian","Chamberland","971-900-7335","503-555-1245", "SDC", "DAS"),
    new User("Brian","Tong","971-900-7335","503-555-1245", "SDC", "DAS"),
    new User("Brian","Chytka","971-900-7335","503-555-1245", "3990 Fairview", "ODHSOHA"),
    new User("Fariborz", "Pakseresht","971-900-7335","503-555-1245", "500 Summer", "ODHS"),
    new User("Simon","Hayes","971-900-7335","503-555-1245", "3990 Fairview", "OHA"),
    new User("Tess","McArdle","971-555-7335","503-555-5555", "Home", "HomeSchool"),
    new User("Ian", "McCloud","971-555-7335","503-555-5555", "Home", "DAS")
}.AsQueryable();

//users.Dump();

var filterPolicy = new ExpressionPolicy
{
    Name = "DHS or OHA",
    Properties = new string[] { }, // Can't do anything with this yet
    Rules = new List<ExpressionRule>()
    {
        ("User", "tags", "IT").ToFilterRule(),
        ("User", "agency", "ODHSOHA").ToFilterRule(),
        ("User", "agency", "ODHS").ToFilterRule(),
        ("User", "agency", "OHA").ToFilterRule(),
        ("Group", "agency", "OHA").ToFilterRule(),
    },
    RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.Or
};
var filterExpression = filterPolicy.GetPredicateExpression<User>();
filterPolicy.Dump(filterPolicy.Name);
users.Where(filterExpression).Dump($"{filterPolicy.Name} operator {filterPolicy.RuleOperator}");



filterPolicy = new ExpressionPolicy
{
    Name = "All DAS *ians case sensitive",
    Properties = new string[] { }, // Can't do anything with this yet
    Rules = new List<ExpressionRule>
    {
        ("User", "first", "*ian").ToFilterRule(), 
        ("User", "agency", "DAS").ToFilterRule()
    }
};
filterExpression = filterPolicy.GetPredicateExpression<User>();
filterPolicy.Dump(filterPolicy.Name);
users.Where(filterExpression).Dump($"operator {filterPolicy.RuleOperator}");

filterPolicy = new ExpressionPolicy
{
    Name = "All DAS *ians case insensitive",
    Properties = new string[] { }, // Can't do anything with this yet
    Rules = new List<ExpressionRule>
    {
        ("User", "first", "~*ian").ToFilterRule(), 
        ("User", "agency", "DAS").ToFilterRule()
    }
};
filterExpression = filterPolicy.GetPredicateExpression<User>();
filterPolicy.Dump(filterPolicy.Name);
users.Where(filterExpression).Dump($"operator {filterPolicy.RuleOperator}");

var dynamicFilterDAS = PredicateExpressionPolicyExtensions.GetPredicateExpressionForType<User>("agency", "DAS");
users.Where(dynamicFilterDAS).Dump("DAS users");

var dynamicFilterBrian = PredicateExpressionPolicyExtensions.GetPredicateExpressionForType<User>("first","Brian");

var policies = new List<Expression<Func<User,bool>>>();
policies.Add(dynamicFilterDAS);
policies.Add(dynamicFilterBrian);
var combined = PredicateExpressionPolicyExtensions.CombineAnd(policies);

users.Where(dynamicFilterBrian).Dump("All *ian's");
(from u in users.Where(combined)
    select new { u.first, u.last, u.agency}).Dump("All Brian's from DAS");



record User(string first, string last, string workPhone, string homePhone, string workAddress, string agency, string[] tags = null);


