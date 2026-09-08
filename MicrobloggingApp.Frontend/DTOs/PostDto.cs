namespace MicrobloggingApp.Frontend.DTOs
{
    public class PostDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string ImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
