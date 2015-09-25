using BizArk.Core.CmdLine;
using BizArk.Core;

namespace OpenStreetMapCache
{
    [CmdLineOptions(ArgumentPrefix = "-")]
    public class CommandLineArguments : CmdLineObject
    {
        [CmdLineArg(ShowInUsage = DefaultBoolean.True, Alias="s")]
        public bool RunAsService { get; set; }
    }
}

