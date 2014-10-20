using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Collections.ObjectModel;

namespace ExpressionViewer
{

    static class EnumerableExtensions
    {
        // コレクションの各要素に対して、指定された処理をインデックス付きで実行
        public static void ForEach<TItem>(this IEnumerable<TItem> collection, Action<TItem, int> action)
        {
            int index = 0;
            foreach (var item in collection)
                action(item, index++);
        }
    }

    public static class Viewer
    {

        // Expression の中を (再帰的に) 表示
        public static void Show(this Expression expression, int level = 0)
        {
            if (expression as LambdaExpression != null)
            {
                ShowLambdaExpression((LambdaExpression)expression, level);
            }
            else if (expression as BinaryExpression != null)
            {
                ShowBinaryExpression((BinaryExpression)expression, level);
            }
            else if (expression as UnaryExpression != null)
            {
                ShowUnaryExpression((UnaryExpression)expression, level);
            }
            else if (expression as MethodCallExpression != null)
            {
                ShowMethodCallExpression((MethodCallExpression)expression, level);
            }
            else if (expression as ConstantExpression != null)
            {
                ShowCOnstantExpression((ConstantExpression)expression, level);
            }
            else if (expression != null)
            {
                ShowExpressionBase(expression, level);
            }
        }

        private static void ShowCOnstantExpression(ConstantExpression expression, int level)
        {
            ShowText(string.Format("定数値: {0}", expression.Value.ToString()), level + 2);
        }

        private static void ShowMethodCallExpression(MethodCallExpression expression, int level)
        {
            ShowExpressionBase(expression, level);
            ShowText(string.Format("メソッド名: {0}", expression.Method.Name), level + 2);
            // expression.Arguments.ForEach( e => Show( e, level + 2 ) );
            foreach (var arg in expression.Arguments)
            {
                Show(arg, level + 2);
            }
        }

        // Expression のベース部分を表示
        static void ShowExpressionBase(Expression expression, int level)
        {
            ShowText(string.Format("Expression: {0}", expression), level);
            ShowText(string.Format("ノードタイプ: {0}", expression.NodeType), level + 1);
        }

        // LambdaExpression (ラムダ式) の中を (再帰的に) 表示
        static void ShowLambdaExpression(LambdaExpression expression, int level)
        {
            ShowExpressionBase(expression, level);
            ShowText(string.Format("名前: {0}", expression.Name), level + 1);
            ShowText(string.Format("戻り値の型: {0}", expression.ReturnType), level + 1);
            ShowParameterExpressions(expression.Parameters, level + 1); // 引数のコレクション
            ShowText(string.Format("本体: {0}", expression.Body), level + 1);
            expression.Body.Show(level + 2); // 本体を再帰的に表示
        }

        // BinaryExpression (二項演算式) の中を (再帰的に) 表示
        static void ShowBinaryExpression(BinaryExpression expression, int level)
        {
            ShowExpressionBase(expression, level);
            ShowText(string.Format("型: {0}", expression.Type), level + 1);
            ShowText(string.Format("左オペランド: {0}", expression.Left), level + 1);
            expression.Left.Show(level + 2); // 左オペランドを再帰的に表示
            ShowText(string.Format("右オペランド: {0}", expression.Right), level + 1);
            expression.Right.Show(level + 2); // 右オペランドを再帰的に表示
        }

        // UnaryExpression (単項演算式) の中を (再帰的に) 表示
        static void ShowUnaryExpression(UnaryExpression expression, int level)
        {
            ShowExpressionBase(expression, level);
            ShowText(string.Format("型: {0}", expression.Type), level + 1);
            ShowText(string.Format("オペランド: {0}", expression.Operand), level + 1);
            expression.Operand.Show(level + 2); // オペランドを再帰的に表示
        }

        // 引数の式のコレクションを表示
        static void ShowParameterExpressions(ReadOnlyCollection<ParameterExpression> parameterExpressions, int level)
        {
            ShowText("引数群", level);
            if (parameterExpressions == null || parameterExpressions.Count == 0)
                ShowText("引数なし", level);
            else
                parameterExpressions.ForEach((parameterExpression, index) => ShowParameterExpression(parameterExpression, index, level + 1));
        }

        // 引数の式の中を表示
        static void ShowParameterExpression(ParameterExpression parameterExpression, int index, int level)
        {
            ShowText(string.Format("引数{0}", index + 1), level + 1);
            ShowExpressionBase(parameterExpression, level + 1);
            ShowText(string.Format("引数の型: {1}, 引数の名前: {2}", parameterExpression.NodeType, parameterExpression.Type, parameterExpression.Name), level + 2);
        }

        // 文字列をレベルに応じてインデント付で表示
        static void ShowText(string itemText, int level)
        {
            Console.WriteLine("{0}{1}", Indent(level), itemText);
        }

        // インデントの為の文字列を生成
        static string Indent(int level)
        {
            return level == 0 ? "" : new string(' ', (level - 1) * 4 + 1) + "|-- ";
        }
    }
}