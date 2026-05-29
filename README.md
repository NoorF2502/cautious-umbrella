# Accountant Portal

ASP.NET Core MVC starter project for an accountant/client document workflow.

## Main features

- Identity-based login and registration.
- Separate client and accountant profile/dashboard flows.
- Client dashboard widget for non-sensitive accountant information.
- Client document upload, document status tracking, payment completion, and delete actions.
- Accountant dashboard and "My Clients" page with non-sensitive client details and uploaded documents.
- Automatic report creation with a simple taxable amount reader for text/CSV uploads and configurable tax brackets.
- Notification messages for uploaded documents, generated reports, and completed payments.

## Getting started in Visual Studio

1. Open `AccountantPortal.csproj`.
2. Update `appsettings.json` if you do not want to use LocalDB.
3. Run these commands from the Package Manager Console or terminal:

   ```bash
   dotnet restore
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   dotnet run
   ```

4. Register a user, create either a client or accountant profile, and assign clients to accountants by setting the accountant ID on the client profile.

## Production notes

- Replace manual accountant ID entry with an invitation/assignment workflow.
- Add virus scanning and file type validation for uploads.
- Replace the demo tax calculator with jurisdiction-specific tax rules and verified parsers for PDFs, spreadsheets, and accounting exports.
- Add role-based authorization policies for stricter separation between client and accountant actions.
