using System;
using System.Collections.Generic;
using System.Text;

namespace McRule {
    public class ExpressionGeneratorException : Exception {
        public ExpressionGeneratorException() { }
        public ExpressionGeneratorException(string message) : base(message) { }
        public ExpressionGeneratorException(string message, Exception innerException) : base(message, innerException) { }
    }
}
