using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace McRule {
    public interface CoreExtenionFunctions {
        Expression<Func<T, bool>> AddStringPropertyExpression<T>(
            Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false);

    }
}
