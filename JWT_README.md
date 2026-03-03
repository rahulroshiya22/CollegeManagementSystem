# 🔐 JWT Authentication & Authorization — College Management System

## 📌 What is JWT?

**JWT (JSON Web Token)** is an open standard (RFC 7519) for securely transmitting information between two parties as a JSON object. It is **digitally signed** using a secret key (HMAC-SHA256 in our project), which means the information can be verified and trusted.

### JWT Token Structure

A JWT token consists of **three parts** separated by dots (`.`):

```
xxxxx.yyyyy.zzzzz
  │       │       │
  │       │       └── 3. Signature  (verifies the token hasn't been tampered with)
  │       └────────── 2. Payload    (contains claims like UserId, Email, Role)
  └────────────────── 1. Header     (algorithm & token type)
```

**Example Token:**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiIxIiwiZW1haWwiOiJhZG1pbkBjbXMuY29tIiwicm9sZSI6IkFkbWluIn0.abc123signature
```

---

## 🏗️ Architecture Overview — How JWT Works in Our Project

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        FRONTEND (Browser)                               │
│                                                                         │
│   1. User enters email + password on Login Page                         │
│   2. Frontend sends POST /api/auth/login                                │
│   3. Receives { accessToken, refreshToken, role, ... }                  │
│   4. Stores tokens in localStorage                                      │
│   5. Every API call includes: Authorization: Bearer <accessToken>       │
│   6. If token expires (401) → auto-refresh using refreshToken           │
└────────────────────────────┬────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     API GATEWAY (Ocelot - Port 7000)                    │
│            Routes requests to the correct microservice                  │
└────────────────────────────┬────────────────────────────────────────────┘
                             │
          ┌──────────────────┼──────────────────────────┐
          ▼                  ▼                           ▼
   ┌──────────────┐  ┌───────────────┐         ┌────────────────┐
   │ AuthService  │  │StudentService │   ...   │ CourseService   │
   │ (Port 5001)  │  │ (Port 5002)   │         │ (Port 5004)    │
   │              │  │               │         │                │
   │ ✅ Generates │  │ ✅ Validates  │         │ ✅ Validates   │
   │  JWT Tokens  │  │  JWT Tokens   │         │  JWT Tokens    │
   │ ✅ Login     │  │ ✅ [Authorize]│         │ ✅ [Authorize] │
   │ ✅ Register  │  │   attribute   │         │   attribute    │
   │ ✅ Refresh   │  │               │         │                │
   └──────────────┘  └───────────────┘         └────────────────┘
```

---

## 📁 File Locations — Where JWT Code Lives

### 🔙 Backend Files

| # | File | Location | Purpose |
|---|------|----------|---------|
| 1 | **JwtService.cs** | `Backend/CMS.AuthService/Services/JwtService.cs` | ⭐ **Core file** — Generates access tokens, refresh tokens, and validates tokens |
| 2 | **AuthService.cs** | `Backend/CMS.AuthService/Services/AuthService.cs` | Uses JwtService during Login, Register, and Token Refresh |
| 3 | **AuthController.cs** | `Backend/CMS.AuthService/Controllers/AuthController.cs` | API endpoints: `/api/auth/login`, `/api/auth/register`, `/api/auth/refresh`, `/api/auth/logout` |
| 4 | **AdminController.cs** | `Backend/CMS.AuthService/Controllers/AdminController.cs` | Role-based authorization example: `[Authorize(Roles = "Admin")]` |
| 5 | **AuthDTOs.cs** | `Backend/CMS.AuthService/DTOs/AuthDTOs.cs` | Request/Response models: `LoginRequest`, `RegisterRequest`, `AuthResponse`, `RefreshTokenRequest` |
| 6 | **Program.cs (AuthService)** | `Backend/CMS.AuthService/Program.cs` | JWT middleware configuration + Swagger JWT setup |
| 7 | **appsettings.json** | `Backend/CMS.AuthService/appsettings.json` | JWT configuration: SecretKey, Issuer, Audience |
| 8 | **CMS.AuthService.csproj** | `Backend/CMS.AuthService/CMS.AuthService.csproj` | NuGet package: `Microsoft.AspNetCore.Authentication.JwtBearer` |

**Other Microservices that validate JWT tokens (same pattern):**

| Service | Program.cs Location |
|---------|---------------------|
| StudentService | `Backend/CMS.StudentService/Program.cs` |
| CourseService | `Backend/CMS.CourseService/Program.cs` |
| FeeService | `Backend/CMS.FeeService/Program.cs` |
| EnrollmentService | `Backend/CMS.EnrollmentService/Program.cs` |
| AttendanceService | `Backend/CMS.AttendanceService/Program.cs` |
| NotificationService | `Backend/CMS.NotificationService/Program.cs` |

### 🎨 Frontend Files

| # | File | Location | Purpose |
|---|------|----------|---------|
| 1 | **auth.js** | `Frontend/assets/js/auth.js` | ⭐ **Core file** — `AuthManager` class for token storage, login, logout, role-based guards |
| 2 | **api.js** | `Frontend/assets/js/api.js` | `APIService` class — auto-attaches `Bearer` token to every request |
| 3 | **login.html** | `Frontend/pages/login.html` | Login form that calls `AuthManager.login()` |
| 4 | **register.html** | `Frontend/pages/register.html` | Registration form that stores tokens after signup |

### 🛠️ Setup Script

| File | Location | Purpose |
|------|----------|---------|
| **Setup-JWT-Auth.ps1** | `Backend/Setup-JWT-Auth.ps1` | PowerShell script that auto-adds JWT configuration to all microservices |

---

## 🔧 Step-by-Step Implementation Guide

### Step 1: Install NuGet Package

Each microservice that needs JWT must install the NuGet package:

```xml
<!-- File: *.csproj (e.g., CMS.AuthService.csproj) -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
```

**Command:**
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
```

---

### Step 2: Configure JWT Settings in `appsettings.json`

```json
// File: Backend/CMS.AuthService/appsettings.json (Line 52-56)

"Jwt": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration12345!",
    "Issuer": "CMS.AuthService",
    "Audience": "CMS.Client"
}
```

| Property | Description |
|----------|-------------|
| `SecretKey` | The secret key used to sign and verify tokens (must be same across all services) |
| `Issuer` | Who issued the token (our AuthService) |
| `Audience` | Who the token is intended for (our Client app) |

> ⚠️ **Important:** The same `SecretKey`, `Issuer`, and `Audience` must be used in ALL microservices for token validation to work across services.

---

### Step 3: Configure JWT Middleware in `Program.cs`

```csharp
// File: Backend/CMS.AuthService/Program.cs (Lines 1-6, 74-98)

// ── Required Namespaces ──
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// ── JWT Authentication Configuration ──
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT Secret Key not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,   // ✅ Verify the token's signature
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,             // ✅ Check who issued the token
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,           // ✅ Check who the token is for
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,           // ✅ Check if token is expired
        ClockSkew = TimeSpan.Zero          // ✅ No grace period for expiry
    };
});

builder.Services.AddAuthorization();

// ── Enable Middleware (ORDER MATTERS!) ──
app.UseAuthentication();   // 👈 Must come BEFORE Authorization
app.UseAuthorization();
```

---

### Step 4: Create the JWT Service (Token Generator)

This is the **heart of JWT implementation** — it creates and validates tokens.

```csharp
// File: Backend/CMS.AuthService/Services/JwtService.cs

// ── Interface ──
public interface IJwtService
{
    string GenerateAccessToken(User user);   // Creates JWT with user claims
    string GenerateRefreshToken();           // Creates random refresh token
    ClaimsPrincipal? ValidateToken(string token);  // Validates a JWT
}

// ── Implementation ──
public class JwtService : IJwtService
{
    public string GenerateAccessToken(User user)
    {
        // 1. Define Claims (data stored inside the token)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("AuthProvider", user.AuthProvider.ToString())
        };

        // 2. Create the signing key from SecretKey
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));

        // 3. Create signing credentials with HMAC-SHA256 algorithm
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 4. Build the JWT token
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),  // Token expires in 24 hours
            signingCredentials: credentials
        );

        // 5. Serialize token to string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random 64-byte token
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
```

**Register in DI Container:**
```csharp
// File: Program.cs (Line 64)
builder.Services.AddScoped<IJwtService, JwtService>();
```

---

### Step 5: Use JWT in Authentication Logic

```csharp
// File: Backend/CMS.AuthService/Services/AuthService.cs (Lines 40-91)

public async Task<AuthResponse?> LoginAsync(LoginRequest request)
{
    // 1. Find user by email
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    if (user == null) return null;

    // 2. Verify password using BCrypt
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return null;

    // 3. Generate JWT Access Token + Refresh Token
    var accessToken = _jwtService.GenerateAccessToken(user);
    var refreshToken = _jwtService.GenerateRefreshToken();

    // 4. Save refresh token to database (expires in 7 days)
    user.RefreshToken = refreshToken;
    user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
    await _context.SaveChangesAsync();

    // 5. Return tokens + user info to frontend
    return new AuthResponse
    {
        AccessToken = accessToken,      // JWT token (24h expiry)
        RefreshToken = refreshToken,    // Random token (7 day expiry)
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role.ToString(),
        UserId = user.UserId
    };
}
```

---

### Step 6: Protect API Endpoints with `[Authorize]`

```csharp
// File: Backend/CMS.AuthService/Controllers/AuthController.cs

[HttpPost("logout")]
[Authorize]                     // 👈 Any logged-in user can access
public async Task<IActionResult> Logout(...) { ... }

[HttpGet("me")]
[Authorize]                     // 👈 Requires valid JWT token
public async Task<IActionResult> GetCurrentUser()
{
    // Extract UserId from JWT claims
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    ...
}

// File: Backend/CMS.AuthService/Controllers/AdminController.cs

[Authorize(Roles = "Admin")]     // 👈 Only Admin role can access
public class AdminController : ControllerBase { ... }
```

---

### Step 7: Frontend — Store and Send JWT Token

**Storing tokens after login:**
```javascript
// File: Frontend/assets/js/auth.js (Lines 43-46, 129-131)

static setTokens(accessToken, refreshToken) {
    localStorage.setItem('accessToken', accessToken);
    if (refreshToken) localStorage.setItem('refreshToken', refreshToken);
}
```

**Auto-attaching token to every API request:**
```javascript
// File: Frontend/assets/js/api.js (Lines 34-36)

// Auto-include JWT token from localStorage
const token = localStorage.getItem('accessToken');
const authHeaders = token ? { 'Authorization': `Bearer ${token}` } : {};
```

**Checking if token is expired (client-side):**
```javascript
// File: Frontend/assets/js/auth.js (Lines 88-104)

static isLoggedIn() {
    const token = AuthManager.getToken();
    if (!token) return false;

    // Decode JWT payload (Base64) and check expiry
    const payload = JSON.parse(atob(token.split('.')[1]));
    const expiry = payload.exp * 1000;  // Convert to milliseconds
    if (Date.now() >= expiry) {
        AuthManager.clearTokens();
        return false;
    }
    return true;
}
```

**Page guard — protecting frontend pages:**
```javascript
// File: Frontend/assets/js/auth.js (Lines 242-257)

static requireAuth(...allowedRoles) {
    if (!AuthManager.isLoggedIn()) {
        window.location.href = 'pages/login.html';  // Redirect to login
        return false;
    }
    if (allowedRoles.length > 0) {
        const userRole = AuthManager.getRole();
        if (!allowedRoles.includes(userRole)) {
            AuthManager.redirectToDashboard();  // Wrong role → redirect
            return false;
        }
    }
    return true;
}

// Usage on any protected page:
initPage('Dashboard', 'dashboard', 'Admin');  // Only Admin can access
```

---

### Step 8: Swagger JWT Configuration (for API Testing)

```csharp
// File: Backend/CMS.AuthService/Program.cs (Lines 32-57)

// Add JWT to Swagger UI so you can test protected APIs
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Enter 'Bearer' followed by your JWT token"
});
```

> **How to use:** In Swagger UI, click the 🔒 **Authorize** button → paste your JWT token → click **Authorize**. Now all protected endpoints will include the token automatically.

---

## 🔄 Complete JWT Flow Diagram

```
    ┌──────────────┐                    ┌──────────────┐
    │   FRONTEND   │                    │   BACKEND    │
    │   (Browser)  │                    │ (AuthService)│
    └──────┬───────┘                    └──────┬───────┘
           │                                    │
    ═══════╪════════════════════════════════════╪═══════════
    LOGIN FLOW                                  │
    ═══════╪════════════════════════════════════╪═══════════
           │  POST /api/auth/login              │
           │  { email, password }               │
           │ ──────────────────────────────────►│
           │                                    │ 1. Find user in DB
           │                                    │ 2. Verify password (BCrypt)
           │                                    │ 3. Generate JWT AccessToken
           │                                    │ 4. Generate RefreshToken
           │                                    │ 5. Save RefreshToken to DB
           │  { accessToken, refreshToken,      │
           │    email, role, ... }              │
           │ ◄──────────────────────────────────│
           │                                    │
           │  localStorage.setItem(             │
           │    'accessToken', token)            │
           │                                    │
    ═══════╪════════════════════════════════════╪═══════════
    API REQUEST FLOW                            │
    ═══════╪════════════════════════════════════╪═══════════
           │  GET /api/student                  │
           │  Header: Authorization:            │
           │    Bearer eyJhbGci...              │
           │ ──────────────────────────────────►│
           │                                    │ JWT Middleware validates:
           │                                    │  ✅ Signature
           │                                    │  ✅ Issuer
           │                                    │  ✅ Audience  
           │                                    │  ✅ Expiry
           │                                    │  ✅ Role (if [Authorize(Roles)])
           │  { student data }                  │
           │ ◄──────────────────────────────────│
           │                                    │
    ═══════╪════════════════════════════════════╪═══════════
    TOKEN REFRESH FLOW                          │
    ═══════╪════════════════════════════════════╪═══════════
           │  (Token expired → 401 response)    │
           │                                    │
           │  POST /api/auth/refresh            │
           │  { refreshToken }                  │
           │ ──────────────────────────────────►│
           │                                    │ 1. Find user by refreshToken
           │                                    │ 2. Check refreshToken expiry
           │                                    │ 3. Generate NEW accessToken
           │                                    │ 4. Generate NEW refreshToken
           │  { newAccessToken, newRefreshToken }│
           │ ◄──────────────────────────────────│
           │                                    │
    ═══════╪════════════════════════════════════╪═══════════
    LOGOUT FLOW                                 │
    ═══════╪════════════════════════════════════╪═══════════
           │  POST /api/auth/logout             │
           │  { refreshToken }                  │
           │ ──────────────────────────────────►│
           │                                    │ Remove refreshToken from DB
           │  localStorage.removeItem(          │
           │    'accessToken')                  │
           │  Redirect → Login Page             │
           └────────────────────────────────────┘
```

---

## 🎯 JWT Claims in Our Project

When a token is generated, the following **claims** (user data) are embedded:

| Claim | .NET Constant | Example Value | Purpose |
|-------|---------------|---------------|---------|
| `NameIdentifier` | `ClaimTypes.NameIdentifier` | `"1"` | User's database ID |
| `Email` | `ClaimTypes.Email` | `"admin@cms.com"` | User's email |
| `GivenName` | `ClaimTypes.GivenName` | `"Rahul"` | First name |
| `Surname` | `ClaimTypes.Surname` | `"Sharma"` | Last name |
| `Role` | `ClaimTypes.Role` | `"Admin"` | User role (Admin/Teacher/Student) |
| `AuthProvider` | Custom claim | `"Local"` | Login method (Local/Google) |
| `PhotoUrl` | Custom claim | `"/uploads/..."` | Profile photo URL (if exists) |

---

## ⏰ Token Expiry Configuration

| Token Type | Expiry Duration | Where Configured |
|------------|----------------|------------------|
| **Access Token (JWT)** | 24 hours | `JwtService.cs` → Line 54: `DateTime.UtcNow.AddHours(24)` |
| **Refresh Token** | 7 days | `AuthService.cs` → Line 74: `DateTime.UtcNow.AddDays(7)` |

---

## 🔑 Key Libraries & NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | JWT Bearer authentication middleware |
| `System.IdentityModel.Tokens.Jwt` | (included) | JWT token creation and validation |
| `Microsoft.IdentityModel.Tokens` | (included) | Token validation parameters & security keys |
| `BCrypt.Net-Next` | (included) | Password hashing (used alongside JWT) |

---

## 🧪 How to Test JWT

### Using Swagger UI:
1. Run the AuthService: `dotnet run` in `Backend/CMS.AuthService/`
2. Open `https://localhost:5001/swagger`
3. Call `POST /api/auth/login` with email and password
4. Copy the `accessToken` from the response
5. Click 🔒 **Authorize** button → paste the token → click **Authorize**
6. Now test any `[Authorize]` protected endpoint

### Using Postman:
1. Login via `POST https://localhost:7000/api/auth/login`
2. Copy `accessToken` from response
3. For protected APIs, add header:
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
   ```

---

## 📚 Summary

| Component | Technology | Role |
|-----------|-----------|------|
| Token Generation | `JwtService.cs` + HMAC-SHA256 | Creates signed JWT tokens with user claims |
| Token Validation | JWT Middleware in `Program.cs` | Automatically validates every incoming request |
| Password Security | BCrypt (12 rounds) | Hashes passwords before storing in DB |
| API Protection | `[Authorize]` attribute | Blocks unauthorized access to endpoints |
| Role-Based Access | `[Authorize(Roles = "Admin")]` | Restricts endpoints to specific roles |
| Frontend Auth | `AuthManager` class in `auth.js` | Manages tokens, login state, and page guards |
| API Calls | `APIService` class in `api.js` | Auto-attaches Bearer token to all requests |
| Token Refresh | `POST /api/auth/refresh` | Gets new tokens when access token expires |

---

> 📖 **For Viva Preparation:** Focus on understanding the flow — User logs in → gets JWT → token is stored in browser → sent with every request → server validates token → allows or denies access. The refresh token mechanism allows users to stay logged in without re-entering credentials.
