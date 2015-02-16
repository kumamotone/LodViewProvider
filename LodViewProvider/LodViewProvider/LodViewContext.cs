using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Net;
using System.Reflection;
using System.Diagnostics;

using LodViewProvider.LinqToTwitter;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LodViewProvider {
	/// <summary>
	/// Context for using LINQ
	/// </summary>
	public class LodViewContext {
        public Dictionary<string, List<IRequestable>> views = new Dictionary<string,List<IRequestable>>();
		public string RawResult { get; private set; }
		public string ViewURI { get; private set; }
		public LodViewContext( string viewUri ) {
			ViewURI = viewUri;
			LodViewExecutor = new LodViewExecute();
		}

		public LodViewContext( string viewUri, LodViewExecute executor ) {
			ViewURI = viewUri;
		}

		internal LodViewExecute LodViewExecutor { get; private set; }

		public LodViewQueryable<Dictionary<String, String>> Dictionary {
			get {
				return new LodViewQueryable<Dictionary<string, string>>( this, ViewURI );
			}
		}

		public LodViewQueryable<Resource> Resource {
			get {
				return new LodViewQueryable<Resource>( this, ViewURI );
			}
		}

		public LodViewQueryable<JToken> JTokens {
			get {
				return new LodViewQueryable<JToken>( this, ViewURI );
			}
		}

		public LodViewQueryable<List<String>> StringList {
			get {
				return new LodViewQueryable<List<string>>( this, ViewURI );
			}
		}

		public virtual object Execute<T>( Expression expression, bool isEnumerable ) {
			var requestProcessor = new RequestProcessor();
			long sum = 0;
			var stopwatch = new Stopwatch();

			//
			// TIME: Analyze ExpressionTree
			//
			stopwatch.Start();
            var joincondition = getJoinConditions(expression);
            requestProcessor.views = this.views;

            var conditions = getRequestParameters( expression, requestProcessor );

            // conditions に入ったIRequestable. を views に分配する
            Dictionary<string, List<IRequestable>> newviews = new Dictionary<string,List<IRequestable>>();

            foreach (var view in views)
            {
                // 初期化 newview[view.Key].Add(single) でエラーが出るため
                newviews.Add(view.Key, new List<IRequestable>());
            }
            foreach (var view in views.ToList())
            {
                foreach (var condition in conditions)
                {
                    // ここから Selection
                    if (condition as MultipleSelection != null)
                    {
                        var multi = condition as MultipleSelection;
                        foreach (SingleSelection single in multi.Variables)
                        {
                            if (single.ViewName == view.Key)
                            {
                                if (newviews[view.Key].Count == 0)
                                {   
                                    newviews[view.Key].Add(new MultipleSelection());
                                }
                                    var a = newviews[view.Key].First() as MultipleSelection;
                                    a.Add(single);
                            }
                        }
                    }
                    // ここから Filter
                    if (condition as Filter != null)
                    {
                        var filt = condition as Filter;
                        if (filt.ViewName == view.Key)
                        {
                            newviews[view.Key].Add(filt);
                        }
                    }

                }

                // views に 無理やりマージ
                views[view.Key] = views[view.Key].Union(newviews[view.Key]).ToList();
                // 無茶クソクソ無理やりダブった MultipleSelection をマージ
                foreach(var values in views.Values)
                {
                    int count = 0;
                    try
                    {
                        foreach (var value in values)
                        {
                            var a = value as MultipleSelection;
                            if (a != null)
                            {
                                count = count + 1;
                            }
                            if (count >= 2)
                            {
                                var b = values.First(c => c as MultipleSelection != null) as MultipleSelection;
                                b.Add(a);
                                values.Remove(value);
                            }
                        }
                    }
                    catch (InvalidOperationException ioe)
                    {

                    }
                }
            }
            
            stopwatch.Stop();

			Console.WriteLine( "ANALYZE: \t{0}", stopwatch.ElapsedMilliseconds.ToString() );
			sum += stopwatch.ElapsedMilliseconds;

            string result = null;
            string resultOuter = null;
            string resultInner = null;
            // JOIN 対象の複数の ViewURL を持てるようにする
            if (joincondition == null)
            {
                Request request = requestProcessor.CreateRequest(ViewURI, conditions);

                //
                // TIME: Request
                //
                stopwatch.Restart();
                result = LodViewExecute.RequestToLod(request, requestProcessor);
                Console.WriteLine("REQUEST: \t{0}", stopwatch.ElapsedMilliseconds.ToString());
                sum += stopwatch.ElapsedMilliseconds;
            }
            else
            {
                Request requestOuter = requestProcessor.CreateRequest(joincondition.OuterViewUrl, views[joincondition.OuterViewName]);
                Request requestInnner = requestProcessor.CreateRequest(joincondition.InnerViewUrl, views[joincondition.InnerViewName]);

                //
                // TIME: Request
                //
                stopwatch.Restart();
                resultOuter = LodViewExecute.RequestToLod(requestOuter, requestProcessor);
                resultInner = LodViewExecute.RequestToLod(requestInnner, requestProcessor);
                Console.WriteLine("REQUEST: \t{0}", stopwatch.ElapsedMilliseconds.ToString());
                sum += stopwatch.ElapsedMilliseconds;
            }
	
            // Results ジョインする

			//
			// TIME: Conversion Results
			//
			stopwatch.Restart();

			// I don't need to make Queryable resources. Because request result is already queried
			// Currently, I don't know how to cancel remaining expression evaluation...

            // var queryableResources = requestProcessor.ProcessResult( result ).AsQueryable();
            IQueryable<Dictionary<string,string>> queryableResources = null;
            if (joincondition == null)
            {
                queryableResources = requestProcessor.ProcessResultAsDictionary(result).AsQueryable();
            }
            else
            {
                var queryableResourcesOuter = requestProcessor.ProcessResultAsDictionary(resultOuter);
                var queryableResourcesInner = requestProcessor.ProcessResultAsDictionary(resultInner);
                /*
                foreach(var o in queryableResourcesOuter)
                {
                    foreach(var k in o)
                    {
                        var a = k.Key;
                        if(a == joincondition.OuterKeyStr)
                        {
                            foreach(var i in queryableResourcesInner)
                            {
                                foreach(var kk in i )
                                {
                                    var b = kk.Key;
                                    if (b == joincondition.InnerKeyStr)
                                    {
                                        
                                    }
                                }
                            }
                        }
                    }
                }*/
                 //queryableResources = queryableResourcesOuter.Union(queryableResourcesInner);
                var qr = from qo in queryableResourcesOuter
                         join qi in queryableResourcesInner
                         on qo[joincondition.OuterKeyStr] equals qi[joincondition.InnerKeyStr]
                         into ALLCOLUMNS
                         from entry in ALLCOLUMNS
                         select entry; 
                var qq = qr.ToList();

                queryableResources = qq.AsQueryable();
            }
            // var queryableResources = requestProcessor.ProcessResultAsStringList( result ).AsQueryable();
			// var queryableResources = requestProcessor.ProcessResultAsJtokens( result ).AsQueryable();
			// var resources = requestProcessor.ProcessResultAsJtokens( result );
			// var queryableResources = resources.AsQueryable();

			// return resources;

			stopwatch.Stop();
			Console.WriteLine( "CONVERT: \t{0}", stopwatch.ElapsedMilliseconds.ToString() );
			sum += stopwatch.ElapsedMilliseconds;
			Console.WriteLine( "TOTAL: \t\t{0}", sum.ToString() );
			Console.WriteLine( "RESULT SIZE: \t{0}", queryableResources.Count().ToString() );

			Console.WriteLine( "\n" + result );
			
			var treeCopier = new ExpressionTreeModifier( queryableResources );
			Expression newExpressionTree = treeCopier.CopyAndModify( expression );

			if ( isEnumerable ) {
				return queryableResources.Provider.CreateQuery( newExpressionTree );
			}

			return queryableResources.Provider.Execute( newExpressionTree );
		}


		private List<IRequestable> getRequestParameters( Expression expression, RequestProcessor requestProcessor ) {
			var conditions = new List<IRequestable>();

			//
			// 'Select' Expression
			//

			var selectExpressions = new SelectClauseFinder().GetAllSelections( expression );
			foreach ( var selectExpression in selectExpressions ) {
				var lambdaExpression = ( LambdaExpression ) ( ( UnaryExpression ) ( selectExpression.Arguments[1] ) ).Operand;
			    lambdaExpression = ( LambdaExpression ) Evaluator.PartialEval( lambdaExpression );

				var selection = requestProcessor.GetParameters( lambdaExpression, TargetMethodType.Projection );
				conditions.Add( selection );
			}

			//
			// 'Where' Expression
			//

			var whereExpressions = new WhereClauseFinder().GetAllWheres( expression );
			foreach ( var whereExpression in whereExpressions ) {
				var lambdaExpression = ( LambdaExpression ) ( ( UnaryExpression ) ( whereExpression.Arguments[1] ) ).Operand;
				lambdaExpression = ( LambdaExpression ) Evaluator.PartialEval( lambdaExpression );

				var filter = requestProcessor.GetParameters( lambdaExpression, TargetMethodType.Selection );
				conditions.Add( filter );
			}

			//
			// Aggregation Expressions
			//

			var aggFunctions = new AggregateFunctionFinder().GetAllTarget( expression );
			foreach( Tuple<MethodCallExpression, AggregationType> aggFunction in aggFunctions ) {
				var lambdaExpression = ( LambdaExpression ) ( ( UnaryExpression) ( aggFunction.Item1.Arguments[1] ) ).Operand;
				lambdaExpression = ( LambdaExpression ) Evaluator.PartialEval( lambdaExpression );

				var aggregation = requestProcessor.GetParameters( lambdaExpression, TargetMethodType.Aggregation, aggFunction.Item2 );
				conditions.Add( aggregation );
			}

			return conditions;
		}

        private JoinCondition getJoinConditions(Expression expression)
        {
            JoinCondition joincondition = null;
            var joinExpressions = new JoinClauseFinder().GetAllJoins(expression);
            if (joinExpressions == null) return null;
            foreach (var m in joinExpressions)
            {
                // arguments を解析してJoinの条件を得る
                // conditions はRewriteに送る条件リストなので，今回は送らずに
                // どこかに保存しておく
                // 返ってきた結果を JOIN してユーザーに返す

                // ConstantExpression outerSource = (ConstantExpression)m.Arguments[0];
                // ConstantExpression innerSource = (ConstantExpression)m.Arguments[1];
                // LambdaExpression outerKey = (LambdaExpression)StripQuotes(m.Arguments[2]);
                ConstantExpression outerSource = (ConstantExpression)m.Arguments[0];
                ConstantExpression innerSource = (ConstantExpression)m.Arguments[1];
                var outerKey = (MethodCallExpression)((LambdaExpression)StripQuotes(m.Arguments[2])).Body;
                var innerKey = (MethodCallExpression)((LambdaExpression)StripQuotes(m.Arguments[3])).Body;

                var outerKeyStr = outerKey.Arguments[0].ToString();
                var outerKeyStr2 = outerKeyStr.Replace("\\","").Replace("\"",""); // たぶんこんなんじゃだめなんだろうけど
                var innerKeyStr = innerKey.Arguments[0].ToString();
                var innerKeyStr2 = innerKeyStr.Replace("\\", "").Replace("\"", ""); // たぶんこんなんじゃだめなんだろうけど
                var outerViewUrl = ((LodViewQueryable<System.Collections.Generic.Dictionary<string, string>>)outerSource.Value).ViewUrl;
                var innerViewUrl = ((LodViewQueryable<System.Collections.Generic.Dictionary<string, string>>)innerSource.Value).ViewUrl;

                var outerviewname = (((LambdaExpression)StripQuotes(m.Arguments[4])).Parameters[0].Name);
                var innerviewname = (((LambdaExpression)StripQuotes(m.Arguments[4])).Parameters[1].Name);
                
                joincondition = new JoinCondition(outerKeyStr2, innerKeyStr2, outerViewUrl, innerViewUrl, outerviewname, innerviewname);

                views.Add(innerviewname, new List<IRequestable>());
                views[innerviewname].Add(new MultipleSelection());
                var a = views[innerviewname].First() as MultipleSelection;
                a.Add((new SingleSelection(innerKeyStr2, "", "", "System.String", innerviewname)));

                views.Add(outerviewname, new List<IRequestable>());
                views[outerviewname].Add(new MultipleSelection());
                var b = views[outerviewname].First() as MultipleSelection;
                b.Add((new SingleSelection(outerKeyStr2, "", "", "System.String", outerviewname)));

                // var a = (MethodCallExpression)(op).Body;
                // var b = a.Arguments[0].ToString();
                // Type resultType = m.Type;
                // ConstantExpression outerSource = (ConstantExpression)m.Arguments[0];
                // ConstantExpression innerSource = (ConstantExpression)m.Arguments[1];
                // LambdaExpression outerKey = (LambdaExpression)StripQuotes(m.Arguments[2]);
                // LambdaExpression innerKey = (LambdaExpression)StripQuotes(m.Arguments[3]);
                // LambdaExpression resultSelector = (LambdaExpression)StripQuotes(m.Arguments[4]);

            }
            return joincondition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
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
