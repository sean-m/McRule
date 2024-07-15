using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using static McRule.Tests.TestPolicies;
using static SMM.ConsoleHelpers;

namespace McRule.Tests {
    public class IDictionarySelector {

        public class SomeContext {
            public string Name { get; set; }
            public bool Authorized { get; set; } = false;
            public ContextStringDictionary Context { get; set; }
        }

        public class ContextStringDictionary : Dictionary<string, string> { }

        public List<SomeContext> SomeContexts = new List<SomeContext>() {
            new SomeContext {
                Name = "Me",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Sean"},
                    { "Surname", "McArdle" },
                    { "Department", "IT" },
                    { "Team", "Cloud" },
                }
            },
            new SomeContext {
                Name = "Dog",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Navi"},
                    { "Surname", "McArdle" },
                    { "Department", "Security" },
                    { "Team", "Pets" },
                }
            },
            new SomeContext {
                Name = "Son",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Thing-1"},
                    { "Surname", "McArdle" },
                    { "Department", "IT" },
                    { "Team", "Children" },
                }
            },
            new SomeContext() {
                Name = "Son",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Thing-2"},
                    { "Surname", "McArdle" },
                    { "Team", "Children" },
                }
            },
            new SomeContext() {
                Name = "Nerd Son",
                Context = new ContextStringDictionary() {
                    { "GivenName", "Thing-2"},
                    { "Surname", "McArdle" },
                    { "Team", "Children" },
                    { "Department", "it" },
                }
            }
        };

        public class ContextStringListDictionary : Dictionary<string, string[]> { }
        public List<ContextStringListDictionary> lists = new List<ContextStringListDictionary> {
            new ContextStringListDictionary {
                { "role", new [] { "User", "Admin" } },
            },
            new ContextStringListDictionary {
                { "role", new [] { "User", "Visitor", "Guest" } },
            },
            new ContextStringListDictionary {
                { "role", new [] { "ServiveAccount", "Admin" } },
            }
        };

        [SetUp] public void SetUp() {

        }

        [Test]
        public void InheritedDictionaryOfListsTest() {
            var lambda = new ExpressionPolicy {
                Rules = new List<ExpressionRule>
                {
                    ("ContextStringListDictionary", "role", "User").ToFilterRule(), // Same rule but with nested selector
                },
                RuleOperator = RuleOperator.And
            }.GetPredicateExpression<ContextStringListDictionary>();

            var filter = lambda.Compile();

            Assert.IsNotNull(filter);

            var filtered = lists.Where(filter).ToList();
            Assert.AreEqual(2, filtered.Count);
        }

        [Test]
        public void CanSelectDictionaryValuesByKey() {
            var lambda = itPeople.GetPredicateExpression<ContextStringDictionary>();

            var filter = lambda.Compile();
            var localContext = SomeContexts;
            var filteredContexts = localContext.Select(x => x.Context)
                .Where(x => x.ContainsKey("Department")) // Skip entry with a missing key
                .Where(filter);

            Assert.NotNull(filteredContexts);
            Assert.AreEqual(filteredContexts.Count(), 2);
        }

        [Test]
        public void CanSelectDictionaryValuesByKeyCaseInsensitive() {
            var lambda = itPeopleCaseless.GetPredicateExpression<ContextStringDictionary>();

            var filter = lambda.Compile();
            var localContext = SomeContexts;
            var filteredContexts = localContext.Select(x => x.Context)
                .Where(x => x.ContainsKey("Department")) // Skip entry with a missing key
                .Where(filter);

            Assert.NotNull(filteredContexts);
            Assert.AreEqual(filteredContexts.Count(), 3);
        }

        [Test]
        public void CanSelectDictionaryValueWithContainsKeyCheck() {
            var lambda = itPeople.GetPredicateExpression<ContextStringDictionary>();

            var filter = lambda.Compile();
            var localContext = SomeContexts;
            var filteredContexts = localContext.Select(x => x.Context)
                .Where(filter);

            Assert.NotNull(filteredContexts);

            int filteredCount = filteredContexts.Count();
            Assert.AreEqual(filteredCount, 2);
        }

        [Test]
        public void CanSelectMemberOfDictionaryValue() {
            var lambda = fourLetterFolks.GetPredicateExpression<SomeContext>();

            var filter = lambda.Compile();
            var localContext = SomeContexts;
            var filteredContexts = localContext.Where(filter);

            Assert.NotNull(filteredContexts);

            var manualFilter = localContext.Where(x => x.Context["GivenName"]?.Length == 4);
            Assert.AreEqual(2, manualFilter.Count());
            Assert.AreEqual(filteredContexts.Count(), manualFilter.Count());
        }
    }
}
