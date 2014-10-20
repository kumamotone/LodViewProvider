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

        protected override Expression VisitMethodCall(MethodCallExpression m) {
            Type resultType = m.Type;
            Expression outerSource = m.Arguments[0];
            // (( Expression ) outerSource ).Show();
            Expression innerSource = m.Arguments[1];
            // ((Expression)innerSource).Show();
            // LambdaExpression outerKey = (LambdaExpression)StripQuotes(m.Arguments[2]);
            // LambdaExpression innerKey = (LambdaExpression)StripQuotes(m.Arguments[3]);
            // LambdaExpression resultSelector = (LambdaExpression)StripQuotes(m.Arguments[4]);
            Visit(outerSource);
            Visit(innerSource);
            return m;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }
    }
}
