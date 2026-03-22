import { useState, useRef, useEffect } from "react";
import "./App.css";

function App() {
  const [prompt, setPrompt] = useState("");
  const [messages, setMessages] = useState([
    { sender: "bot", text: "Hi! Tell me what to draw." },
  ]);

  const [currentCommands, setCurrentCommands] = useState([]);
  const [currentDrawingId, setCurrentDrawingId] = useState(null);

  const [history, setHistory] = useState([[]]);
  const [historyIndex, setHistoryIndex] = useState(0);

  const canvasRef = useRef(null);
  const isDrawing = useRef(false);
  const currentStrokePoints = useRef([]);

  const cloneCommands = (commands) => JSON.parse(JSON.stringify(commands));

  const pushToHistory = (nextCommands) => {
    const cloned = cloneCommands(nextCommands);

    setHistory((prev) => {
      const trimmed = prev.slice(0, historyIndex + 1);
      return [...trimmed, cloned];
    });

    setHistoryIndex((prev) => prev + 1);
  };

  const drawSingleCommand = (ctx, command) => {
    ctx.beginPath();
    ctx.lineWidth = command.lineWidth || 2;
    ctx.strokeStyle = command.color || "black";
    ctx.fillStyle = command.color || "black";
    ctx.lineCap = "round";
    ctx.lineJoin = "round";

    switch (command.type) {
      case "circle":
        ctx.arc(command.x, command.y, command.radius, 0, Math.PI * 2);
        if (command.fill) {
          ctx.fill();
        } else {
          ctx.stroke();
        }
        break;

      case "rect":
      case "rectangle":
        if (command.fill) {
          ctx.fillRect(command.x, command.y, command.width, command.height);
        } else {
          ctx.strokeRect(command.x, command.y, command.width, command.height);
        }
        break;

      case "line":
        ctx.moveTo(command.x1, command.y1);
        ctx.lineTo(command.x2, command.y2);
        ctx.stroke();
        break;

      case "triangle":
        ctx.moveTo(command.x1, command.y1);
        ctx.lineTo(command.x2, command.y2);
        ctx.lineTo(command.x3, command.y3);
        ctx.closePath();
        if (command.fill) {
          ctx.fill();
        } else {
          ctx.stroke();
        }
        break;

      case "text":
        ctx.font = command.font || "20px Arial";
        ctx.fillStyle = command.color || "black";
        ctx.fillText(command.text, command.x, command.y);
        break;

      case "stroke":
        if (!command.points || command.points.length < 2) break;

        ctx.lineWidth = command.lineWidth || 2;
        ctx.strokeStyle = command.color || "black";
        ctx.lineCap = "round";
        ctx.lineJoin = "round";

        ctx.moveTo(command.points[0].x, command.points[0].y);

        for (let i = 1; i < command.points.length; i++) {
          ctx.lineTo(command.points[i].x, command.points[i].y);
        }

        ctx.stroke();
        break;

      default:
        console.warn("Unknown command type:", command.type);
        break;
    }
  };

  const renderCanvas = (commands) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    commands.forEach((command) => {
      drawSingleCommand(ctx, command);
    });
  };

  const applyCommands = (nextCommands, saveToHistory = true) => {
    setCurrentCommands(nextCommands);

    if (saveToHistory) {
      pushToHistory(nextCommands);
    }
  };

  const testDrawFromCommands = () => {
    const testCommands = [
      {
        type: "circle",
        x: 120,
        y: 100,
        radius: 40,
        color: "yellow",
        fill: true,
        lineWidth: 2,
      },
      {
        type: "line",
        x1: 120,
        y1: 140,
        x2: 120,
        y2: 240,
        color: "brown",
        lineWidth: 4,
      },
      {
        type: "line",
        x1: 120,
        y1: 170,
        x2: 80,
        y2: 210,
        color: "brown",
        lineWidth: 4,
      },
      {
        type: "line",
        x1: 120,
        y1: 170,
        x2: 160,
        y2: 210,
        color: "brown",
        lineWidth: 4,
      },
      {
        type: "text",
        x: 85,
        y: 280,
        text: "stick man",
        color: "black",
        font: "18px Arial",
      },
    ];

    applyCommands(testCommands);

    setMessages((prev) => [
      ...prev,
      { sender: "bot", text: "Test commands drawing created." },
    ]);
  };

  const handleSend = async () => {
    if (!prompt.trim()) return;

    const originalPrompt = prompt;
    const userPrompt = prompt.toLowerCase().trim();

    setMessages((prev) => [...prev, { sender: "user", text: originalPrompt }]);
    setPrompt("");

    try {
      const response = await fetch("http://localhost:5238/api/prompt/parse", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ prompt: userPrompt }),
      });

      if (!response.ok) {
        throw new Error("Server error");
      }

      const data = await response.json();
      const newCommands = data.commands || [];

      if (!newCommands || newCommands.length === 0) {
        setMessages((prev) => [
          ...prev,
          { sender: "bot", text: "I didn't understand what to draw." },
        ]);
        return;
      }

      const mode =
        data.mode || (userPrompt.startsWith("add") ? "add" : "draw");

      if (mode === "add") {
        const mergedCommands = [...currentCommands, ...newCommands];
        applyCommands(mergedCommands);

        setMessages((prev) => [
          ...prev,
          { sender: "bot", text: "Added to the current drawing." },
        ]);
      } else {
        applyCommands(newCommands);

        setMessages((prev) => [
          ...prev,
          { sender: "bot", text: "Drawing created from prompt." },
        ]);
      }
    } catch (error) {
      console.error(error);
      setMessages((prev) => [
        ...prev,
        { sender: "bot", text: "Error communicating with server." },
      ]);
    }
  };

  const handleClear = () => {
    applyCommands([]);

    setCurrentDrawingId(null);

    setMessages((prev) => [
      ...prev,
      { sender: "bot", text: "Canvas cleared." },
    ]);
  };

  const handleNewDrawing = () => {
    applyCommands([]);
    setCurrentDrawingId(null);

    setMessages((prev) => [
      ...prev,
      { sender: "bot", text: "Started a new drawing." },
    ]);
  };

  const handleUndo = () => {
    if (historyIndex <= 0) return;

    const newIndex = historyIndex - 1;
    setHistoryIndex(newIndex);
    setCurrentCommands(cloneCommands(history[newIndex]));
  };

  const handleRedo = () => {
    if (historyIndex >= history.length - 1) return;

    const newIndex = historyIndex + 1;
    setHistoryIndex(newIndex);
    setCurrentCommands(cloneCommands(history[newIndex]));
  };

  const handleSave = async () => {
    if (!currentCommands || currentCommands.length === 0) {
      setMessages((prev) => [
        ...prev,
        { sender: "bot", text: "Nothing to save yet." },
      ]);
      return;
    }

    try {
      const response = await fetch("http://localhost:5238/api/drawings/save", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId: "esti",
          title: "My Drawing",
          commands: currentCommands,
        }),
      });

      if (!response.ok) {
        throw new Error("Failed to save drawing.");
      }

      const data = await response.json();
      setCurrentDrawingId(data.id);

      setMessages((prev) => [
        ...prev,
        { sender: "bot", text: `Drawing saved to server. ID = ${data.id}` },
      ]);
    } catch (err) {
      console.error(err);
      setMessages((prev) => [
        ...prev,
        { sender: "bot", text: "Failed to save drawing to server." },
      ]);
    }
  };

  const handleLoad = async () => {
    const id = window.prompt(
      "Enter drawing ID to load:",
      currentDrawingId ? String(currentDrawingId) : "1"
    );

    if (!id) return;

    try {
      const response = await fetch(`http://localhost:5238/api/drawings/${id}`);

      if (!response.ok) {
        throw new Error("Failed to load drawing.");
      }

      const data = await response.json();

      if (!data.commands || data.commands.length === 0) {
        setMessages((prev) => [
          ...prev,
          { sender: "bot", text: "Drawing found, but it has no commands." },
        ]);
        return;
      }

      applyCommands(data.commands);
      setCurrentDrawingId(data.id);

      setMessages((prev) => [
        ...prev,
        { sender: "bot", text: `Drawing ${data.id} loaded from server.` },
      ]);
    } catch (err) {
      console.error(err);
      setMessages((prev) => [
        ...prev,
        { sender: "bot", text: "Failed to load drawing from server." },
      ]);
    }
  };

  useEffect(() => {
    renderCanvas(currentCommands);
  }, [currentCommands]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const getPoint = (e) => ({
      x: e.offsetX,
      y: e.offsetY,
    });

    const startDrawing = (e) => {
      isDrawing.current = true;
      currentStrokePoints.current = [getPoint(e)];
    };

    const draw = (e) => {
      if (!isDrawing.current) return;

      currentStrokePoints.current.push(getPoint(e));

      const previewCommands = [
        ...currentCommands,
        {
          type: "stroke",
          points: currentStrokePoints.current,
          color: "black",
          lineWidth: 2,
        },
      ];

      renderCanvas(previewCommands);
    };

    const stopDrawing = () => {
      if (!isDrawing.current) return;

      isDrawing.current = false;

      if (currentStrokePoints.current.length >= 2) {
        const strokeCommand = {
          type: "stroke",
          points: [...currentStrokePoints.current],
          color: "black",
          lineWidth: 2,
        };

        applyCommands([...currentCommands, strokeCommand]);
      } else {
        renderCanvas(currentCommands);
      }

      currentStrokePoints.current = [];
    };

    canvas.addEventListener("mousedown", startDrawing);
    canvas.addEventListener("mousemove", draw);
    canvas.addEventListener("mouseup", stopDrawing);
    canvas.addEventListener("mouseleave", stopDrawing);

    return () => {
      canvas.removeEventListener("mousedown", startDrawing);
      canvas.removeEventListener("mousemove", draw);
      canvas.removeEventListener("mouseup", stopDrawing);
      canvas.removeEventListener("mouseleave", stopDrawing);
    };
  }, [currentCommands]);

  return (
    <div className="app">
      <header className="topbar">
        <select className="drawing-select">
          <option>
            {currentDrawingId ? `Drawing #${currentDrawingId}` : "Drawing #1"}
          </option>
        </select>

        <div className="toolbar">
          <button onClick={handleNewDrawing}>+ New Drawing</button>
          <button onClick={handleUndo}>Undo</button>
          <button onClick={handleRedo}>Redo</button>
          <button onClick={handleClear}>Clear</button>
          <button onClick={handleSave}>Save</button>
          <button onClick={handleLoad}>Load</button>
          <button onClick={testDrawFromCommands}>Test Commands</button>
        </div>
      </header>

      <main className="main-layout">
        <section className="chat-panel">
          <h2>Your chat with the bot</h2>

          <div className="messages">
            {messages.map((msg, index) => (
              <div
                key={index}
                className={`message ${msg.sender === "user" ? "user" : "bot"}`}
              >
                {msg.text}
              </div>
            ))}
          </div>

          <div className="input-row">
            <input
              type="text"
              placeholder="Write a message"
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleSend()}
            />
            <button onClick={handleSend}>Send</button>
          </div>
        </section>

        <section className="canvas-panel">
          <canvas
            ref={canvasRef}
            id="drawing-canvas"
            width={800}
            height={500}
            style={{
              border: "2px solid #ccc",
              borderRadius: "12px",
              background: "white",
              cursor: "crosshair",
              width: "100%",
              maxWidth: "800px",
              display: "block",
            }}
          ></canvas>
        </section>
      </main>
    </div>
  );
}

export default App;