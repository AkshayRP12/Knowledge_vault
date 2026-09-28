# Knowledge Vault

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-5.x-646CFF?logo=vite&logoColor=white)](https://vitejs.dev/)
[![SQL Server](https://img.shields.io/badge/Microsoft%20SQL%20Server-2022-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/en-us/sql-server)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**Knowledge Vault** is a full-stack enterprise knowledge management and collaborative documentation platform designed for teams to create, review, categorize, and discover internal knowledge. Built with a **React** single-page frontend and a high-performance **ASP.NET Core 8 Web API** backend utilizing pure **ADO.NET** parameterized T-SQL queries with the Active Record and Data Transfer Object (DTO) patterns.

---

## Table of Contents

- [Architectural Overview](#architectural-overview)
- [Tech Stack](#tech-stack)
- [Key Features](#key-features)
- [Database Schema (3NF)](#database-schema-3nf)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Database Setup](#1-database-setup)
  - [Backend Setup](#2-backend-setup)
  - [Frontend Setup](#3-frontend-setup)
- [Default Credentials](#default-credentials)
- [API Endpoints](#api-endpoints)
- [Role-Based Access Control](#role-based-access-control)
- [Security Features](#security-features)
- [Project Structure](#project-structure)
- [Troubleshooting & FAQ](#troubleshooting--faq)

---

## Architectural Overview

Knowledge Vault employs a multi-tiered, decoupled architecture engineered for raw performance, auditability, and clear separation of concerns:

```
┌───────────────────────────────────────────────────────────┐
│                React Frontend (Vite + SPA)                │
│    • WYSIWYG Rich Text Editor (Bold / Italic / Lists)     │
│    • AuthContext (JWT LocalStorage Management)            │
│    • Responsive Enterprise Dashboard & Admin UI           │
└─────────────────────────────┬─────────────────────────────┘
                              │  HTTP REST (Bearer JWT)
                              ▼
┌───────────────────────────────────────────────────────────┐
│               ASP.NET Core 8 Web API Layer                │
│    • REST Controllers (Auth, Articles, Categories, Users) │
│    • JWT Bearer Authentication & Authorization Middleware │
│    • Swagger UI OpenAPI Documentation                     │
└─────────────────────────────┬─────────────────────────────┘
                              │  Connection Factory
                              ▼
┌───────────────────────────────────────────────────────────┐
│             Active Record & DTO Data Layer                │
│    • Models/: Mutation commands (INSERT, UPDATE, DELETE)  │
│    • DTOs/: Optimized multi-table SELECT JOIN queries     │
│    • ADO.NET (Microsoft.Data.SqlClient) Parameterized SQL │
└─────────────────────────────┬─────────────────────────────┘
                              │  Raw T-SQL Execution
                              ▼
┌───────────────────────────────────────────────────────────┐
│            Microsoft SQL Server (KnowledgeVaultDb)        │
│    • Normalized 3NF Relational Database Schema            │
└───────────────────────────────────────────────────────────┘
```

---

## Tech Stack

### Frontend
- **Framework**: React 18 (SPA) with Vite
- **Routing**: React Router DOM (v6)
- **Styling**: Vanilla CSS Design System with responsive design tokens
- **Icons**: React Icons (Feather Icons)
- **Notifications**: React Hot Toast
- **Rich Text**: Native `contentEditable` WYSIWYG formatting engine

### Backend
- **Framework**: ASP.NET Core 8.0 Web API
- **Data Access**: Pure ADO.NET (`Microsoft.Data.SqlClient`)
- **Authentication**: JWT (JSON Web Tokens) with `System.IdentityModel.Tokens.Jwt`
- **Password Security**: `BCrypt.Net-Next` (Salted Cryptographic Hashing)
- **API Documentation**: Swashbuckle Swagger UI (`/swagger`)

### Database
- **Engine**: Microsoft SQL Server 2019/2022 / Express / LocalDB
- **Design**: Fully normalized Third Normal Form (3NF) with referential constraints

---

## Key Features

- **WYSIWYG Rich Text Authoring**: Write articles with inline visual formatting (bold, italics, headings, lists) without Markdown syntax complexity.
- **Role-Based Article Moderation**:
  - **Employees**: Submit drafts that enter a `Pending` state for administrative review.
  - **Admins**: Review submissions, read full contexts, and approve or reject articles with instant status updates.
- **Instant Search & Tag Filtering**: Real-time article lookup across titles and bodies with category filtering and tag aggregation.
- **Interactive Social Features**: Likes, threaded comments, and personal bookmarking for later reference.
- **Administrative Dashboard**: Real-time metric tracking, user access control, and dynamic category creation/deletion.
- **Pure ADO.NET Performance**: Direct T-SQL execution eliminating ORM reflection and change-tracker overhead.

---

## Database Schema (3NF)

The database schema is defined in [`schema.sql`](file:///z:/excel/schema.sql) and consists of 8 core tables:

1. **`Users`**: Stores authentication credentials, hashed passwords, roles (`Admin` / `Employee`), and profile timestamps.
2. **`Categories`**: High-level classification taxonomy for knowledge organization.
3. **`Articles`**: Central repository containing content, status (`Pending`, `Approved`, `Rejected`), author references, and foreign keys.
4. **`Tags`**: Reusable classification keywords.
5. **`ArticleTags`**: Many-to-many bridge mapping articles to multiple tags.
6. **`Comments`**: Relational commentary thread linked to individual articles and user accounts.
7. **`Likes`**: Composite-key tracking user engagements per article.
8. **`Bookmarks`**: User-saved references for quick retrieval.

---

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v18 or higher)](https://nodejs.org/) & npm
- [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express, Developer, or LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or Visual Studio Code

---

### 1. Database Setup

1. Open **SQL Server Management Studio (SSMS)** or your preferred database tool.
2. Connect to your SQL Server instance (e.g., `.\SQLEXPRESS` or `(localdb)\mssqllocaldb`).
3. Open and execute the [`schema.sql`](file:///z:/excel/schema.sql) script:
   ```sql
   -- Creates the KnowledgeVaultDb database, all tables, indices, and default seed data
   ```
4. Verify that default seed users and categories are populated.

---

### 2. Backend Setup

1. Open a terminal in the API folder:
   ```bash
   cd KnowledgeVault.API
   ```
2. Verify connection string in [`appsettings.json`](file:///z:/excel/KnowledgeVault.API/appsettings.json):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.\\SQLEXPRESS;Database=KnowledgeVaultDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
3. Restore dependencies and run the API:
   ```bash
   dotnet restore
   dotnet run
   ```
4. The API will start listening on:
   - **Base URL**: `http://localhost:5000`
   - **Swagger UI**: `http://localhost:5000/swagger`

---

### 3. Frontend Setup

1. Open a new terminal in the UI folder:
   ```bash
   cd knowledge-vault-ui
   ```
2. Install npm dependencies:
   ```bash
   npm install
   ```
3. Start the Vite development server:
   ```bash
   npm run dev
   ```
4. Navigate to `http://localhost:5173` in your browser.

---

## Default Credentials

| Role | Email | Password |
|---|---|---|
| **Admin** | `admin@vault.com` | `Admin@123` |
| **Employee** | `employee@vault.com` | `Employee@123` |

*(You can also register new accounts directly via the Registration screen).*

---

## API Endpoints

### Authentication (`/api/auth`)
- `POST /api/auth/register` — Register a new account (returns JWT).
- `POST /api/auth/login` — Authenticate and receive a JWT Bearer token.
- `GET /api/auth/me` — Retrieve current authenticated user profile.

### Articles (`/api/articles`)
- `GET /api/articles` — Retrieve all approved articles.
- `GET /api/articles/pending` — Retrieve pending articles (*Admin only*).
- `GET /api/articles/{id}` — Get full article details by ID.
- `POST /api/articles` — Create a new article.
- `PUT /api/articles/{id}` — Update article (*Author or Admin*).
- `DELETE /api/articles/{id}` — Delete article (*Author or Admin*).
- `PATCH /api/articles/{id}/approve` — Approve or reject article (*Admin only*).

### Social & Interactions
- `POST /api/articles/{id}/like` — Toggle article like status.
- `POST /api/articles/{id}/bookmark` — Toggle article bookmark.
- `GET /api/bookmarks` — List bookmarked articles for current user.
- `POST /api/articles/{id}/comments` — Post a comment on an article.
- `DELETE /api/comments/{id}` — Delete a comment.

### Administration (`/api/categories`, `/api/users`)
- `GET /api/categories` — List all categories.
- `POST /api/categories` — Create a new category (*Admin only*).
- `DELETE /api/categories/{id}` — Remove a category (*Admin only*).
- `GET /api/users` — List all system users (*Admin only*).
- `DELETE /api/users/{id}` — Deactivate a user (*Admin only*).

---

## Security Features

- **SQL Injection Immunization**: All SQL queries utilize ADO.NET parameterized queries via `SqlCommand.Parameters.AddWithValue()`. User inputs are never concatenated into command text.
- **Cryptographic Password Security**: Passwords are never stored in plain text. Salting and hashing are handled by industry-standard BCrypt.
- **Stateless Authorization**: Secured endpoints enforce `[Authorize]` attributes backed by JSON Web Tokens validating signature, issuer, audience, and expiration.
- **Role Verification**: Admin-restricted endpoints strictly enforce `[Authorize(Roles = "Admin")]`.

---

## Project Structure

```
Knowledge_vault/
├── KnowledgeVault.sln              # Visual Studio Solution entry point
├── schema.sql                      # SQL Server database schema & seed script
│
├── KnowledgeVault.API/             # ASP.NET Core 8 Web API
│   ├── Controllers/                # REST Controllers (Auth, Articles, etc.)
│   ├── Data/                       # DbConnectionFactory for ADO.NET
│   ├── DTOs/                       # Data Transfer Objects with inline query readers
│   ├── Models/                     # Active Record domain models with inline mutations
│   ├── Services/                   # TokenService (JWT generation)
│   ├── Program.cs                  # Dependency Injection & Middleware pipeline
│   └── appsettings.json            # Database connection & JWT config
│
└── knowledge-vault-ui/             # React (Vite) Frontend
    ├── src/
    │   ├── api/                    # Centralized API service methods (Axios)
    │   ├── components/             # Reusable UI components (Navbar, Cards, Comments)
    │   ├── context/                # AuthContext state provider
    │   ├── pages/                  # Route views (Dashboard, Admin, Detail, Editor)
    │   ├── index.css               # Global theme & typography design system
    │   └── App.jsx                 # Route definitions & layout wrappers
    ├── package.json
    └── vite.config.js
```

---

## Troubleshooting & FAQ

**Q: Why do I see a SQL connection error on startup?**  
**A:** Ensure SQL Server is running and the `DefaultConnection` string in `appsettings.json` points to your correct SQL instance name (e.g., `.\SQLEXPRESS` or `(localdb)\mssqllocaldb`).

**Q: How do I debug breakpoints in Visual Studio 2022?**  
**A:** Open `KnowledgeVault.sln`, set your breakpoints (`F9`) inside any Controller or Model method, and press **`F5`**. Send requests from Swagger or the React frontend to step through execution.

---

## License

This project is licensed under the MIT License - see the LICENSE file for details.
