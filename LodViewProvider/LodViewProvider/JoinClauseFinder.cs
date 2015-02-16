using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LodViewProvider
{

    class JoinClauseFinder : ExpressionVisitor
    {

        static readonly string joinMethodName = "Join";
        readonly List<MethodCallExpression> joinExpressions = new List<MethodCallExpression>();

        internal MethodCallExpression[] GetAllJoins(Expression expression)
        {
            Visit(expression);
            return joinExpressions.ToArray();
        }
        
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.Name == joinMethodName)
            {
                joinExpressions.Add(m);
            }

            Visit(m.Arguments[0]);
            return m;

        }
    }
}
