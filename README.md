# 🎉 Smart Event System

Smart Event System is a web-based application built using **ASP.NET Core MVC** that helps manage and organize events efficiently. It provides a complete solution for event creation, participant management, and secure payment processing.

---

## 🚀 Features

### 📅 Event Management

* Create, update, and delete events
* Manage event details (date, time, venue, description)
* View list of all events

### 👥 Participant Management

* Register participants for events
* Store participant details securely
* Manage attendee lists

### 💳 Payment Integration

* Integrated with **Stripe Payment Gateway**
* Secure payment handling
* Supports test and live modes

### 📊 Admin Panel

* Dashboard for managing events and participants
* Easy navigation using MVC structure
* Centralized data control

### 🗂️ Database Management

* SQL Server (SSMS) integration
* Entity Framework Core (ORM)
* Migration support

---

## 🛠️ Tech Stack

* **Backend:** ASP.NET Core MVC (.NET)
* **Language:** C#
* **Frontend:** HTML, CSS, Bootstrap, jQuery
* **Database:** SQL Server (SSMS)
* **ORM:** Entity Framework Core
* **Payment Gateway:** Stripe

---

## 📁 Project Structure

```
SmartEventSystem/
│
├── SmartEvent.Web/
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── Data/
│   ├── Migrations/
│   ├── wwwroot/
│   ├── appsettings.json
│   └── Program.cs
│
└── SmartEventSystem.sln
```

---

## ⚙️ Setup Instructions

### 1️⃣ Clone Repository

```bash
git clone https://github.com/Manthan1706/SmartEvent.git
cd SmartEvent
```

---

### 2️⃣ Open Project

* Open `.sln` file in **Visual Studio 2022**

---

### 3️⃣ Configure Database (SSMS)

* Open **SQL Server (SSMS)**
* Create a new database:

```sql
SmartEventDb
```

* Update connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=SmartEventDb;Trusted_Connection=True;"
}
```

---

### 4️⃣ Run Migrations

```bash
dotnet ef database update
```

---

### 5️⃣ Configure Stripe (IMPORTANT)

⚠️ Do NOT store real keys in code

Update in `appsettings.json`:

```json
"Stripe": {
  "PublishableKey": "YOUR_KEY",
  "SecretKey": "YOUR_SECRET"
}
```

👉 Recommended: Use User Secrets

```bash
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "your_real_key"
```

---

### 6️⃣ Run Project

* Press **F5** or click **Run**
* Application will start in browser

---

## 🔐 Security Notes

* Do not store API keys in code
* Use environment variables or user secrets
* `.gitignore` excludes sensitive files


---


## 📸 Screenshots

<img width="1888" height="884" alt="image" src="https://github.com/user-attachments/assets/3a97fd93-c293-4164-ae76-4c65181f9715" />

## Organizer Dashboard 
<img width="1863" height="890" alt="image" src="https://github.com/user-attachments/assets/7b104a3a-bf29-4a17-9deb-fe7e2edfd87f" />

## Admin Dashboard 
<img width="1547" height="598" alt="image" src="https://github.com/user-attachments/assets/8963b20f-e00e-44e3-81f4-3856d25991ed" />

## User Dashboard 
<img width="1892" height="632" alt="image" src="https://github.com/user-attachments/assets/696e536b-f394-430d-a192-97336bb68263" />

---


## 🚀 Future Enhancements

* Role-based authentication
* Email & SMS notifications
* Event analytics
* Mobile responsive UI

---

## 👨‍💻 Author

**Manthan Rupapara**
Software Developer

---

## 📄 License

This project is for learning and demonstration purposes.
