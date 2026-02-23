# Frontend Setup Guide

## 🚀 Quick Start

### Prerequisites
- Node.js 16+ and npm
- Backend microservices running on ports 7000-7008

### Installation Steps

1. **Install Dependencies**
```bash
cd Frontend
npm install
```

2. **Environment Configuration**
```bash
cp .env.example .env
# Edit .env with your configuration
```

3. **Start Development Server**
```bash
npm start
```

The application will be available at `http://localhost:3000`

## 🔧 Configuration

### Environment Variables
Required variables in `.env`:
- `REACT_APP_API_URL`: Backend API gateway URL
- `REACT_APP_GOOGLE_CLIENT_ID`: Google OAuth client ID

### Backend Services
Ensure these services are running:
- API Gateway: `https://localhost:7000`
- Auth Service: `https://localhost:7007`
- Student Service: `https://localhost:7001`
- Course Service: `https://localhost:7002`
- And other microservices...

## 📱 Access Information

### Demo Credentials
- **Admin**: admin@college.edu / admin123
- **Teacher**: teacher@college.edu / teacher123  
- **Student**: student@college.edu / student123

### Role-Based Access
- Login redirects to appropriate dashboard based on user role
- Navigation menu adapts to user permissions
- Features are role-protected

## 🎨 Features Overview

### Admin Dashboard
- System analytics and statistics
- User management (students, teachers)
- Course and department management
- Fee structure configuration
- Complete system control

### Teacher Dashboard  
- Course management and scheduling
- Student attendance tracking
- Grade assignment and management
- Exam creation and evaluation

### Student Dashboard
- Course enrollment and management
- Academic progress tracking
- Fee payment processing
- Personal information management

## 🔐 Authentication

### Login Methods
- Email/Password authentication
- Google OAuth integration
- JWT token-based security
- Automatic token refresh

### Security Features
- Role-based access control
- Protected routes
- Session management
- Password encryption

## 📊 Key Modules

### Student Management
- Registration and profiles
- Academic records
- Search and filtering
- Bulk operations

### Course Management
- Course catalog
- Enrollment system
- Schedule management
- Department organization

### Attendance System
- Real-time tracking
- Reporting and analytics
- Bulk attendance marking
- Export functionality

### Fee Management
- Payment processing
- Fee structures
- Financial reports
- Receipt generation

### Examination System
- Exam scheduling
- Grade management
- Result publishing
- Performance analytics

## 🎯 UI/UX Features

### Modern Design
- Responsive layout (mobile-first)
- Tailwind CSS styling
- Interactive components
- Smooth animations

### User Experience
- Intuitive navigation
- Real-time notifications
- Loading states
- Error handling

### Data Visualization
- Interactive charts (Recharts)
- Dashboard analytics
- Performance metrics
- Export capabilities

## 🛠️ Development

### Available Scripts
```bash
npm start          # Start development server
npm run build      # Build for production
npm test           # Run tests
npm run eject      # Eject (one-way operation)
```

### Project Structure
```
src/
├── components/     # Reusable UI components
├── pages/         # Page components
├── services/      # API service layers
├── store/         # Redux state management
├── utils/         # Utility functions
└── styles/        # Global styles
```

## 🔍 Troubleshooting

### Common Issues

1. **CORS Errors**
   - Ensure backend is running on correct ports
   - Check API gateway configuration

2. **Authentication Issues**
   - Verify JWT configuration
   - Check token refresh logic

3. **Build Errors**
   - Clear node_modules and reinstall
   - Check Node.js version compatibility

### Debug Mode
- Use React DevTools for state inspection
- Check browser console for errors
- Verify network requests in DevTools

## 📦 Production Deployment

### Build Process
```bash
npm run build
```

### Deployment Options
- Static hosting (Vercel, Netlify)
- Server deployment (Nginx, Apache)
- Docker containerization
- Cloud platforms (AWS, Azure)

## 🤝 Contributing

1. Follow the existing code style
2. Write tests for new features
3. Update documentation
4. Submit pull requests

## 📞 Support

For technical support:
- Check the documentation
- Review error logs
- Contact development team
- Create GitHub issues

---

Happy coding! 🎉
