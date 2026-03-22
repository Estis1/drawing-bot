using System.Collections.Generic;

namespace DrawingBotApi.Models
{
    public class ParsePromptResponse
    {
        public string Mode { get; set; } = "draw";
        public List<DrawingCommand> Commands { get; set; } = new();
    }
}