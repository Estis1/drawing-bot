namespace DrawingBotApi.Models
{
    public class DrawingCommand
    {
        public string Type { get; set; } = string.Empty;

        public int? X { get; set; }
        public int? Y { get; set; }
        public int? Radius { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        public int? X1 { get; set; }
        public int? Y1 { get; set; }
        public int? X2 { get; set; }
        public int? Y2 { get; set; }
        public int? X3 { get; set; }
        public int? Y3 { get; set; }

        public string Color { get; set; } = "black";
        public bool Fill { get; set; } = false;
        public int LineWidth { get; set; } = 2;

        public string? Text { get; set; }
        public string? Font { get; set; }
    }
}