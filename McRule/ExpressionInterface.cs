using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {
    public interface IExpressionRule {
        string TargetType { get; set; }
        Expression<Func<T, bool>>? GetExpression<T>();
        Expression<Func<T, bool>>? GetExpression<T>(ExpressionOptions options);
    }

    public interface ExpressionOptions {
        public bool SupportEF { get; }
    }
}
