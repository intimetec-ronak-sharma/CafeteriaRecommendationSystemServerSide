using System;

namespace CafeteriaRecommendationSystem.Models
{
    internal class Sentiment
    {
        public int ItemId { get; set; }
        public float OverallRating { get; set; }
        public string OverallCommentSentiment { get; set; }
        public float SentimentScore { get; set; }
        public int VoteCount { get; set; }
        public string CommentSentiments { get; set; }
    }
}
