
using Ruler;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

using static RulerDev.MyExtensions;

var users = new List<User> {
    new User("Sean","McArdle","971-900-7335","503-555-1245", "SDC", "DAS"),
    new User("Brian","Chamberland","971-900-7335","503-555-1245", "SDC", "DAS"),
    new User("Brian","Tong","971-900-7335","503-555-1245", "SDC", "DAS"),
    new User("Brian","Chytka","971-900-7335","503-555-1245", "3990 Fairview", "ODHSOHA"),
    new User("Fariborz", "Pakseresht","971-900-7335","503-555-1245", "500 Summer", "ODHS"),
    new User("Simon","Hayes","971-900-7335","503-555-1245", "3990 Fairview", "OHA"),
    new User("Tess","McArdle","971-555-7335","503-555-5555", "Home", "HomeSchool")
}.AsQueryable();

//users.Dump();

var filterPolicy = new FilterPolicy
{
    name = "DHS or OHA",
    properties = new string[] { }, // Can't do anything with this yet
    scope = new List<(string, string, string)>
    {
        ("User", "agency", "ODHSOHA"),
        ("User", "agency", "ODHS"),
        ("User", "agency", "OHA")
    },
    ruleOperator = FilterPolicyExtensions.RuleOperator.Or
};
var filterExpression = filterPolicy.GetFilterExpression<User>();
filterPolicy.Dump(filterPolicy.name);
users.Where(filterExpression).Dump($"{filterPolicy.name} operator {filterPolicy.ruleOperator}");



filterPolicy = new FilterPolicy
{
    name = "All DAS Brians",
    properties = new string[] { }, // Can't do anything with this yet
    scope = new List<(string, string, string)>
    {
        ("User", "first", "Brian"), 
        ("User", "agency", "DAS")
    }
};
filterExpression = filterPolicy.GetFilterExpression<User>();
filterPolicy.Dump(filterPolicy.name);
users.Where(filterExpression).Dump($"operator {filterPolicy.ruleOperator}");



var dynamicFilterDAS = FilterPolicyExtensions.GetFilterExpressionForType<User>("agency", "DAS");
users.Where(dynamicFilterDAS).Dump("DAS users");

var dynamicFilterBrian = FilterPolicyExtensions.GetFilterExpressionForType<User>("first","Brian");

var policies = new List<Expression<Func<User,bool>>>();
policies.Add(dynamicFilterDAS);
policies.Add(dynamicFilterBrian);
var combined = FilterPolicyExtensions.CombineAnd(policies);

users.Where(dynamicFilterBrian).Dump("All Brian's");
(from u in users.Where(combined)
    select new { u.first, u.last, u.agency}).Dump("All Brian's from DAS");



record User(string first, string last, string workPhone, string homePhone, string workAddress, string agency);


