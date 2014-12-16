using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LodViewProvider
{
    class JoinCondition
    {
        public string outerKeyStr;
        public string innerKeyStr;
        public string outerViewUrl;
        public string innerViewUrl;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="outerKeyStr"></param>
        /// <param name="innerKeyStr"></param>
        /// <param name="outerViewUrl"></param>
        /// <param name="innerViewUrl"></param>
        public JoinCondition(string outerKeyStr, string innerKeyStr, string outerViewUrl, string innerViewUrl)
        {
            this.outerKeyStr = outerKeyStr;
            this.innerKeyStr = innerKeyStr;
            this.outerViewUrl = outerViewUrl;
            this.innerViewUrl = innerViewUrl;
        }
    }
}
