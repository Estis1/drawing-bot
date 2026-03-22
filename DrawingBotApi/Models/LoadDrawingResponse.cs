using System.Collections.Generic;

namespace DrawingBotApi.Models
{
    public class LoadDrawingResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<DrawingCommand> Commands { get; set; } = new();
    }
}