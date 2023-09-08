using Newtonsoft.Json.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;

namespace McRule.Tests {
    public class Filtering {

        People[] peoples = new[] { 
            new People("Sean",   "Confused",  35,  true,  new[] {"muggle"}),
            new People("Sean",   "Actor",     90,  false, new[] {"muggle", "metallurgist"}),
            new People("Bean",   "Runt",      20,  false, new[] {"muggle", "giant"}),
            new People("Robin",  "Comedian",  63,  false, new[] {"muggle", "hilarious"}),
            new People("Tim",    "Enchantor", 999, false, new[] {"magical", "grumpy"}),
            new People("Ragnar", "Viking",    25,  true,  new[] {"muggle", "grumpy"}),
            new People("Lars",   "Viking",    30,  false, new[] {"muggle", "grumpy"}),
            new People("Ferris", "Student",   17,  true,  new[] {"muggle"}),
            new People("Greta",  null,        20,  true,  null),
        };

        public class People
        {
            public string name { get; set; }
            public string kind { get; set; }
            public int? number { get; set; }
            public bool stillWithUs { get; set; }
            public string[] tags { get; set; }

            public People(string name, string kind, int? number, bool stillWithUs, string[] tags = null)
            {
                this.name = name;
                this.kind = kind;
                this.number = number;
                this.stillWithUs = stillWithUs;
                this.tags = tags;
            }

            public override bool Equals(object obj)
            {
                return obj is People other && 
                       name == other.name && 
                       kind == other.kind && 
                       number == other.number && 
                       stillWithUs == other.stillWithUs && 
                       System.Linq.Enumerable.SequenceEqual(tags, other.tags);
            }
        }

        #region testPolicies

        ExpressionPolicy everyKindInclusive = new ExpressionPolicy {
            Name = "Any kind including null",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "*").ToFilterRule(),
            }
        };

        ExpressionPolicy matchNullLiteral = new ExpressionPolicy {
            Name = "Any kind null",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "{{NULL}}").ToFilterRule(),
            }
        };

        ExpressionPolicy matchNullByString = new ExpressionPolicy {
            Name = "Don't think this should work",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "null").ToFilterRule(),
            }
        };

        ExpressionPolicy notSean = new ExpressionPolicy {
            Name = "Not named Sean",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "name", "!Sean").ToFilterRule(),
            }
        };

        ExpressionPolicy eans = new ExpressionPolicy {
            Name = "eans",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "name", "*ean").ToFilterRule(),
                ("People", "name", "~*EAN").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        ExpressionPolicy youngens = new ExpressionPolicy {
            Name = "Young folk",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "number", ">=17").ToFilterRule(),
                ("People", "number", "<30").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        ExpressionPolicy vikings = new ExpressionPolicy {
            Name = "Vikings",
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "~viking").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        ExpressionPolicy muggles = new ExpressionPolicy {
            Name = "Non-magic folk",
            Rules = new List<ExpressionRule>
            {
                ("People", "tags", "muggle").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        ExpressionPolicy notQuiteDead = new ExpressionPolicy {
            Rules = new List<ExpressionRule>
            {
                ("People", "stillWithUs", "true").ToFilterRule(),
            },
            RuleOperator = RuleOperator.And
        };

        ExpressionPolicy deadOrViking = new ExpressionPolicy {
            Rules = new List<ExpressionRule>
            {
                ("People", "stillWithUs", "false").ToFilterRule(),
                ("People", "kind", "Viking").ToFilterRule(),
            },
            RuleOperator = RuleOperator.Or
        };

        #endregion  testPolicies

        [SetUp]
        public void Setup() { }

        [Test]
        public void MatchNullLiteral() {
            var filter = matchNullLiteral.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.IsTrue(folks.All(x => x.kind == null));

            // Test using EF generator
            var efGenerator = PredicateExpressionPolicyExtensions.GetEfExpressionGenerator();
            var efFilter = matchNullLiteral.GeneratePredicateExpression<People>(efGenerator)?.Compile();

            folks = peoples.Where(efFilter);

            Assert.IsTrue(folks.All(x => x.kind == null));
        }

        [Test]
        public void MatchNullByString() {
            var expression = matchNullByString.GetPredicateExpression<People>();
            var filter = expression?.Compile();
            var folks = peoples.Where(filter);

            Assert.Zero(folks.Count(), "A string value of null \"null\" should not evaluate to a null literal so should yield no results here.");

            // Test using EF generator
            var efGenerator = PredicateExpressionPolicyExtensions.GetEfExpressionGenerator();
            var efFilter = matchNullByString.GeneratePredicateExpression<People>(efGenerator)?.Compile();

            folks = peoples.Where(efFilter);

            Assert.Zero(folks.Count(), "A string value of null \"null\" should not evaluate to a null literal so should yield no results here.");
        }

        [Test]
        public void NegativeStringMatch() {
            var filter = notSean.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.Null(folks.FirstOrDefault(x => x.name == "Sean"));
        }

        [Test]
        public void EndsWith() {
            // Filter should match on people who's name ends in 'ean',
            // and case insensitive ends with 'EAN'.
            var filter = eans.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.NotNull(folks);
            Assert.IsTrue(folks.All(x => x.name.EndsWith("ean")));
        }

        [Test]
        public void CombineTwoExpressionsIntoCollection() {
            // Combine two different filters with an And
            var filter = new ExpressionRuleCollection() {
                Rules = new[] {
                    youngens, vikings
                },
                RuleOperator = RuleOperator.And
            }?.GetPredicateExpression<People>()?.Compile();

            var folks = peoples.Where(filter);

            // Match should be exclusive enough to only include Ragnar
            Assert.NotNull(folks);
            Assert.IsTrue(folks.Count() == 1);
            Assert.IsTrue(folks.FirstOrDefault()?.name == "Ragnar");

            // Process both expressions separately to verify they
            // have different results.
            filter = youngens.GetPredicateExpression<People>()?.Compile();
            folks = peoples.Where(filter);
            Assert.IsTrue(folks.Count() > 1);

            filter = vikings.GetPredicateExpression<People>()?.Compile();
            folks = peoples.Where(filter);
            Assert.IsTrue(folks.Count() > 1);

            // Original compound filter with an Or predicate
            filter = new ExpressionRuleCollection() {
                Rules = new[] {
                    youngens, vikings
                },
                RuleOperator = RuleOperator.Or
            }?.GetPredicateExpression<People>()?.Compile();

            folks = peoples.Where(filter);
            Assert.IsTrue(folks.Count() > 1);
            // Should include Vikings by kind and Student by number
            Assert.NotNull(folks.Where(x => x.kind == "Viking"));
            Assert.NotNull(folks.Where(x => x.kind == "Student"));
        }

        [Test]
        public void YoungPeople() {
            var filter = youngens.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.NotNull(folks);
            Assert.IsTrue(folks.All(x => x.number >= 17 && x.number < 30));
        }

        [Test]
        public void FilterListOfObjectsByMemberCollectionContents()
        {
            var generator = new PolicyToExpressionGenerator();
            var generatedFilter = muggles.GeneratePredicateExpression<People>(generator);
            var filteredFolks = peoples.Where(generatedFilter.Compile());
            
            var efGenerator = new PolicyToEFExpressionGenerator();
            var efGeneratedFilter = muggles.GeneratePredicateExpression<People>(efGenerator);
            var efFilteredFolks = peoples.Where(efGeneratedFilter.Compile());

            
            var filter = muggles.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.NotNull(folks);
            Assert.IsTrue(folks.All(x => x.tags.Contains("muggle")));
            Assert.IsTrue(folks.All(x => filteredFolks.Contains(x)));
            Assert.IsTrue(folks.All(x => efFilteredFolks.Contains(x)));
        }

        [Test]
        public void BoolConditional() {
            var filter = notQuiteDead.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.NotNull(folks);
            Assert.IsTrue(folks.Count() > 0);
        }

        [Test]
        public void FilterWhenNoMatchingTypesThrows() {
            // Shouldn't have any filters in the policy for string objects.
            Assert.Throws<ExpressionGeneratorException>(() => { notQuiteDead.GetPredicateExpression<string>(); });
        }

        [Test]
        public void TestPolicyWithOrConditional() {
            var filter = deadOrViking.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.NotNull(folks);
            Assert.NotNull(folks.Where(x => x.kind == "Viking" && x.stillWithUs == false));
            Assert.NotNull(folks.Where(x => x.kind == "Viking" && x.stillWithUs == true));
            
            // Should be either a viking or dead, not neither.
            Assert.Null(folks.FirstOrDefault(x => x.kind != "Viking" && x.stillWithUs == true));
        }

        [Test]
        public void TestBooleanMatches() {
            var filter = notQuiteDead.GetPredicateExpression<People>()?.Compile();
            var folks = peoples.Where(filter);

            Assert.IsTrue(folks.All(x => x.stillWithUs == true));
        }

        [Test]
        public void PolicyEFExpressionShouldNotEmitComparisonTypeStringMatches() {
            var filter = eans.GetPredicateExpression<People>();

            var efGenerator = PredicateExpressionPolicyExtensions.GetEfExpressionGenerator();
            var efFilter = eans.GeneratePredicateExpression<People>(efGenerator);

            Assert.NotNull(filter);
            Assert.AreNotEqual(efFilter.ToString(), filter.ToString());
            Assert.IsTrue(filter.ToString().Contains("CurrentCulture"));
            Assert.IsFalse(efFilter.ToString().Contains("CurrentCulture"), "EF safe string comparision contains a CurrentCulture directive, wrong generator used for AddStringPropertyExpression.");
        }

        [Test]
        public void BaseThrowsOnPoorlyImplementedGenerator()
        {
            var failedGenerator = new FailedExpressionGeneratorBase();
            Assert.Throws<NotImplementedException>(() => { _ = eans.GeneratePredicateExpression<People>(failedGenerator); });
        }
    }

    internal class FailedExpressionGeneratorBase : ExpressionGeneratorBase
    {
        public FailedExpressionGeneratorBase()
        {
        }
    }
}
