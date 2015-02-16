using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LodViewProvider
{
    class JoinCondition
    {
        private readonly string _outerKeyStr;
        private readonly string _innerKeyStr;
        private readonly string _outerViewUrl;
        private readonly string _innerViewUrl;
        private readonly string _outerViewName;
        private readonly string _innerViewName;

        public string OuterKeyStr { get { return _outerKeyStr; } }
        public string InnerKeyStr { get { return _innerKeyStr;  } }
        public string OuterViewUrl { get { return _outerViewUrl; } }
        public string InnerViewUrl { get { return _innerViewUrl; } }
        public string OuterViewName { get { return _outerViewName; } }
        public string InnerViewName { get { return _innerViewName; } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="outerKeyStr"></param>
        /// <param name="innerKeyStr"></param>
        /// <param name="outerViewUrl"></param>
        /// <param name="innerViewUrl"></param>
        public JoinCondition(string outerKeyStr, string innerKeyStr, string outerViewUrl, string innerViewUrl, string outerViewName, string innerViewName)
        {
            this._outerKeyStr = outerKeyStr;
            this._innerKeyStr = innerKeyStr;
            this._outerViewUrl = outerViewUrl;
            this._innerViewUrl = innerViewUrl;
            this._outerViewName = outerViewName;
            this._innerViewName = innerViewName;
        }
    }
}
