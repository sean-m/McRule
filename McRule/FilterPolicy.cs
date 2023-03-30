using System;
using System.Collections.Generic;
using System.Text;

namespace McRule {

    public class FilterPolicy : FilterRuleCollection {
        public string name { get; set; }
        public string[] properties { get; set; }
    }
}
