# VietVoyage - Travel Agency Management System

VietVoyage (HV Travel) is a comprehensive, modern web application designed for managing a high-end travel agency. Built with ASP.NET Core and following Clean Architecture principles, it provides a robust platform for managing tours, bookings, customers, and financial transactions.

## 🚀 Key Features

### 🌟 Usage & Dashboard
-   **Interactive Dashboard:** Real-time overview of business performance with KPI cards and charts.
-   **Dark Mode Support:** Fully synchronized dark mode across all pages, including authentication screens, with `localStorage` persistence.
-   **Responsive Design:** Optimized for various devices using Tailwind CSS.

### 📦 Tour Management
-   **CRUD Operations:** specific flows for creating, editing, and managing tours.
-   **Advanced Filtering:** Filter tours by City, Category, Price Range, and Duration using interactive UI chips.
-   **Soft Delete & Archiving:** Safe deletion mechanisms to preserve data integrity.
-   **Rich Content:** Support for detailed tour descriptions, images, and itineraries.

### 🔐 Authentication & Security
-   **Secure Auth Flow:** integrated Login, Register, Forgot Password, and Account Pending pages.
-   **Role-Based Access:** Administrative interfaces protected by authentication.
-   **Modern UI:** Glassmorphism and high-quality visual effects on auth pages.

### 👥 Customer & Booking Management
-   **Customer Profiles:** Detailed view of customer information and history.
-   **Booking Tracking:** Monitor booking status and details.
-   **Payment Processing:** Manage transactions, refunds, and expenses.

## 🛠️ Technology Stack

-   **Backend:** ASP.NET Core (Clean Architecture)
-   **Frontend:** ASP.NET Core MVC / Razor Views
-   **Styling:** Tailwind CSS (via CDN/Processing)
-   **Database:** MongoDB
-   **Infrastructure:** Dependency Injection, Repository Pattern

## 📂 Project Structure

-   **`VietVoyage.Domain`**: Core business entities and interfaces.
-   **`VietVoyage.Application`**: Business logic, services, and DTOs.
-   **`VietVoyage.Infrastructure`**: Data access implementation (MongoDB context, repositories).
-   **`VietVoyage.Web`**: Presentation layer (Controllers, Views, Static files).

## ⚡ Getting Started

### Prerequisites
-   [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 or later recommended)
-   [MongoDB](https://www.mongodb.com/try/download/community) (Local or Atlas)

### Installation

1.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    cd ASP.NET-HV-Travel
    ```

2.  **Configure Environment:**
    -   Ensure your MongoDB connection string is correctly set in `appsettings.json` or `.env` file (if used).
    -   Example `appsettings.json`:
        ```json
        {
          "ConnectionStrings": {
            "MongoDb": "mongodb://localhost:27017/VietVoyageDb"
          }
        }
        ```

3.  **Run the Application:**
    Navigate to the Web project directory and run:
    ```bash
    cd VietVoyage.Web
    dotnet run
    ```
    Or use `dotnet watch run` for hot reloading during development.

4.  **Access the App:**
    Open your browser and navigate to `https://localhost:7198` (or the port indicated in the terminal).

## 🎨 UI/UX Highlights
-   **Consistent Theming:** Standardized color palette (`primary`, `surface-dark`, `background-dark`) used throughout.
-   **Interactive Elements:** Hover effects, transitions, and dynamic filter chips.
-   **Polish:** Custom scrollbars, glassmorphism overlays, and refined typography (Be Vietnam Pro).

---
*Developed for HV Travel.*
