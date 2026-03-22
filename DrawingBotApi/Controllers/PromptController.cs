using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DrawingBotApi.Models;

namespace DrawingBotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PromptController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("parse")]
        public async Task<ActionResult<ParsePromptResponse>> ParsePrompt([FromBody] ParsePromptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest("Prompt is required.");
            }

            var prompt = request.Prompt.Trim().ToLower();
            var fallbackMode = prompt.StartsWith("add") ? "add" : "draw";

            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Ok(new ParsePromptResponse
                    {
                        Mode = fallbackMode,
                        Commands = GetFallbackCommands(prompt)
                    });
                }

                var promptInstructions =
                    "You are a drawing-command generator for an HTML canvas app.\n" +
                    "Convert the user's natural-language drawing request into JSON only.\n\n" +
                    "IMPORTANT:\n" +
                    "- Return JSON only.\n" +
                    "- No markdown.\n" +
                    "- No explanation.\n" +
                    "- No code fences.\n" +
                    "- The JSON MUST include:\n" +
                    "  \"mode\": \"draw\" or \"add\",\n" +
                    "  \"commands\": array of drawing commands.\n" +
                    "- If the user asks to add something to an existing drawing, use \"mode\": \"add\".\n" +
                    "- Otherwise use \"mode\": \"draw\".\n" +
                    "- Use only these shapes: circle, rect, line, triangle, text.\n" +
                    "- Canvas size is 800x500.\n" +
                    "- Keep all coordinates inside canvas.\n" +
                    "- For simple requests like circle, square, rectangle, sun, house, tree, flower, person, car, cloud, return reasonable commands.\n" +
                    "- If unclear, return { \"mode\": \"draw\", \"commands\": [] }.\n\n" +
                    "Examples:\n" +
                    "{ \"mode\": \"draw\", \"commands\": [ { \"type\": \"circle\", \"x\": 400, \"y\": 250, \"radius\": 60, \"color\": \"blue\", \"fill\": false } ] }\n" +
                    "{ \"mode\": \"add\", \"commands\": [ { \"type\": \"rect\", \"x\": 300, \"y\": 200, \"width\": 120, \"height\": 120, \"color\": \"red\", \"fill\": false } ] }\n\n" +
                    "User prompt:\n" + prompt;

                var geminiRequest = new
                {
                    contents = new object[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new
                                {
                                    text = promptInstructions
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        topP = 0.8,
                        topK = 20
                    }
                };

                var client = _httpClientFactory.CreateClient();

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                var content = new StringContent(
                    JsonSerializer.Serialize(geminiRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(url, content);
                var rawResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Gemini request failed. Falling back.");
                    Console.WriteLine(rawResponse);

                    return Ok(new ParsePromptResponse
                    {
                        Mode = fallbackMode,
                        Commands = GetFallbackCommands(prompt)
                    });
                }

                using var doc = JsonDocument.Parse(rawResponse);

                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                    candidates.GetArrayLength() == 0)
                {
                    return Ok(new ParsePromptResponse
                    {
                        Mode = fallbackMode,
                        Commands = GetFallbackCommands(prompt)
                    });
                }

                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return Ok(new ParsePromptResponse
                    {
                        Mode = fallbackMode,
                        Commands = GetFallbackCommands(prompt)
                    });
                }

                text = text.Trim();

                if (text.StartsWith("```json"))
                {
                    text = text.Replace("```json", "").Replace("```", "").Trim();
                }
                else if (text.StartsWith("```"))
                {
                    text = text.Replace("```", "").Trim();
                }

                Console.WriteLine("===== GEMINI RESPONSE TEXT =====");
                Console.WriteLine(text);
                Console.WriteLine("================================");

                try
                {
                    var parsed = JsonSerializer.Deserialize<ParsePromptResponse>(
                        text,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (parsed != null)
                    {
                        parsed.Mode = string.IsNullOrWhiteSpace(parsed.Mode)
                            ? fallbackMode
                            : parsed.Mode.Trim().ToLower();

                        if (parsed.Mode != "draw" && parsed.Mode != "add")
                        {
                            parsed.Mode = fallbackMode;
                        }

                        parsed.Commands ??= new List<DrawingCommand>();

                        return Ok(parsed);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to deserialize Gemini response as ParsePromptResponse:");
                    Console.WriteLine(ex.Message);
                }

                try
                {
                    var commandsOnly = JsonSerializer.Deserialize<List<DrawingCommand>>(
                        text,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (commandsOnly != null)
                    {
                        return Ok(new ParsePromptResponse
                        {
                            Mode = fallbackMode,
                            Commands = commandsOnly
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to deserialize Gemini response as commands list:");
                    Console.WriteLine(ex.Message);
                }

                return Ok(new ParsePromptResponse
                {
                    Mode = fallbackMode,
                    Commands = GetFallbackCommands(prompt)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Prompt parse error:");
                Console.WriteLine(ex.Message);

                return Ok(new ParsePromptResponse
                {
                    Mode = fallbackMode,
                    Commands = GetFallbackCommands(prompt)
                });
            }
        }

        private List<DrawingCommand> GetFallbackCommands(string prompt)
        {
            var p = prompt.ToLower();
            var commands = new List<DrawingCommand>();

            if (p.Contains("circle"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 400,
                    Y = 250,
                    Radius = 60,
                    Color = "blue",
                    Fill = false
                });
            }

            if (p.Contains("square"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "rect",
                    X = 300,
                    Y = 200,
                    Width = 120,
                    Height = 120,
                    Color = "red",
                    Fill = false
                });
            }

            if (p.Contains("rectangle"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "rect",
                    X = 280,
                    Y = 200,
                    Width = 180,
                    Height = 100,
                    Color = "green",
                    Fill = false
                });
            }

            if (p.Contains("sun"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 650,
                    Y = 100,
                    Radius = 50,
                    Color = "yellow",
                    Fill = true
                });
            }

            if (p.Contains("cloud"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 180,
                    Y = 100,
                    Radius = 30,
                    Color = "lightgray",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 220,
                    Y = 90,
                    Radius = 35,
                    Color = "lightgray",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 260,
                    Y = 100,
                    Radius = 30,
                    Color = "lightgray",
                    Fill = true
                });
            }

            if (p.Contains("tree"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "rect",
                    X = 520,
                    Y = 280,
                    Width = 40,
                    Height = 120,
                    Color = "#8B4513",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 540,
                    Y = 250,
                    Radius = 60,
                    Color = "green",
                    Fill = true
                });
            }

            if (p.Contains("house"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "rect",
                    X = 250,
                    Y = 260,
                    Width = 200,
                    Height = 150,
                    Color = "#c97a3d",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "triangle",
                    X1 = 220,
                    Y1 = 260,
                    X2 = 480,
                    Y2 = 260,
                    X3 = 350,
                    Y3 = 180,
                    Color = "#8b3f1f",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "rect",
                    X = 330,
                    Y = 320,
                    Width = 40,
                    Height = 90,
                    Color = "#5c4033",
                    Fill = true
                });
            }

            if (p.Contains("flower"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "line",
                    X1 = 400,
                    Y1 = 280,
                    X2 = 400,
                    Y2 = 380,
                    Color = "green",
                    LineWidth = 4
                });

                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 400,
                    Y = 240,
                    Radius = 20,
                    Color = "yellow",
                    Fill = true
                });

                commands.Add(new DrawingCommand { Type = "circle", X = 370, Y = 240, Radius = 18, Color = "pink", Fill = true });
                commands.Add(new DrawingCommand { Type = "circle", X = 430, Y = 240, Radius = 18, Color = "pink", Fill = true });
                commands.Add(new DrawingCommand { Type = "circle", X = 400, Y = 210, Radius = 18, Color = "pink", Fill = true });
                commands.Add(new DrawingCommand { Type = "circle", X = 400, Y = 270, Radius = 18, Color = "pink", Fill = true });
            }

            if (p.Contains("person"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 200,
                    Y = 120,
                    Radius = 30,
                    Color = "#f1c27d",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "line",
                    X1 = 200,
                    Y1 = 150,
                    X2 = 200,
                    Y2 = 260,
                    Color = "black",
                    LineWidth = 4
                });

                commands.Add(new DrawingCommand
                {
                    Type = "line",
                    X1 = 200,
                    Y1 = 180,
                    X2 = 150,
                    Y2 = 220,
                    Color = "black",
                    LineWidth = 4
                });

                commands.Add(new DrawingCommand
                {
                    Type = "line",
                    X1 = 200,
                    Y1 = 180,
                    X2 = 250,
                    Y2 = 220,
                    Color = "black",
                    LineWidth = 4
                });

                commands.Add(new DrawingCommand
                {
                    Type = "line",
                    X1 = 200,
                    Y1 = 260,
                    X2 = 160,
                    Y2 = 330,
                    Color = "black",
                    LineWidth = 4
                });

                commands.Add(new DrawingCommand
                {
                    Type = "line",
                    X1 = 200,
                    Y1 = 260,
                    X2 = 240,
                    Y2 = 330,
                    Color = "black",
                    LineWidth = 4
                });
            }

            if (p.Contains("car"))
            {
                commands.Add(new DrawingCommand
                {
                    Type = "rect",
                    X = 220,
                    Y = 280,
                    Width = 220,
                    Height = 70,
                    Color = "blue",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "triangle",
                    X1 = 260,
                    Y1 = 280,
                    X2 = 380,
                    Y2 = 280,
                    X3 = 320,
                    Y3 = 220,
                    Color = "blue",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 270,
                    Y = 360,
                    Radius = 25,
                    Color = "black",
                    Fill = true
                });

                commands.Add(new DrawingCommand
                {
                    Type = "circle",
                    X = 390,
                    Y = 360,
                    Radius = 25,
                    Color = "black",
                    Fill = true
                });
            }

            return commands;
        }
    }
}