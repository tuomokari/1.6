using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using System.Threading;
using MarkdownSharp;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.Builtin
{
    public class text : MC2Controller
    {
        private string minutesStr = null;
        private string hoursStr = null;
        private object timeStrLockObject = new object();

        internal ThreadLocal<Markdown> markdownProvider = new ThreadLocal<Markdown>();

        private static Dictionary<string, string> markdownCache = new Dictionary<string, string>();

        public Markdown MarkdownProvider
        {
            get
            {
                if (markdownProvider.Value == null)
                    markdownProvider.Value = new Markdown();

                return markdownProvider.Value;
            }
        }

        #region Blocks

        public MC2Value markdown(string content)
        {
            lock (markdownCache)
            {
                string result;
                if (markdownCache.TryGetValue(content, out result))
                {
                    return result;
                }
            }

            Markdown markdownProvider = MarkdownProvider;

            string unencodedContent = HttpUtility.HtmlDecode(content);
            string markdownContent = markdownProvider.Transform(unencodedContent);

            lock (markdownCache)
            {
                const int MaxMarkdownCacheEntries = 1000;

                // Very simple cache invalidation rule. We need some cleanup because 
                // cache without invalidation is little different from a memory leak.
                if (markdownCache.Count > MaxMarkdownCacheEntries)
                    markdownCache.Clear();

                markdownCache[content] = markdownContent;
            }

            return markdownContent;
        }

        public MC2Value formattimespan(int timespan = 0)
        {

            lock (timeStrLockObject)
            {
                if (minutesStr == null)
                {
                    minutesStr = Runtime.GetTranslation("unit_minutes", "core");
                    hoursStr = Runtime.GetTranslation("unit_hours", "core");
                }
            }

            int hours = timespan / (1000 * 60 * 60);
            timespan -= hours * 1000 * 60 * 60;
            int minutes = timespan / (1000 * 60);

            string ret = string.Empty;

            if (hours > 0)
                ret = string.Format("{0} {1}", hours, hoursStr);

            if (minutes > 0)
            {
                if (!string.IsNullOrEmpty(ret))
                    ret += " ";

                ret += string.Format("{0} {1}", minutes, minutesStr);
            }

            return ret;
        }

		public MC2Value formatdate(DateTime dateTime)
		{
			return dateTime.ToString("d");
		}

		#endregion
	}
}