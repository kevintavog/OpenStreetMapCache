using System;
using System.IO;
using System.Data.SQLite;
using NLog;
using Rangic.Utilities.Os;
using System.Collections.Generic;

namespace OpenStreetMapCache.Lookup
{
    public class PersistentCachingReverseLookupProvider : IReverseLookupProvider
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
        static private IReverseLookupProvider innerLookupProvider = new OpenStreetMapLookupProvider();
        private static readonly Object lockObject = new object();


        public string Lookup(double latitude, double longitude)
        {
            var key = String.Format("{0}, {1}", latitude, longitude);
            var result = GetDataForKey(key);
            if (result != null)
                return result.Placename;

            var placename = innerLookupProvider.Lookup(latitude, longitude);
            StoreDataForKey(key, placename);
            return placename;
        }

        public FindNearestResult FindNearest(double latitude, double longitude)
        {
            var key = $"{latitude}, {longitude}";
            var result = GetDataForKey(key);
            if (result == null)
            {
                // Trim digits and find the closest match we can
                key = $"{latitude.ToString("0.00000")}%, {longitude.ToString("0.00000")}%";
                result = GetNearestDataForKey(key);
                if (result == null)
                {
                    key = $"{latitude.ToString("0.0000")}%, {longitude.ToString("0.0000")}%";
                    result = GetNearestDataForKey(key);
                    if (result == null)
                    {
                        key = $"{latitude.ToString("0.000")}%, {longitude.ToString("0.000")}%";
                        result = GetNearestDataForKey(key);
                    }
                }
            }

            if (result == null)
                return null;

            var tokens = result.GeoLocation.Split(',');
            if (tokens.Length != 2)
            {
                logger.Warn($"Unable to parse '{result.GeoLocation}' into tokens");
                return null;
            }
            double matchedLatitude, matchedLongitude;
            if (!Double.TryParse(tokens[0], out matchedLatitude) || !Double.TryParse(tokens[1], out matchedLongitude))
            {
                logger.Warn($"Unable to convert to doubles: {tokens[0]}; {tokens[1]}");
                return null;
            }

            return new FindNearestResult { Latitude = matchedLatitude, Longitude = matchedLongitude, Placename = result.Placename };
        }

        static private StoredPlacename GetDataForKey(string key)
        {
            return GetData("geoLocation = @key", key);
        }

        static private StoredPlacename GetNearestDataForKey(string key)
        {
            return GetData("geoLocation LIKE @key", key);
        }

        static private StoredPlacename GetData(string whereClause, string key)
        {
            try
            {
                using (var connection = new SQLiteConnection(("Data Source=" + DatabasePath)))
                {
                    EnsureExists(connection);

                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = $"SELECT geoLocation,fullPlacename FROM LocationCache WHERE {whereClause}";
                        command.Parameters.AddWithValue("@key", key);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var geoLocation = reader.GetString(0);
                                var placename = reader.GetString(1);

                                if (!placename.Contains("apiStatusCode"))
                                    return new StoredPlacename { GeoLocation = geoLocation, Placename = placename };
                                else
                                    Console.WriteLine($"Ignoring '{placename}'");
                            }
                        }
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                logger.Error("Error looking up key [{0}]: {1}", key, e.ToString());
            }
            return null;
        }

		static public IEnumerable<FindNearestResult> GetAllData()
		{
			using (var connection = new SQLiteConnection(("Data Source=" + DatabasePath)))
			{
				EnsureExists(connection);

				connection.Open();
				using (var command = connection.CreateCommand())
				{
					command.CommandType = System.Data.CommandType.Text;
					command.CommandText = $"SELECT geoLocation,fullPlacename FROM LocationCache";
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var geoLocation = reader.GetString(0);
							var placename = reader.GetString(1);

							var tokens = geoLocation.Split(',');
							if (tokens.Length == 2)
							{
								double matchedLatitude, matchedLongitude;
								if (Double.TryParse(tokens[0], out matchedLatitude) && Double.TryParse(tokens[1], out matchedLongitude))
								{
									yield return new FindNearestResult { Latitude = matchedLatitude, Longitude = matchedLongitude, Placename = placename };
								}
							}
						}
					}
				}
			}
		}

		static private void StoreDataForKey(string key, string data)
        {
            if (String.IsNullOrWhiteSpace(data))
            {
                logger.Warn($"Not storing empty data for '{key}'");
                return;
            }
            if (data.Contains("apiStatusCode"))
            {
                logger.Warn($"Not storing api failure for '{key}': '{data}'");
                return;
            }

            try
            {
                using (var connection = new SQLiteConnection(("Data Source=" + DatabasePath)))
                {
                    connection.BusyTimeout = 5 * 1000;
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "INSERT OR REPLACE INTO LocationCache (geoLocation, fullPlacename) VALUES(@key, @data)";
                        command.Parameters.AddWithValue("@key", key);
                        command.Parameters.AddWithValue("@data", data);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Error storing key [{0}]: {1}", key, e.ToString());
            }
        }

        static private string DatabasePath { get { return Path.Combine(DatabaseFolder, "location.cache"); } }
        static private string DatabaseFolder { get { return Path.Combine(Platform.UserDataFolder("Rangic"), "Location"); } }

        static private void EnsureExists(SQLiteConnection connection)
        {
            try
            {
                if (!File.Exists(DatabasePath))
                {
                    lock(lockObject)
                    {
                        if (!File.Exists(DatabasePath))
                        {
                            if (!Directory.Exists(DatabaseFolder))
                            {
                                logger.Warn("Creating location cache folder: {0}", DatabaseFolder);
                                Directory.CreateDirectory(DatabaseFolder);
                            }

                            logger.Warn("Creating cache schema");
                            connection.Open();
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = "CREATE TABLE IF NOT EXISTS LocationCache (geoLocation TEXT PRIMARY KEY, fullPlacename TEXT)";
                                command.CommandType = System.Data.CommandType.Text;
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Unable to open or create database cache: {0}", e.ToString());
            }
        }
    }

    public class StoredPlacename
    {
        public string GeoLocation { get; set; }
        public string Placename { get; set; }
    }
}
