using System;
using System.Net;
using DalSoft.RestClient;
using NLog;

namespace OpenStreetMapCache.Lookup
{
	public class PersistentLookupProvider : IReverseLookupProvider
	{
		static public string ElasticSearchHost = "";
		
		const string IndexName = "openstreetmap";

		static private readonly Logger logger = LogManager.GetCurrentClassLogger();
		static private IReverseLookupProvider innerLookupProvider = new OpenStreetMapLookupProvider();

		private static dynamic _singleClient;
		private static dynamic ClientInstance
		{
			get
			{
				if (_singleClient == null)
				{
					_singleClient = CreateClient();
				}
				return _singleClient;
			}
		}


		public string Lookup(double latitude, double longitude)
		{
			var nearest = NearbyLocation(latitude, longitude);
			if (nearest != null && nearest.sort[0] < 3)
			{
				return nearest._source.Placename;
			}

			var placename = innerLookupProvider.Lookup(latitude, longitude);
			StoreLocation(new LocationPlacename { location = new ElasticGeoPoint { lat = latitude, lon = longitude }, Placename = placename });
			return placename;
		}

		public FindNearestResult FindNearest(double latitude, double longitude)
		{
			var nearest = NearbyLocation(latitude, longitude);
			if (nearest != null && nearest.sort[0] < 15)
			{
				return new FindNearestResult { Latitude = nearest._source.location.lat, Longitude = nearest._source.location.lon, Placename = nearest._source.Placename };
			}

			return null;
		}

		public void StoreLocation(LocationPlacename location)
		{
			CheckResponseForErrors(ClientInstance.openstreetmap.placenames(location.Id).Put(location.GetBody()).Result);
		}

		private dynamic NearbyLocation(double latitude, double longitude)
		{
			var body = @"{
				    ""sort"" : [ {
				        ""_geo_distance"" : {
				            ""location"" : {
				                ""lat"" : " + latitude + @",
				                ""lon"" : " + longitude + @"
				            }, 
				            ""order"" : ""asc"",
				            ""unit"" : ""m""
				        }
				    }],
				    ""query"":{
				        ""bool"" : {
				            ""must"" : {
				                ""match_all"" : {}
				            },
				            ""filter"" : {
				                ""geo_distance"" : {
				                    ""distance"" : ""2km"",
				                    ""location"" : {
				                        ""lat"" : " + latitude + @",
				                        ""lon"" : " + longitude + @"
				                    }
				                }
				            }
				        }
				    }
				}";

			var response = CheckResponseForErrors(ClientInstance.openstreetmap._search.Query(new { source = body }).Get().Result);
			if (response.hits != null && response.hits.total > 0)
			{
				return response.hits.hits[0];
			}

			return null;
		}

		static public void Initialize()
		{
			CheckIndex(ClientInstance);
		}

		static private RestClient CreateClient()
		{
			if (String.IsNullOrWhiteSpace(ElasticSearchHost))
				throw new Exception("Set the ElasticSearch host");

			var client = new RestClient(ElasticSearchHost);
			return client;
		}

		static private void CheckIndex(dynamic client)
		{
			// Throw an exception if we can't connect to the server - no value in continuing
			dynamic response = client.openstreetmap.Get().Result;

			// And check whether the index exists
			ElasticResponse elasticResponse = response;
			if (!elasticResponse.status.HasValue || elasticResponse.status == (int)HttpStatusCode.OK)
			{
				return;
			}

			logger.Info("Creating the index");
			response = client.openstreetmap.Put(new
			{
				settings = new
				{
					number_of_shards = 1,
					number_of_replicas = 0
				},
				mappings = new
				{
					placenames = new
					{
						properties = new
						{
							location = new
							{
								type = "geo_point"
							}
						}
					}
				}
			}).Result;

			elasticResponse = CheckResponseForErrors(response);
			if (elasticResponse.acknowledged != true)
			{
				throw new Exception("CreateIndex failed, acknowledged not set");
			}
		}

		static private ElasticResponse CheckResponseForErrors(dynamic response)
		{
			ElasticResponse elasticResponse = response;
			if (elasticResponse.status.HasValue && elasticResponse.status != (int)HttpStatusCode.OK)
			{
				throw new Exception($"Request failed: ${elasticResponse.error.type}, {elasticResponse.error.reason}");
			}
			return elasticResponse;
		}
	}

	public class LocationPlacename
	{
		public string Id { get { return $"{location.lat}, {location.lon}"; } }
		public ElasticGeoPoint location;
		public string Placename;

		public dynamic GetBody()
		{
			return new
			{
				location = new
				{
					lat = location.lat,
					lon = location.lon,
				},
				placename = Placename
			};
		}

		public override string ToString()
		{
			return $"[Location: {location.lat}, {location.lon}, {Placename}";
		}
	}

	public class ElasticGeoPoint
	{
		public double lat;
		public double lon;
	}

	public class ElasticResponse
	{
		public int? status;
		public ElasticError error;
		public bool? acknowledged;
		public ElasticHits hits;

		public override string ToString()
		{
			return $"[ElasticResponse {status}, {error}, {hits}]";
		}
	}

	public class ElasticError
	{
		public string type;
		public string reason;
		public override string ToString()
		{
			return $"[ElasticError {type} -- {reason}]";
		}
	}

	public class ElasticHits
	{
		public int total;
		public ElasticHitItem[] hits;
		public override string ToString()
		{
			var result = $"[ElasticHits {total}";
			if (total > 0)
			{
				result += $", {hits[0]}";
			}
			result += "]";
			return result;
		}
	}

	public class ElasticHitItem
	{
		public LocationPlacename _source;
		public double[] sort;
		public override string ToString()
		{
			return $"[ElasticHitItem {sort}, {_source}]";
		}
	}
}
