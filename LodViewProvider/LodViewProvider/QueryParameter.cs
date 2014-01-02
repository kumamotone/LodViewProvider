﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Web;

using Newtonsoft.Json;

namespace LodViewProvider {

	public class QueryParameter {

		public List<ICondition> Conditions { get; private set; }

		public QueryParameter()
			: this( new List<ICondition>() ) { }

		public QueryParameter( List<ICondition> conditions ) {
			Conditions = conditions;
		}

		public string CreateQueryString () {
			string serializedFilters = JsonConvert.SerializeObject( Conditions );
			string encodedQuery = HttpUtility.UrlEncode( serializedFilters );
			// TODO: Consider empty query string
			return String.Format( "query={0}", encodedQuery );
		}
	}
}
