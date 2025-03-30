# Legal and Accounting Chatbot

A bilingual (English/Macedonian) chatbot that provides information about legal and accounting matters in North Macedonia.

## Project Structure

- `LegalChatbot.API/` - .NET backend service
- `legal-chatbot-frontend/` - React frontend application

## Features

- Bilingual support (English/Macedonian)
- Legal document search and retrieval
- Context-aware responses using Groq's llama-3.3-70b-versatile model
- Conversation history management
- Source document citations

## Prerequisites

- .NET 9.0 SDK
- Node.js 18+
- Groq API key

## Setup

1. Clone the repository:
```bash
git clone [repository-url]
cd LegalAccountingChatBot
```

2. Backend Setup:
```bash
cd LegalChatbot.API
dotnet restore
```

3. Frontend Setup:
```bash
cd legal-chatbot-frontend
npm install
```

4. Configure your Groq API key in `appsettings.Development.json`

## Running the Application

1. Start the backend:
```bash
cd LegalChatbot.API
dotnet run
```

2. Start the frontend:
```bash
cd legal-chatbot-frontend
npm start
```

The application will be available at:
- Frontend: http://localhost:3000
- Backend API: https://localhost:7263 or http://localhost:5135

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request