# IsaacForms

**CustomFormsApp - Form Builder & Response Management System**

**Link to deployed app:** [https://isaacforms-1.onrender.com](https://isaacforms-1.onrender.com)

---

## Overview

**CustomFormsApp** is a powerful, full-featured form builder and template management application built with **ASP.NET Core Blazor**. It enables users to create, customize, and share forms for data collection, surveys, questionnaires, and more. The system supports multilingual content, public/private form sharing, and comprehensive response management.

---

## Features

### Form Builder
- **Drag-and-drop Interface**: Intuitively arrange and reorder questions  
- **Multiple Question Types**:  
  Support for single-line text, multi-line text, multiple choice, checkboxes, date, number, email, scale, and dropdown questions  
- **Required Fields**: Mark questions as required/optional  
- **Question Description**: Add detailed descriptions for each question  
- **Real-time Preview**: See what respondents will experience  
- **Auto-saving**: Never lose your work with automatic draft saving  

### Template System
- **Reusable Templates**: Create templates that can be reused across different forms  
- **Template Library**: Browse public templates created by other users  
- **Template Access Control**: Keep templates private or share them publicly  
- **Topic Tags**: Organize templates by categories and topics  
- **Like System**: "Like" templates to bookmark them for later or show appreciation  

### Response Management
- **Response Collection**: Gather and store form submissions  
- **Response Analytics**: View and analyze responses  
- **Export Options**: Export response data for further analysis  
- **Filtering & Sorting**: Organize responses for easier review  

### User Management
- **Authentication**: Secure authentication powered by Clerk  
- **User Profiles**: View and manage your templates and form responses  
- **Admin Controls**: Administrative features for system management  

### Additional Features
- **Multi-language Support**: Built-in localization support (English, Spanish)  
- **Mobile Responsive**: Works smoothly on devices of all sizes  
- **Search Functionality**: Find forms and templates easily  
- **Commenting System**: Interact with other users' templates  

---

## Technology Stack

- **Backend**: ASP.NET Core 8.0  
- **Frontend**: Blazor WebAssembly  
- **Database**: Entity Framework Core with PostgreSQL  
- **Authentication**: Clerk Authentication  
- **UI Framework**: Bootstrap 5  
- **Localization**: Microsoft.Extensions.Localization  

### Other Libraries
- **Markdig**: Markdown rendering  
- **Blazor DragDrop**: Drag-and-drop functionality  
- **AutoMapper**: Object mapping  

---

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later  
- PostgreSQL database  
- Clerk account for authentication (or modify to use a different auth provider)  

### Installation

1. Clone the repository:  
2. Set up environment variables:  
   - Create a `.env` file in the root directory  
3. Run database migrations:  
4. Run the application:  
5. Open your browser and navigate to `https://localhost:5001`  

---

## Docker Deployment

A `Dockerfile` is included for containerized deployment.

---

## Configuration

The application can be configured through `appsettings.json` or environment variables. Key settings include:

- Database connection string  
- Clerk API credentials  
- Admin password  
- Feature toggles  

---

## Development

### Project Structure

- `Components/`: Reusable Blazor components  
- `Data/`: Entity Framework context, models, and migrations  
- `Pages/`: Blazor pages for the application  
- `Services/`: Business logic and services  
- `Extensions/`: Extension methods for additional functionality  
- `Resources/`: Localization and resource files  
- `wwwroot/`: Static files like CSS, JavaScript, and images  

### Adding New Question Types

The system is designed to be extensible. To add a new question type:

1. Add the new type to the `QuestionType` enum  
2. Implement rendering logic in the `FormBuilder` component  
3. Update the `FillForm` component to support the new type  
4. Add response handling for the new question type  

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## License

N/A

---

## Contact

For questions or support, please contact:  
📧 **isak.silva81@gmail.com**
