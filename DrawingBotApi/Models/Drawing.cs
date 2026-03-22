using System.ComponentModel.DataAnnotations;

namespace DrawingBotApi.Models
{
    public class Drawing
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "default-user";

        public string Title { get; set; } = "Untitled Drawing";

        [Required]
        public string CommandsJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}