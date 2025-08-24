# ExpertEase

ExpertEase is a marketplace platform that connects clients with specialists for on-site services.  
It is built with **Angular** (frontend), **.NET** (backend), **PostgreSQL** (relational data), and **Firebase Realtime Database** (non-relational data).  
Payments are handled with **Stripe Connect**, location features use **Google Maps** and the user can login and register using social login by **Google Sign-In** and **Facebook Login**.

---

## Features
- User registration & role management (client, specialist, admin)
- Service requests and real-time messaging
- Booking & secure payments (Stripe Connect)
- Location search & maps integration
- Admin dashboard for moderation and payouts

---

## Tech Stack
- **Frontend**: Angular  
- **Backend**: .NET 8, EF Core  
- **Database**: PostgreSQL, Firebase Realtime Database
- **Payments**: Stripe Connect
- **Maps**: Google Maps API
- **Social Login**: Google Sign-In, Facebook Login

---

## Setup

### Prerequisites
- Node.js 18+  
- .NET 8 SDK  
- PostgreSQL  
- Firebase project (Firestore)  
- Stripe account (test mode)  
- Google Maps API key
- Google Sign-In API key
- Facebook Login App Id

### Run locally

**Backend**
```bash
cd backend/ExpertEase.API
dotnet run
```

**Frontend**
```bash
cd frontend
npm install
npm run start
```
- Backend runs at: http://localhost:5241
- Frontend runs at: http://localhost:4200

## Configuration

- **Backend**: `appsettings.Development.json`  
  - Connection strings  
  - Firebase credentials  
  - Stripe keys  

- **Frontend**: `src/environments/environment.ts`  
  - API base URL  
  - Restricted Google Maps key  
