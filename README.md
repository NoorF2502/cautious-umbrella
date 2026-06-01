# Cautious Umbrella MVC Workflow

## Existing project review

Before adding code, the repository was checked for ASP.NET MVC assets, controllers, models, views, and project files. The initial repository only contained `.gitkeep`, so there was no original controller, model, view, route, or coding style available to preserve in this checkout.

Because no pre-existing application code was present, the implementation in this branch is intentionally limited to the smallest MVC surface needed for the requested workflow:

- `HomeController` is only used for the landing and error pages.
- `ClientController` contains the client registration, login, dashboard, document, report, and payment actions.
- `AccountantController` contains the accountant registration, login, dashboard, client, document review, and report creation actions.
- No `DashboardController`, `DocumentController`, `ReportController`, or other extra workflow controllers were created.

## How to merge this into an existing MVC project

If you have another local copy that already contains your original ASP.NET MVC code, treat this branch as additive reference code instead of a replacement project:

1. Copy only missing properties from the models in `Models/` into your existing `Accountant`, `Client`, `Document`, and `Report` classes.
2. Copy only missing actions from `ClientController` into your existing client controller.
3. Copy only missing actions from `AccountantController` into your existing accountant controller.
4. Copy the matching Razor view sections into your existing views, preserving your layout, CSS, and naming conventions.
5. Add only the missing `ApplicationDbContext` relationships if your context does not already define them.

This keeps your existing code style and routes intact while still adding the requested client and accountant functionality.

## Implemented workflow coverage

### Client

- Register and log in.
- View a dashboard.
- See assigned accountant contact details.
- Upload documents, images, PDFs, spreadsheets, CSV, text, Word, QBO, and OFX files.
- View uploaded documents and their review/payment statuses.
- Delete only documents that are still pending.
- View accountant reports.
- Mark report payments as completed.

### Accountant

- Register and log in.
- View a dashboard.
- View assigned clients without exposing password hashes in views.
- View client documents, statuses, and payment statuses.
- See recent client activity.
- Mark documents as reviewed.
- Create reports linked to both client and document records.

### Reports

Reports include income, expenses, taxable amount, tax due, payment amount, payment status, notes, and accountant comments. Each report is linked to a client and a document.
