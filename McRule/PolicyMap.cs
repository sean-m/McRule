using System;
using System.Collections.Generic;
using System.Text;

namespace McRule {
    public interface PolicyMap {
        string Name { get; }
        string Description { get; }
        Func<FilterPolicy,bool> IsMatch(FilterPolicy policy);
    }
}
