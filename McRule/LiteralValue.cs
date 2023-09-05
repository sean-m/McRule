using System;
using System.Collections.Generic;
using System.Text;

namespace McRule {
    public class LiteralValue {
        public dynamic Value { get; set; }
        public override string ToString() => "BaseLiteral";
    }

    public class NullValue : LiteralValue {
        
        public override string ToString() => "null";
    }
}
