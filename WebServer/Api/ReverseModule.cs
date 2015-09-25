using System;
using Rangic.Utilities.Geo;

namespace OpenStreetMapCache.WebServer.Api
{
    public class ReverseModule : BaseWebServerModule
    {
        private static readonly IReverseLookupProvider lookupProvider = new PersistentCachingReverseLookupProvider();


        public ReverseModule() : base("/nominatim/v1")
        {
            Get["/reverse"] = p =>
            {
                string latStr = Request.Query.lat.HasValue ? Request.Query.lat : "";
                string lonStr = Request.Query.lon.HasValue ? Request.Query.lon : "";

                double lat, lon;
                if (!Double.TryParse(latStr, out lat) || !Double.TryParse(lonStr, out lon))
                {
                    var logTimer = Context.GetLogger();
                    logTimer.Set("failedParsingLatOrLong", String.Format("{0}-{1}", latStr, lonStr));
                    return "";
                }

                return lookupProvider.Lookup(lat, lon);
            };
        }
    }
}

