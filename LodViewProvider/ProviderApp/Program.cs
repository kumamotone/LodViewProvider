using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using LodViewProvider;

namespace ProviderApp {
	class Program {
		static void Main( string[] args ) {

			// string viewUrl = "http://lodviewwebapp.herokuapp.com/test/1/";

			const int viewBaseUrlPort = 5000;
            // const string viewBaseHost = "130.158.76.30";
            const string viewBaseHost = "localhost";
            string viewBaseUrl = String.Format( "http://{0}:{1}/", viewBaseHost, viewBaseUrlPort.ToString() );
			    string viewUrlProf = viewBaseUrl + "prof/";
            string viewUrlLab = viewBaseUrl + "lab/";

			Console.WriteLine( viewUrlProf );
            Console.WriteLine( viewUrlLab );
			//
			// Initialize Contxts
			//

			// var context = new LodViewContext( viewUrl ).Resource;
			var profs = new LodViewContext( viewUrlProf ).Dictionary;
            var labs = new LodViewContext( viewUrlLab ).Dictionary;
			// var jcontext = new LodViewContext( viewUrl ).JTokens;
			// var stringlistcont = new LodViewContext( viewUrl ).StringList;

			//
			// For notation in paper
			//

            /*
            var result = from resource in profs
                         where resource["profName"] == "北川 博之"
                         select new { ID = resource["profId"], Name = resource["profName"] };
            */

            var join = from prof in profs
                       join lab in labs
                       on prof["labID"] equals lab["ID"]
                       where prof["Name"] == "Kitagawa"
                       select new
                       {
                           LabName = lab["Name"]
                       };

            
            /*foreach(var j in join.ToList())
            {
                Console.WriteLine("ProfID:{0} ProfName:{1} LabName:{2}", j.ProfID,j.ProfName, j.LabName);
            }*/
            var res = join.ToList();
            
            // var res = result.ToList();
			Console.ReadKey();
		}
	}
}
