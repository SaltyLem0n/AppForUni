# RSU360 DataCentric (AppForUni)

A web-based application for managing lucky draw events, employee prize searching, and winner announcements for Rangsit University (RSU360 DataCentric). Built with ASP.NET Core MVC.

## 🚀 Features

- **Employee Search**: Allows staff to search for their employee ID to check if they have won a prize.
- **Prize Selection**: Interface for eligible employees to select their preferred prizes.
- **Winner Announcement**: Public display of lucky draw winners with interactive animations (Confetti, Money Rain).
- **Admin Dashboard**:
  - Manage employee data.
  - Secure Admin authentication with password hashing.
  - Export and management of prize data.
- **Responsive Design**: Modern UI using **TailwindCSS** and **Bootstrap**, fully optimizing for mobile and desktop.
- **Thai Language Support**: Full Thai localization for frontend interfaces.

## 🛠 Technology Stack

- **Backend**: ASP.NET Core 6.0/8.0 (MVC Framework)
- **Database**: Microsoft SQL Server (MSSQL) with Entity Framework Core
- **Frontend**:
  - Razor Views (.cshtml)
  - TailwindCSS (via CDN)
  - Bootstrap 5
  - Vanilla JavaScript for animations and interactions
- **Authentication**: ASP.NET Core Identity / Custom Cookie Authentication

## 📦 Installation & Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/AppForUni.git
   cd AppForUni
   ```

2. **Database Configuration**
   - Update the connection string in `appsettings.json`.
   - Run Entity Framework migrations to set up the schema:
     ```bash
     dotnet ef database update
     ```

3. **Run the Application**
   ```bash
   dotnet run
   ```
   The application will typically start at `https://localhost:7251` or `http://localhost:5166`.

4. **Admin Access**
   - Ensure the admin user is seeded (Standard ID: `1`, Password as configured).

## 📂 Project Structure

- `Controllers/`: Contains core logic for Account, Employees, and Prizes.
- `Views/`: Razor pages for the UI (Search, Select, Announce).
- `Models/`: Entity Framework data models (Employee, PrizeAward, etc.).
- `wwwroot/`: Static assets (CSS, JS, Images).

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.