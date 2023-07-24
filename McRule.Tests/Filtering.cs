namespace McRule.Tests {
    public class Filtering {

        People[] things = new[] { 
            new People("Sean",   "Confused",  35,  new[] {"muggle"}),
            new People("Sean",   "Actor",     90,  new[] {"muggle", "metallurgist"}),
            new People("Bean",   "Runt",      20,  new[] {"muggle", "giant"}),
            new People("Robin",  "Comedian",  63,  new[] {"muggle", "hilarious"}),
            new People("Tim",    "Enchantor", 999, new[] {"magical", "grumpy"}),
            new People("Ragnar", "Viking",    25,  new[] {"muggle", "grumpy"}),
            new People("Lars",   "Viking",    30,  new[] {"muggle", "grumpy"}),
            new People("Ferris", "Student",   17,  new[] {"muggle"}),
        };

        public record People(string name, string kind, int number, string[] tags = null);

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
    }
}