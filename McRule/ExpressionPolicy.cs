using System;
using System.Collections.Generic;
using System.Text;

namespace McRule {

    public class ExpressionPolicy : ExpressionRuleCollection {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string[] Properties { get; set; }

        public ExpressionPolicy() : base() { }
    }
}
