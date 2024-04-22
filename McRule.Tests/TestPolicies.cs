using McRule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McRule.Tests {
    public static class TestPolicies {

        #region testPolicies

        public static ExpressionPolicy everyKindInclusive = new ExpressionPolicy {
            Name = "Any kind including null",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "*").ToFilterRule(),
            }
        };

        public static ExpressionPolicy matchNullLiteral = new ExpressionPolicy {
            Name = "Any kind null",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "{{NULL}}").ToFilterRule(),
            }
        };

        public static ExpressionPolicy matchNullByString = new ExpressionPolicy {
            Name = "Don't think this should work",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "null").ToFilterRule(),
            }
        };

        public static ExpressionPolicy notSean = new ExpressionPolicy {
            Name = "Not named Sean",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "name", "!Sean").ToFilterRule(),
            }
        };

        public static ExpressionPolicy eans = new ExpressionPolicy {
            Name = "eans",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "name", "*ean").ToFilterRule(),
                ("People", "name", "~*EAN").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        public static ExpressionPolicy youngens = new ExpressionPolicy {
            Name = "Young folk",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "number", ">=17").ToFilterRule(),
                ("People", "number", "<30").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        public static ExpressionPolicy vikings = new ExpressionPolicy {
            Name = "Vikings",
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "~viking").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        public static ExpressionPolicy muggles = new ExpressionPolicy {
            Name = "Non-magic folk",
            Rules = new List<ExpressionRule>
                    {
                ("People", "tags", "muggle").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        public static ExpressionPolicy notQuiteDead = new ExpressionPolicy {
            Rules = new List<ExpressionRule>
                    {
                ("People", "stillWithUs", "true").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        public static ExpressionPolicy deadOrViking = new ExpressionPolicy {
            Rules = new List<ExpressionRule>
                    {
                ("People", "stillWithUs", "false").ToFilterRule(),
                ("People", "kind", "Viking").ToFilterRule(),
            },
            RuleOperator = RuleOperator.Or
        };

        #endregion  testPolicies
    }
}
