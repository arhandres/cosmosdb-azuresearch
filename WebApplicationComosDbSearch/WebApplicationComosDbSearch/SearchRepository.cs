
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using WebApplicationComosDbSearch.Models;
using WebApplicationComosDbSearch.Util;

namespace WebApplicationComosDbSearch
{
    public static class SearchRepository<T> where T : class
    {
        private static SearchServiceClient _serviceClient = null;
        private static ISearchIndexClient _indexClient = null;

        public static DocumentSearchResult<T> SearchPhrase(string phrase, bool lucene = false, params string[] columns)
        {
            Debug.WriteLine("Phrase: {0}", phrase);
            Debug.WriteLine("Lucene Syntax: {0}", lucene ? "Yes" : "No");
            Debug.WriteLine("Columns: {0}", string.Join(",", columns));

            var data = _indexClient.Documents.Search<T>(phrase, new SearchParameters()
            {
                SearchFields = new List<string>(columns),
                SearchMode = SearchMode.Any,
                IncludeTotalResultCount = true,
                QueryType = lucene ? QueryType.Full : QueryType.Simple, //For Lucene Syntax,
                Top = 1000
            });

            return data;
        }

        static ISearchIndexClient CreateIndexAndGetClient()
        {
            var searchServiceName = Config.GetValue<string>("SearchServiceName");
            var apiKey = Config.GetValue<string>("SearchKey");
            var index = GetIndexDefinition();

            _serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));

            if (!_serviceClient.Indexes.Exists(index.Name))
                _serviceClient.Indexes.Create(index);

            var indexClient = _serviceClient.Indexes.GetClient(index.Name);

            return indexClient;
        }

        static Index GetIndexDefinition()
        {
            var definition = new Index()
            {
                Name = Config.GetValue<string>("SearchIndexName"),
                Fields = FieldBuilder.BuildForType<Product>()
            };

            return definition;
        }

        public static void Intialize()
        {
            _indexClient = CreateIndexAndGetClient();
        }

        public static void IntializeIndexer(bool update = false)
        {
            if (_serviceClient == null)
                throw new ArgumentNullException("Must call Intialize before InitializeIndexer");
            
            CreateIndexer(update);
        }

        static Indexer CreateIndexer(bool update = false)
        {
            var indexerName = $"{typeof(T).Name}-indexer".ToLower();

            var exists = _serviceClient.Indexers.Exists(indexerName);

            if (!exists || update)
            {
                var indexName = Config.GetValue<string>("SearchIndexName");

                var dataSource = CreateDataSource(update);

                _serviceClient.Indexers.CreateOrUpdate(new Indexer()
                {
                    Name = indexerName,
                    DataSourceName = dataSource.Name,
                    IsDisabled = false,
                    Schedule = new IndexingSchedule()
                    {
                        Interval = TimeSpan.FromMinutes(5),
                        StartTime = DateTimeOffset.Now
                    },
                    TargetIndexName = indexName,
                });
            }

            var indexer = _serviceClient.Indexers.Get(indexerName);
            return indexer;
        }

        static List<FieldMapping> GetFieldMapping()
        {
            var indexDefinition = GetIndexDefinition();

            var mapping = indexDefinition.Fields.Select(f => new FieldMapping(f.Name, f.Name)).ToList();
            return mapping;
        }

        static DataSource CreateDataSource(bool update = false)
        {
            var host = Config.GetValue<string>("CosmosEndpoint");
            var key = Config.GetValue<string>("CosmosKey");
            var database = Config.GetValue<string>("CosmosDatabase");
            var collection = Config.GetValue<string>("CosmosCollection");

            var dataSourceName = $"{typeof(T).Name}-source".ToLower();
            var exists = _serviceClient.DataSources.Exists(dataSourceName);

            if (!exists || update)
            {
                var cosmosdbConnectionString = $"AccountEndpoint={host};AccountKey={key};Database={database}";

                var query = "SELECT * FROM c WHERE c._ts >= @HighWaterMark ORDER BY c._ts";

                _serviceClient.DataSources.CreateOrUpdate(new DataSource()
                {
                    Name = dataSourceName,
                    Type = DataSourceType.DocumentDb,
                    Container = new DataContainer(collection, query),
                    Credentials = new DataSourceCredentials()
                    {
                        ConnectionString = cosmosdbConnectionString
                    }
                });
            }

            var dataSource = _serviceClient.DataSources.Get(dataSourceName);

            return dataSource;
        }
    }
}