using System;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace ServerlessTrivia
{
    public class Clue : TableEntity
    {
        public const string RowKeyValue = "CLUE";
        public Clue()
        {
            PartitionKey = Guid.NewGuid().ToString();
            RowKey = RowKeyValue;
        }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("question")]
        public string Question { get; set; }

        [JsonProperty("answer")]
        public string Answer { get; set; }
        
        [JsonProperty("categoryTitle")]
        public string CategoryTitle { get; set; }
    }
}