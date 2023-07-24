namespace McRule.Tests {
    public class Filtering {

        People[] things = new[] { 
            new People("Sean",   "Confused",  35,  true,  new[] {"muggle"}),
            new People("Sean",   "Actor",     90,  false, new[] {"muggle", "metallurgist"}),
            new People("Bean",   "Runt",      20,  false, new[] {"muggle", "giant"}),
            new People("Robin",  "Comedian",  63,  false, new[] {"muggle", "hilarious"}),
            new People("Tim",    "Enchantor", 999, false, new[] {"magical", "grumpy"}),
            new People("Ragnar", "Viking",    25,  true,  new[] {"muggle", "grumpy"}),
            new People("Lars",   "Viking",    30,  false, new[] {"muggle", "grumpy"}),
            new People("Ferris", "Student",   17,  true,  new[] {"muggle"}),
        };

        public record People(string name, string kind, int? number, bool stillWithUs, string[] tags = null);

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
            RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.And
        };

        ExpressionPolicy youngens = new ExpressionPolicy {
            Name = "Young folk",
            Properties = new string[] { }, // Can't do anything with this yet
            Rules = new List<ExpressionRule>
            {
                ("People", "number", ">=17").ToFilterRule(),
                ("People", "number", "<30").ToFilterRule(),
            },
            RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.And
        };

        ExpressionPolicy vikings = new ExpressionPolicy {
            Name = "Vikings",
            Rules = new List<ExpressionRule>
            {
                ("People", "kind", "~viking").ToFilterRule(),
            },
            RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.And
        };

        ExpressionPolicy muggles = new ExpressionPolicy {
            Name = "Non-magic folk",
            Rules = new List<ExpressionRule>
            {
                ("People", "tags", "muggle").ToFilterRule(),
            },
            RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.And
        };

        ExpressionPolicy notQuiteDead = new ExpressionPolicy {
            Rules = new List<ExpressionRule>
            {
                ("People", "stillWithUs", "true").ToFilterRule(),
            },
            RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.And
        };

        ExpressionPolicy deadOrViking = new ExpressionPolicy {
            Rules = new List<ExpressionRule>
            {
                ("People", "stillWithUs", "false").ToFilterRule(),
                ("People", "kind", "Viking").ToFilterRule(),
            },
            RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.Or
        };

        [SetUp]
        public void Setup() {

        }

        [Test]
        public void NegativeStringMatch() {
            var filter = notSean.GetExpression<People>()?.Compile();
            var folks = things.Where(filter);

            Assert.Null(folks.FirstOrDefault(x => x.name == "Sean"));
        }

        [Test]
        public void EndsWith() {
            // Filter should match on people who's name ends in 'ean',
            // and case insensitive ends with 'EAN'.
            var filter = eans.GetExpression<People>()?.Compile();
            var folks = things.Where(filter);

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
                RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.And
            }?.GetExpression<People>()?.Compile();

            var folks = things.Where(filter);

            // Match should be exclusive enough to only include Ragnar
            Assert.NotNull(folks);
            Assert.IsTrue(folks.Count() == 1);
            Assert.IsTrue(folks.FirstOrDefault()?.name == "Ragnar");

            // Process both expressions separately to verify they
            // have different results.
            filter = youngens.GetExpression<People>()?.Compile();
            folks = things.Where(filter);
            Assert.IsTrue(folks.Count() > 1);

            filter = vikings.GetExpression<People>()?.Compile();
            folks = things.Where(filter);
            Assert.IsTrue(folks.Count() > 1);

            // Original compound filter with an Or predicate
            filter = new ExpressionRuleCollection() {
                Rules = new[] {
                    youngens, vikings
                },
                RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.Or
            }?.GetExpression<People>()?.Compile();

            folks = things.Where(filter);
            Assert.IsTrue(folks.Count() > 1);
            // Should include Vikings by kind and Student by number
            Assert.NotNull(folks.Where(x => x.kind == "Viking"));
            Assert.NotNull(folks.Where(x => x.kind == "Student"));
        }

        [Test]
        public void YoungPeople() {
            var filter = youngens.GetPredicateExpression<People>()?.Compile();
            var folks = things.Where(filter);

            Assert.NotNull(folks);
            Assert.IsTrue(folks.All(x => x.number >= 17 && x.number < 30));
        }

        [Test]
        public void MugglesByTag() {
            var filter = muggles.GetExpression<People>()?.Compile();
            var folks = things.Where(filter);

            Assert.NotNull(folks);
            Assert.IsTrue(folks.All(x => x.tags.Contains("muggle")));
        }

        [Test]
        public void BoolConditional() {
            var filter = notQuiteDead.GetExpression<People>()?.Compile();
            var folks = things.Where(filter);

            Assert.NotNull(folks);
            Assert.IsTrue(folks.Count() > 0);
        }

        [Test]
        public void NullFilterWhenNoMatchingTypes() {
            // Shouldn't have any filters in the policy for string objects.
            var filter = notQuiteDead.GetExpression<string>()?.Compile();
            
            Assert.Null(filter);
        }

        [Test]
        public void TestPolicyWithOrConditional() {
            var filter = deadOrViking.GetExpression<People>()?.Compile();
            var folks = things.Where(filter);

            Assert.NotNull(folks);
            Assert.NotNull(folks.Where(x => x.kind == "Viking" && x.stillWithUs == false));
            Assert.NotNull(folks.Where(x => x.kind == "Viking" && x.stillWithUs == true));
            
            // Should be either a viking or dead, not neither.
            Assert.Null(folks.FirstOrDefault(x => x.kind != "Viking" && x.stillWithUs == true));
        }
    }
}