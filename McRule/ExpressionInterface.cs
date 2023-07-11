using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {
    public interface IExpressionRule {
        string TargetType { get; set; }
        Expression<Func<T, bool>>? GetExpression<T>();
    }
}
