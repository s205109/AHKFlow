namespace AHKFlow.Domain.Entities
{
    public class TestMessage
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? Category { get; set; }
    }
}
