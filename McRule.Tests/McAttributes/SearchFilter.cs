using McRule.Tests.Models;
using SMM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static SMM.FilterPatternHelpers;

namespace McRule.Tests.McAttributes {
    public class SearchFilter {

        [SetUp]
        public void SetUp() {

        }

        [Test]
        public void SearchCriteriaWithASpaceCombinesSearchFilters() {
            var fixture = new SearchTestFixture();
            fixture.SearchCriteria = "Williams Robin ";

            Assert.DoesNotThrow(() => fixture.DoSearch());
            var query = fixture.SearchExpression?.Compile()?.ToString();
            Console.WriteLine(query);
        }
    }

    internal class SearchTestFixture {
        public string SearchCriteria { get; set; }

        public Expression<Func<User, bool>> SearchExpression { get; set; }

        public void DoSearch() {
            SearchExpression = GetUserFilter();
        }

        private Expression<Func<User, bool>> GetUserFilter() {
            if (String.IsNullOrEmpty(SearchCriteria)) return PredicateBuilder.False<User>();


            var efGenerator = new SMM.NpgsqlGenerator();

            var filter = new ExpressionRuleCollection() {
                TargetType = nameof(Models.User),
            };
            filter.RuleOperator = RuleOperator.Or;
            var _rules = new List<IExpressionPolicy>();

            IExpressionPolicy subFilter = null;

            // If the search criteria contains spaces, it's likely the intent is to match against display name,
            // as it's the only property where users likely have spaces in the value.
            if (SearchCriteria.Trim().Contains(' ')) {
                _rules.Add(
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.DisplayName), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.Contains | FilterOptions.IgnoreCase)))
                );
                subFilter = new ExpressionRuleCollection();
                ((ExpressionRuleCollection)subFilter).RuleOperator = RuleOperator.And;
                var subRules = new List<IExpressionPolicy>();
                foreach (var x in SearchCriteria.Trim().Split()) subRules.Add(new ExpressionRule((nameof(Models.User), nameof(Models.User.Mail), x.Trim()?.AddFilterOptionsIfNotSpecified(FilterOptions.Contains | FilterOptions.IgnoreCase))));
                ((ExpressionRuleCollection)subFilter).Rules = subRules;

                filter.Rules = _rules;

                // TODO make this kind of thing eaiser
                var metaExpression = new ExpressionRuleCollection() {
                    TargetType = nameof(Models.User),
                    RuleOperator = RuleOperator.Or,
                    Rules = new[] { filter, subFilter }
                };

                return efGenerator.GetPredicateExpression<User>((IExpressionRuleCollection)metaExpression) ?? PredicateBuilder.False<User>();
            } else {
                _rules.AddRange(new[] {
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.Mail), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.Upn), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.EmployeeId), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredGivenName), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredSurname), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                });
            }
            filter.Rules = _rules;

            return efGenerator.GetPredicateExpression<User>((IExpressionRuleCollection)filter) ?? PredicateBuilder.False<User>();
        }
    }
}

namespace McRule.Tests.Models {
    public class User {

        public DateTime? LastFetched { get; set; } = null;

        public DateTime? Merged { set; get; } = null;

        public DateTime? Modified { get; set; } = null;

        public DateTime? Created { get; set; } = null;

        public bool Enabled { get; set; }

        public bool Deleted { get; set; }

        public string? Tenant { get; set; }

        public Guid AadId { get; set; }

        public string? Upn { get; set; }

        public string? Mail { get; set; }

        public string? DisplayName { get; set; }

        public string? EmployeeId { get; set; }

        public string? AdEmployeeId { get; set; }

        public string? HrEmployeeId { get; set; }

        public string? Wid { get; set; }

        public string? CreationType { get; set; }

        public string? Company { get; set; }

        public string? Title { get; set; }

        public string? PreferredGivenName { get; set; }

        public string? PreferredSurname { get; set; }

        public string? Articles { get; set; }

        public string? Pronouns { get; set; }

        public string? OnPremiseDn { get; set; }
    }
}