using Nancy;
using NLog;
using System.Collections.Generic;
using System;
using OpenStreetMapCache.Lookup;

namespace OpenStreetMapCache.WebServer
{
    abstract public class BaseWebServerModule : NancyModule
    {
        protected static readonly PersistentCachingReverseLookupProvider lookupProvider = new PersistentCachingReverseLookupProvider();
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected BaseWebServerModule(string modulePath) : base(modulePath)
        {
            Before += ctx =>
            {
                ctx.RegisterLogger(logger);

                var contextLogger = ctx.GetLogger();
                contextLogger.Set("path", ctx.Request.Path);

                var queryParams = new List<string>();
                foreach (var key in ctx.Request.Query.Keys)
                    queryParams.Add(String.Format("{0}={1}", key, ctx.Request.Query[key]));
                contextLogger.Set("query", String.Join("&", queryParams));

                return null;
            };
        }
    }
}
