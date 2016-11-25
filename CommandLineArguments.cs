using BizArk.Core.CmdLine;
using BizArk.Core;

namespace OpenStreetMapCache
{
    [CmdLineOptions(ArgumentPrefix = "-")]
    public class CommandLineArguments : CmdLineObject
    {
        [CmdLineArg(ShowInUsage = DefaultBoolean.True, Alias="s")]
        public bool RunAsService { get; set; }

		[CmdLineArg(ShowInUsage = DefaultBoolean.True, Alias = "e")]
		public string ElasticSearchHost { get; set; }
    }
}
