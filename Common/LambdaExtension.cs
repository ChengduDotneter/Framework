using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Common
{
    /// <summary>
    /// Lambda表达式相关扩展类
    /// </summary>
    public static class LambdaExtension
    {
        /// <summary>
        /// Lambda表达式拼接
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="merge"></param>
        /// <returns></returns>
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);
            // apply composition of lambda expression bodies to parameters from the first expression
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        /// <summary>
        /// And扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Expression<T> And<T>(this Expression<T> first, Expression<T> second)
        {
            return first.Compose(second, Expression.And);
        }

        /// <summary>
        /// AndAlso扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Expression<T> AndAlso<T>(this Expression<T> first, Expression<T> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        /// <summary>
        /// Or扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Expression<T> Or<T>(this Expression<T> first, Expression<T> second)
        {
            return first.Compose(second, Expression.Or);
        }

        /// <summary>
        /// OrElse扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Expression<T> OrElse<T>(this Expression<T> first, Expression<T> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        /// <summary>
        /// 参数转换扩展
        /// </summary>
        /// <typeparam name="TOrignParameter">原参数类型</typeparam>
        /// <typeparam name="TChangeParameter">转换后的参数类型</typeparam>
        /// <typeparam name="TResult">表达式返回值类型</typeparam>
        /// <param name="expression">原表达式</param>
        /// <param name="changeParameterExpression">转换参数的表达式</param>
        /// <returns>转换参数后的表达式</returns>
        public static Expression<Func<TChangeParameter, TResult>> ChangeParameter<TOrignParameter, TChangeParameter, TResult>
            (this Expression<Func<TOrignParameter, TResult>> expression, Expression<Func<TChangeParameter, TOrignParameter>> changeParameterExpression)
        {
            ParameterExpression changeParameter = Expression.Parameter(typeof(TChangeParameter), "item");

            return Expression.Lambda<Func<TChangeParameter, TResult>>
                (new ParameterChanger<TOrignParameter, TChangeParameter, TResult>().ChangeParameter(expression.Body, changeParameter, changeParameterExpression),
                changeParameter);
        }

        /// <summary>
        /// 多参数转换扩展
        /// </summary>
        /// <typeparam name="TAOrignParameter">原参数类型A</typeparam>
        /// <typeparam name="TBOrignParameter">原参数类型B</typeparam>
        /// <typeparam name="TAChangeParameter">转换后的参数类型A</typeparam>
        /// <typeparam name="TBChangeParameter">转换后的参数类型B</typeparam>
        /// <typeparam name="TResult">表达式返回值类型</typeparam>
        /// <param name="expression">原表达式</param>
        /// <param name="aChangeParameterExpression">转换参数的表达式A</param>
        /// <param name="bChangeParameterExpression">转换参数的表达式B</param>
        /// <param name="aParameterName">参数A名称</param>
        /// <param name="bParameterName">参数B名称</param>
        /// <returns>转换参数后的表达式</returns>
        public static Expression<Func<TAChangeParameter, TBChangeParameter, TResult>> MultiChangeParameter<TAOrignParameter, TBOrignParameter, TAChangeParameter, TBChangeParameter, TResult>
            (this Expression<Func<TAOrignParameter, TBOrignParameter, TResult>> expression,
             Expression<Func<TAChangeParameter, TAOrignParameter>> aChangeParameterExpression,
             Expression<Func<TBChangeParameter, TBOrignParameter>> bChangeParameterExpression,
             string aParameterName,
             string bParameterName)
        {
            ParameterExpression aChangeParameter = Expression.Parameter(typeof(TAChangeParameter), aParameterName);
            ParameterExpression bChangeParameter = Expression.Parameter(typeof(TBChangeParameter), bParameterName);

            return Expression.Lambda<Func<TAChangeParameter, TBChangeParameter, TResult>>
                (new ParameterChanger<TAOrignParameter, TBOrignParameter, TAChangeParameter, TBChangeParameter, TResult>().
                ChangeParameter(expression.Body, aChangeParameter, bChangeParameter, aChangeParameterExpression, bChangeParameterExpression),
                aChangeParameter, bChangeParameter);
        }

        /// <summary>
        /// 多参数转换扩展
        /// </summary>
        /// <typeparam name="TAOrignParameter">原参数类型A</typeparam>
        /// <typeparam name="TBOrignParameter">原参数类型B</typeparam>
        /// <typeparam name="TChangeParameter">转换后的参数类型</typeparam>
        /// <typeparam name="TResult">表达式返回值类型</typeparam>
        /// <param name="expression">原表达式</param>
        /// <param name="aParameterChangeHandler">转换参数A的表达式的委托</param>
        /// <param name="bParameterChangeHandler">转换参数B的表达式的委托</param>
        /// <param name="aParameterName">参数A名称</param>
        /// <param name="bParameterName">参数B名称</param>
        /// <returns>转换参数后的表达式</returns>
        public static Expression<Func<TChangeParameter, TResult>> ExpressionSingleChangeParameter<TAOrignParameter, TBOrignParameter, TChangeParameter, TResult>
            (this Expression<Func<TAOrignParameter, TBOrignParameter, TResult>> expression,
             Func<ParameterExpression, Expression> aParameterChangeHandler,
             Func<ParameterExpression, Expression> bParameterChangeHandler,
             string aParameterName,
             string bParameterName)
        {
            ParameterExpression changeParameter = Expression.Parameter(typeof(TChangeParameter), "item");

            return Expression.Lambda<Func<TChangeParameter, TResult>>
                (new ExpressionParameterChanger<TAOrignParameter, TBOrignParameter, TChangeParameter, TResult>().
                ChangeParameter(expression.Body, changeParameter, aParameterChangeHandler, bParameterChangeHandler, aParameterName, bParameterName),
                changeParameter);
        }

        /// <summary>
        /// 参数重命名扩展
        /// </summary>
        /// <typeparam name="TParameter">参数类型</typeparam>
        /// <typeparam name="TResult">表达式返回值类型</typeparam>
        /// <param name="expression">原表达式</param>
        /// <param name="parameterName">参数名称</param>
        /// <returns>重命名后的表达式</returns>
        public static Expression<Func<TParameter, TResult>> RenameParameter<TParameter, TResult>(this Expression<Func<TParameter, TResult>> expression, string parameterName)
        {
            return (Expression<Func<TParameter, TResult>>)new ParameterRenamer<TParameter, TResult>().RenameParameter(expression, parameterName);
        }

        /// <summary>
        /// 多参数重命名扩展
        /// </summary>
        /// <typeparam name="TAParameter">参数A类型</typeparam>
        /// <typeparam name="TBParameter">参数B类型</typeparam>
        /// <typeparam name="TResult">表达式返回值类型</typeparam>
        /// <param name="expression">原表达式</param>
        /// <param name="aParameterName">参数A名称</param>
        /// <param name="bParameterName">参数B名称</param>
        /// <returns>重命名后的表达式</returns>
        public static Expression<Func<TAParameter, TBParameter, TResult>> RenameParameter<TAParameter, TBParameter, TResult>(this Expression<Func<TAParameter, TBParameter, TResult>> expression, string aParameterName, string bParameterName)
        {
            return (Expression<Func<TAParameter, TBParameter, TResult>>)new ParameterRenamer<TAParameter, TBParameter, TResult>().RenameParameter(
                expression, expression.Parameters[0].Name, expression.Parameters[1].Name, aParameterName, bParameterName);
        }

        /// <summary>
        /// 赋值转换扩展
        /// </summary>
        /// <typeparam name="T">需要转换的表达式参数类型</typeparam>
        /// <param name="expression">需要转换的表达式</param>
        /// <returns>转换后的表达式</returns>
        public static Expression<Func<T, bool>> ReplaceAssign<T>(this Expression<Func<T, bool>> expression)
        {
            return (Expression<Func<T, bool>>)EquelToAssignVisitor.ReplaceBinary(expression);
        }

        /// <summary>
        /// 表达式树形转换为字符串扩展
        /// </summary>
        /// <typeparam name="T">需要转换的表达式参数类型</typeparam>
        /// <param name="expression">需要转换的表达式</param>
        /// <returns>转换后的表达式</returns>
        public static string ToString<T>(this Expression<Func<T, bool>> expression)
        {
            ToStringVisitor toStringVisitor = new ToStringVisitor();
            toStringVisitor.Visit(expression);

            return toStringVisitor.ToString(expression);
        }
    }

    /// <summary>
    /// 表达式转换为字符串转换器
    /// </summary>
    public class ToStringVisitor : ExpressionVisitor
    {
        private readonly StringBuilder m_parametersStringBuilder;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ToStringVisitor()
        {
            m_parametersStringBuilder = new StringBuilder();
        }

        /// <summary>
        /// 将表达式转换为表达式+参数名+常量值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public string ToString<T>(Expression<Func<T, bool>> expression)
        {
            return expression.Body.ToString() + m_parametersStringBuilder.ToString();
        }

        /// <summary>
        /// 访问表达式成员
        /// </summary>
        /// <param name="memberNode"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression memberNode)
        {
            if (memberNode.Expression.NodeType == ExpressionType.Constant)
            {
                m_parametersStringBuilder.Append(memberNode.ToString());
                m_parametersStringBuilder.Append(Expression.Lambda(memberNode).Compile().DynamicInvoke().ToString());
            }

            return base.VisitMember(memberNode);
        }
    }

    /// <summary>
    /// 赋值转换器
    /// </summary>
    public class EquelToAssignVisitor : ExpressionVisitor
    {
        private static BinaryExpression m_binaryExpression;

        /// <summary>
        /// 替换表达式树
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Expression ReplaceBinary<T>(Expression<T> node)
        {
            new EquelToAssignVisitor().Visit(node);

            LabelTarget target = Expression.Label(typeof(bool));
            Expression value = Expression.Constant(true);

            IList<Expression> expressions = new List<Expression>();

            expressions.Add(m_binaryExpression);
            expressions.Add(Expression.Return(target, value));
            expressions.Add(Expression.Label(target, value));

            return Expression.Lambda<T>(Expression.Block(expressions), node.Parameters[0]);
        }

        /// <summary>
        /// 访问表达式树
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            m_binaryExpression = Expression.Assign(node.Left, node.Right);
            return base.VisitBinary(node);
        }
    }

    /// <summary>
    /// 重新绑定参数访问器
    /// </summary>
    internal class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;

        /// <summary>
        /// 重新绑定参数
        /// </summary>
        /// <param name="map"></param>
        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        /// <summary>
        /// 替换参数
        /// </summary>
        /// <param name="map"></param>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        /// <summary>
        /// 访问参数
        /// </summary>
        /// <param name="parameterExpression"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            ParameterExpression replacement;

            if (map.TryGetValue(parameterExpression, out replacement))
                parameterExpression = replacement;

            return base.VisitParameter(parameterExpression);
        }
    }

    /// <summary>
    /// 参数转换访问器
    /// </summary>
    internal class ParameterChanger<TOrignParameter, TChangeParameter, TResult> : ExpressionVisitor
    {
        private Expression<Func<TChangeParameter, TOrignParameter>> m_changeParameterExpression;
        private ParameterExpression m_changeParameter;

        /// <summary>
        /// 访问参数
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Expression.Invoke(m_changeParameterExpression, m_changeParameter);
        }

        /// <summary>
        /// 访问表达式树
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Expression.Lambda<Func<TChangeParameter, TResult>>(Visit(node.Body), m_changeParameter);
        }

        /// <summary>
        /// 变换参数
        /// </summary>
        /// <param name="body"></param>
        /// <param name="changeParameter"></param>
        /// <param name="changeParameterExpression"></param>
        /// <returns></returns>
        internal Expression ChangeParameter(Expression body, ParameterExpression changeParameter, Expression<Func<TChangeParameter, TOrignParameter>> changeParameterExpression)
        {
            m_changeParameterExpression = changeParameterExpression;
            m_changeParameter = changeParameter;
            return Visit(body);
        }
    }

    /// <summary>
    /// 参数转换访问器
    /// </summary>
    internal class ParameterChanger<TAOrignParameter, TBOrignParameter, TAChangeParameter, TBChangeParameter, TResult> : ExpressionVisitor
    {
        private IDictionary<string, ParameterChangeInstance> m_parameterChangeInstances;

        private class ParameterChangeInstance
        {
            public ParameterExpression ParameterExpression { get; }
            public Expression ChangeParameterExpression { get; }

            public ParameterChangeInstance(ParameterExpression prameterExpression, Expression changeParameterExpression)
            {
                ParameterExpression = prameterExpression;
                ChangeParameterExpression = changeParameterExpression;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParameterChanger()
        {
            m_parameterChangeInstances = new Dictionary<string, ParameterChangeInstance>();
        }

        /// <summary>
        /// 访问参数
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Expression.Invoke(m_parameterChangeInstances[node.Name].ChangeParameterExpression, m_parameterChangeInstances[node.Name].ParameterExpression);
        }

        /// <summary>
        /// 访问表达式树
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Expression.Lambda<Func<TAChangeParameter, TBChangeParameter, TResult>>(Visit(node.Body), m_parameterChangeInstances.Values.Select(item => item.ParameterExpression));
        }

        /// <summary>
        /// 变换参数
        /// </summary>
        /// <param name="body"></param>
        /// <param name="aChangeParameter"></param>
        /// <param name="bChangeParameter"></param>
        /// <param name="aChangeParameterExpression"></param>
        /// <param name="bChangeParameterExpression"></param>
        /// <returns></returns>
        internal Expression ChangeParameter(Expression body,
                                            ParameterExpression aChangeParameter,
                                            ParameterExpression bChangeParameter,
                                            Expression<Func<TAChangeParameter, TAOrignParameter>> aChangeParameterExpression,
                                            Expression<Func<TBChangeParameter, TBOrignParameter>> bChangeParameterExpression)
        {
            m_parameterChangeInstances.Add(aChangeParameter.Name, new ParameterChangeInstance(aChangeParameter, aChangeParameterExpression));
            m_parameterChangeInstances.Add(bChangeParameter.Name, new ParameterChangeInstance(bChangeParameter, bChangeParameterExpression));

            return Visit(body);
        }
    }

    /// <summary>
    /// 参数转换访问器
    /// </summary>
    internal class ExpressionParameterChanger<TAOrignParameter, TBOrignParameter, TChangeParameter, TResult> : ExpressionVisitor
    {
        private ParameterExpression m_parameterExpression;
        private IDictionary<string, Func<ParameterExpression, Expression>> m_changeParameterExpressions;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ExpressionParameterChanger()
        {
            m_changeParameterExpressions = new Dictionary<string, Func<ParameterExpression, Expression>>();
        }

        /// <summary>
        /// 访问参数
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return m_changeParameterExpressions[node.Name](m_parameterExpression);
        }

        /// <summary>
        /// 访问表达式树
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Expression.Lambda<Func<TChangeParameter, TResult>>(Visit(node.Body), m_parameterExpression);
        }

        /// <summary>
        /// 变换参数
        /// </summary>
        /// <param name="body"></param>
        /// <param name="changeParameter"></param>
        /// <param name="aParameterChangeHandler"></param>
        /// <param name="bParameterChangeHandler"></param>
        /// <param name="aParameterName"></param>
        /// <param name="bParameterName"></param>
        /// <returns></returns>
        internal Expression ChangeParameter(Expression body,
                                            ParameterExpression changeParameter,
                                            Func<ParameterExpression, Expression> aParameterChangeHandler,
                                            Func<ParameterExpression, Expression> bParameterChangeHandler,
                                            string aParameterName,
                                            string bParameterName)
        {
            m_parameterExpression = changeParameter;
            m_changeParameterExpressions.Add(aParameterName, aParameterChangeHandler);
            m_changeParameterExpressions.Add(bParameterName, bParameterChangeHandler);

            return Visit(body);
        }
    }

    /// <summary>
    /// 参数重命名访问器
    /// </summary>
    internal class ParameterRenamer<TParameter, TResult> : ExpressionVisitor
    {
        private ParameterExpression m_parameterExpression;

        /// <summary>
        /// 访问参数
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return m_parameterExpression;
        }

        /// <summary>
        /// 访问表达式树
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Expression.Lambda<Func<TParameter, TResult>>(Visit(node.Body), m_parameterExpression);
        }

        /// <summary>
        /// 重命名参数
        /// </summary>
        /// <param name="body"></param>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        internal Expression RenameParameter(Expression body,
                                            string parameterName)
        {
            m_parameterExpression = Expression.Parameter(typeof(TParameter), parameterName);
            return Visit(body);
        }
    }

    /// <summary>
    /// 参数重命名访问器
    /// </summary>
    internal class ParameterRenamer<TAParameter, TBParameter, TResult> : ExpressionVisitor
    {
        private IDictionary<string, ParameterExpression> m_parameters;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ParameterRenamer()
        {
            m_parameters = new Dictionary<string, ParameterExpression>();
        }

        /// <summary>
        /// 访问参数
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return m_parameters[node.Name];
        }

        /// <summary>
        /// 访问表达式树
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return Expression.Lambda<Func<TAParameter, TBParameter, TResult>>(Visit(node.Body), m_parameters.Values);
        }

        /// <summary>
        /// 重命名参数
        /// </summary>
        /// <param name="body"></param>
        /// <param name="aOrignParameterName"></param>
        /// <param name="bOrignParameterName"></param>
        /// <param name="aParameterName"></param>
        /// <param name="bParameterName"></param>
        /// <returns></returns>
        internal Expression RenameParameter(Expression body,
                                            string aOrignParameterName,
                                            string bOrignParameterName,
                                            string aParameterName,
                                            string bParameterName)
        {
            m_parameters.Add(aOrignParameterName, Expression.Parameter(typeof(TAParameter), aParameterName));
            m_parameters.Add(bOrignParameterName, Expression.Parameter(typeof(TBParameter), bParameterName));

            return Visit(body);
        }
    }
}