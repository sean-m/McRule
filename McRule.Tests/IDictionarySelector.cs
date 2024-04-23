using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using static McRule.Tests.TestPolicies;

namespace McRule.Tests {
    public class IDictionarySelector {

        public class SomeContext {
            public string Name { get; set; }
            public bool Authorized { get; set; } = false;
            public ContextStringDictionary Context { get; set; }
        }

        public class ContextStringDictionary : Dictionary<string, string> { }

        public List<SomeContext> SomeContexts = new List<SomeContext>();

        [SetUp] public void SetUp() {
            SomeContexts.Add(new SomeContext() {
                Name = "Me",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Sean"},
                    { "Surname", "McArdle" },
                    { "Department", "IT" },
                    { "Team", "Cloud" },
                }
            });

            SomeContexts.Add(new SomeContext() {
                Name = "Dog",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Navi"},
                    { "Surname", "McArdle" },
                    { "Department", "Security" },
                    { "Team", "Pets" },
                }
            });

            SomeContexts.Add(new SomeContext() {
                Name = "Son",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Thing-1"},
                    { "Surname", "McArdle" },
                    { "Department", "IT" },
                    { "Team", "Children" },
                }
            });

            SomeContexts.Add(new SomeContext() {
                Name = "Son",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Thing-2"},
                    { "Surname", "McArdle" },
                    { "Team", "Children" },
                }
            });
        }


        [Test]
        public void CanSelectDictionaryValuesByKey() {
            var lambda = itPeople.GetPredicateExpression<ContextStringDictionary>();
            Console.WriteLine(lambda);

            var filter = lambda.Compile();
            var filteredContexts = SomeContexts.Select(x => x.Context)
                .Where(x => x.ContainsKey("Department")) // Skip entry with a missing key
                .Where(filter)?.ToList();

            Assert.NotNull(filteredContexts);
            Assert.AreEqual(filteredContexts.Count, 2);
        }


        [Test]
        public void CanSelectDictionaryValueWithContainsKeyCheck() {
            var lambda = itPeople.GetPredicateExpression<ContextStringDictionary>();
            Console.WriteLine(lambda);

            var filter = lambda.Compile();
            var filteredContexts = SomeContexts.Select(x => x.Context)
                .Where(filter)?.ToList();

            Assert.NotNull(filteredContexts);
            Assert.AreEqual(filteredContexts.Count, 2);
        }
    }
}
