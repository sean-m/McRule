using System;
using System.Collections.Generic;
using System.Text;

namespace McRule {

    public class FilterPolicy : FilterRuleCollection {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string[] Properties { get; set; }
    }
}
