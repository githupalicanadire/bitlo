# EShop Microservices - Authentication Implementation

## Overview

The EShop Microservices application has been enhanced with comprehensive authentication using Identity Server 4. This document outlines all the authentication features that have been implemented.

## 🔐 Authentication System Features

### Identity Server Configuration

- **Identity Server URL**: `http://localhost:6006`
- **Supported Scopes**: openid, profile, email, roles, catalog.api, basket.api, ordering.api, discount.grpc, shopping.web, gateway.api
- **Client Configuration**: shopping.web client with proper redirect URIs
- **CORS Configuration**: Allows requests from shopping web app (port 5000)

### Protected Routes

The following routes require user authentication:

1. **Cart Management**

   - `/cart` - View shopping cart
   - Add to cart functionality (POST requests)

2. **Order Management**

   - `/orderlist` - View order history
   - `/orderdetail` - View specific order details
   - `/checkout` - Checkout process
   - `/confirmation` - Order confirmation

3. **User Account**
   - Profile management (future implementation)

### Authentication Flow

#### 1. Login Process

- Users are redirected to Identity Server for authentication
- OpenID Connect (OIDC) flow with PKCE security
- Return URL preservation for seamless navigation after login
- User-friendly error handling and messaging

#### 2. Logout Process

- Signs out from both application cookies and Identity Server
- Redirects to home page after logout
- Cleans up all authentication tokens

#### 3. Token Management

- Automatic token refresh
- Bearer token injection for API calls
- Secure token storage in HTTP context

## 🎯 User Experience Features

### Authentication Warnings

- **Product List Page**: Info banner for non-authenticated users
- **Product Detail Page**: Warning when trying to add items to cart
- **Home Page**: Benefits section promoting account creation

### Protected Access

- Automatic redirect to login for protected pages
- Contextual messages based on the requested action
- Return URL preservation for smooth user experience

### Navigation Enhancement

- Dynamic navigation based on authentication status
- User dropdown with profile options when logged in
- Clear sign-in/register buttons when not authenticated

## 🛡️ Security Features

### API Protection

- All API calls include Bearer authentication tokens
- Automatic token refresh handling
- Secure HTTP client configuration

### Session Management

- Cookie-based session with sliding expiration (60 minutes)
- Secure cookie configuration
- HTTPS enforcement in production

### CORS Security

- Restricted origins for Identity Server
- Secure credential handling
- Proper preflight request handling

## 🔧 Technical Implementation

### Middleware Configuration

```csharp
// Authentication middleware checks protected paths
var protectedPaths = new[]
{
    "/cart",
    "/checkout",
    "/orderlist",
    "/orderdetail",
    "/confirmation"
};
```

### Service Registration

- HttpClient with authentication handler
- Refit clients for API communication
- User service for claims management

### Identity Server Client

```csharp
ClientId = "shopping.web"
AllowedGrantTypes = GrantTypes.Code
RequirePkce = true
AllowOfflineAccess = true
```

## 📱 User Interface Components

### Authentication Status Indicators

- User name display in navigation
- Authentication required alerts
- Login/logout buttons
- Registration promotion sections

### Error Handling

- Authentication failure messages
- Network error handling
- User-friendly error displays

## 🚀 Benefits for Users

### Authenticated Users Can:

- ✅ Save items to cart across sessions
- ✅ View order history and track orders
- ✅ Faster checkout with saved information
- ✅ Access personalized recommendations
- ✅ Receive exclusive offers and notifications

### Guest Users:

- ⚠️ Can browse products but cannot add to cart
- ⚠️ Cannot place orders or track purchases
- ⚠️ Must sign in for any transactional features

## 🔄 Integration Points

### Microservices Integration

- **Catalog API**: Public access for browsing
- **Basket API**: Requires authentication for all operations
- **Ordering API**: Requires authentication for all operations
- **Identity API**: Handles authentication and user management

### Gateway Configuration

- API Gateway on port 6004
- Routing to appropriate microservices
- Authentication token forwarding

## 📋 Configuration Summary

### Key Configuration Files Updated:

1. `src/WebApps/Shopping.Web/Program.cs` - Authentication middleware and services
2. `src/WebApps/Shopping.Web/appsettings.json` - Identity Server URL
3. `src/Services/Identity/Identity.API/Configuration/Config.cs` - Client and scope configuration
4. `src/Services/Identity/Identity.API/appsettings.json` - Issuer URI configuration
5. `src/WebApps/Shopping.Web/wwwroot/css/style.css` - Authentication UI styles

### Port Configuration:

- **Shopping Web**: 5000
- **Identity Server**: 6006
- **API Gateway**: 6004

## 🎉 Result

The application now provides a complete authentication experience where:

- Users must sign in to perform any shopping actions
- Clear messaging guides users to create accounts
- Secure token-based API communication
- Seamless user experience with proper redirects
- Professional UI with authentication status indicators

All authentication requirements have been successfully implemented according to the Identity Server configuration provided!
