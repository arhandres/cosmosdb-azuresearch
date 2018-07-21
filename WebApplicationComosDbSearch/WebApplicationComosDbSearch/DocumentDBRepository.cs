using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebApplicationComosDbSearch
{
    public static class DocumentDBRepository<T> where T : class
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["CosmosDatabase"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["CosmosCollection"];
        private static DocumentClient client;

        public static async Task<T> GetItemAsync(string id, string category)
        {
            try
            {
                Document document =
                    await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<ContinuationResult<T>> GetItemsWithContinuationAsync(Expression<Func<T, bool>> predicate, int pageSize = 10, string continuation = null)
        {
            var result = new ContinuationResult<T>();
            var items = new List<T>();

            var token = !string.IsNullOrEmpty(continuation) ? Encoding.UTF8.GetString(Convert.FromBase64String(continuation)) : null;

            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = pageSize, EnableCrossPartitionQuery = true, RequestContinuation = token })
                .Where(predicate)
                .AsDocumentQuery();

            if (query.HasMoreResults)
            {
                var data = await query.ExecuteNextAsync<T>();
                items.AddRange(data);

                result.Items = items;
                result.ContinuationToken = data.ResponseContinuation;
                result.PageSize = pageSize;
            }

            return result;
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate, List<Expression<Func<T, bool>>> filters = null)
        {
            var results = new List<T>();

            var queryable = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                .Where(predicate);

            filters?.ForEach(f => queryable = queryable.Where(f));

            IDocumentQuery<T> documentQuery = queryable.AsDocumentQuery();

            while (documentQuery.HasMoreResults)
            {
                results.AddRange(await documentQuery.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
        }

        public static async Task DeleteItemAsync(string id, string category)
        {
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
        }

        public static void Initialize()
        {
            client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["CosmosEndpoint"]), ConfigurationManager.AppSettings["CosmosKey"]);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();

            
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection
                        {
                            Id = CollectionId
                        },
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public class ContinuationResult<T>
    {
        private string _continuationToken;

        public IEnumerable<T> Items { get; set; }

        public int PageSize { get; set; }

        public string ContinuationToken
        {
            get
            {
                return _continuationToken;
            }

            set
            {
                _continuationToken = !string.IsNullOrEmpty(value) ? Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) : null;
            }
        }
    }
}