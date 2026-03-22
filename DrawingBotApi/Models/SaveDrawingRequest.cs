using System.Collections.Generic;

namespace DrawingBotApi.Models
{
    public class SaveDrawingRequest
    {
        public string UserId { get; set; } = "default-user";
        public string Title { get; set; } = "Untitled Drawing";
        public List<DrawingCommand> Commands { get; set; } = new();
    }
}