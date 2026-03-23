# 🎨 Drawing Bot – Fullstack AI Canvas App

A fullstack web application that allows users to generate drawings using natural language.

Users can describe what they want to draw (e.g., "draw a house with a sun"), and the system converts the prompt into structured drawing commands using an LLM (Google Gemini), then renders it on an HTML canvas.

---

## 🚀 Features

* ✏️ Draw using natural language (AI-powered)
* ➕ Add elements to an existing drawing (`add` vs `draw`)
* 🖱️ Freehand drawing with mouse
* ↩️ Undo / Redo support
* 💾 Save drawings to server
* 📂 Load saved drawings
* 🧠 AI integration with fallback logic
* 🎨 Canvas rendering engine

---

## 🛠️ Tech Stack

Frontend:

* React (Vite)
* HTML5 Canvas

Backend:

* .NET Core Web API
* C#

AI:

* Google Gemini API

---

## ⚙️ How to Run

### Backend

cd DrawingBotApi
dotnet run

### Frontend

cd client
npm install
npm run dev

---

## 🔑 API Key

Add to appsettings.json:

{
"Gemini": {
"ApiKey": I have a private ApiKey which I won't share in the public repo
}
}

---

## 👩‍💻 Author

Esther Shenfeld Cohen
