using System;
using System.Text.RegularExpressions;
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
        private string answer;
        public string Answer
        {
            get { return answer;}
            set { answer = CleanUpAnswer(value); }
        }
        
        [JsonProperty("categoryTitle")]
        public string CategoryTitle { get; set; }

        private string CleanUpAnswer(string answer)
        {
            answer = answer ?? "";
            return answer.Replace("<i>", "").Replace("</i>", "").Replace(@"\", "");
        }

        public int CalculateQuality()
        {
            var score = 0; // the higher the better

            var nonLettersCount = Regex.Matches(Answer, "[^a-zA-Z ]").Count;
            score -= nonLettersCount;

            var isAllDigits = Regex.IsMatch(Answer, @"^\d+$");
            if (isAllDigits)
            {
                score -= 10;
            }

            if(string.IsNullOrWhiteSpace(Question) || string.IsNullOrWhiteSpace(Answer))
            {
                score -= 100;
            }
            else if (Regex.IsMatch(Question, @"\bseen here\b", RegexOptions.IgnoreCase))
            {
                // "seen here" usually indicates a visual clue
                score -= 50;
            }

            if (Answer.Length > 16)
            {
                score -= 10; // deduct points if too long
            }

            return score;
        }
    }
}