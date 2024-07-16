using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static McRule.Tests.IDictionarySelector;

namespace McRule.Tests {
    public class CollectionSelector {
        Dictionary<string, string[]> BagOStuff = new Dictionary<string, string[]>() {
            { "Names", new[] {
                        "Sean",
                        "Robin",
                        "Christopher",
                        "Lewis",
                        "James",
                        "Trevor",
                    }
            },
            { "Foods", new[] {
                        "Pizza",
                        "Toast",
                        "Stew",
                        "Omlette",
                    }
            }
        };



        [Test]
        public void StringCollectionContainsCaseInsensitive() {
            var lambda = new ExpressionPolicy {
                Rules = new List<ExpressionRule>
                {
                    (typeof(Dictionary<string, string[]>).Name, "Names", "~sean").ToFilterRule(),
                },
                RuleOperator = RuleOperator.And
            }.GetPredicateExpression<Dictionary<string, string[]>>();

            var filter = lambda.Compile();

            Assert.IsNotNull(filter);

            var foundEm = filter.Invoke(BagOStuff);
            Assert.IsTrue(foundEm);
        }

        [Test]
        public void StringCollectionContainsNegate() {
            var lambda = new ExpressionPolicy {
                Rules = new List<ExpressionRule>
                {
                    (typeof(Dictionary<string, string[]>).Name, "Names", "!Sean").ToFilterRule(),
                },
                RuleOperator = RuleOperator.And
            }.GetPredicateExpression<Dictionary<string, string[]>>();

            var filter = lambda.Compile();

            Assert.IsNotNull(filter);

            var foundEm = filter.Invoke(BagOStuff);
            Assert.IsFalse(foundEm);
        }
    }
}
