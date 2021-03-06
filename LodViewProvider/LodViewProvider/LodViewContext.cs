﻿using System;
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
			var conditions = getRequestParameters( expression, requestProcessor );
			stopwatch.Stop();
			Console.WriteLine( "ANALYZE: \t{0}", stopwatch.ElapsedMilliseconds.ToString() );
			sum += stopwatch.ElapsedMilliseconds;

			Request request = requestProcessor.CreateRequest( ViewURI, conditions );

			//
			// TIME: Request
			//
			stopwatch.Restart();
			string result = LodViewExecute.RequestToLod( request, requestProcessor );
			Console.WriteLine( "REQUEST: \t{0}", stopwatch.ElapsedMilliseconds.ToString() );
			sum += stopwatch.ElapsedMilliseconds;

			//
			// TIME: Conversion Results
			//
			stopwatch.Restart();

			// I don't need to make Queryable resources. Because request result is already queried
			// Currently, I don't know how to cancel remaining expression evaluation...

			// var queryableResources = requestProcessor.ProcessResult( result ).AsQueryable();
			var queryableResources = requestProcessor.ProcessResultAsDictionary( result ).AsQueryable();
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

			Console.ReadKey();
			Console.WriteLine( "\n" + result );
			Console.ReadKey();

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
	}
}
