﻿using System;
using OpenStreetMapCache.Lookup;

namespace OpenStreetMapCache.WebServer.Api
{
    public class ReverseModule : BaseWebServerModule
    {
        public ReverseModule() : base("/nominatim/v1")
        {
            Get["/reverse"] = p =>
            {
                string latStr = Request.Query.lat.HasValue ? Request.Query.lat : "";
                string lonStr = Request.Query.lon.HasValue ? Request.Query.lon : "";

                double lat, lon;
                if (!Double.TryParse(latStr, out lat) || !Double.TryParse(lonStr, out lon))
                {
                    var logger = Context.GetLogger();
                    logger.Set("failedParsingLatOrLong", $"{latStr},{lonStr}");
                    return "";
                }

                return lookupProvider.Lookup(lat, lon);
            };
        }
    }
}
