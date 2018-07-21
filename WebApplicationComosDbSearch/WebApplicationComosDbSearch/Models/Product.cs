using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplicationComosDbSearch.Models
{
    public class Product
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable, IsSearchable, IsSortable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnLucene)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsSearchable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnLucene)]
        [JsonProperty("description")]
        public string Description { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("price")]
        public double? Price { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; }

        public Product()
        {
            this.Id = Guid.NewGuid().ToString();
        }
    }
}