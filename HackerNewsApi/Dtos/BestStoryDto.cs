namespace HackerNewsApi.Dtos
{
    public record BestStoryDto
    (
        string Title,
        string Uri,
        string PostedBy,
        DateTime Time,
        int Score,
        int CommentCount
    );
}
