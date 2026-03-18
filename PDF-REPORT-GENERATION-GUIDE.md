# PDF Report Generation Guide (QuestPDF)

## Overview

The SAFARIstack PMS includes a comprehensive report generation service using **QuestPDF** - a lightweight, fluent C# library for generating PDFs without external dependencies.

**IReportService** provides:
- Booking invoices (with guest details, rates, taxes)
- Occupancy reports (daily/monthly occupancy rates)
- Revenue reports (ADR, RevPAR, total revenue by period)
- Guest manifests (arrivals/departures for the day)
- Staff schedules (shift assignments, coverage)
- Financial statements (income, expenses, net profit)

## Installation

### 1. Install QuestPDF NuGet Package

```bash
# Via Package Manager
dotnet add package QuestPDF

# Or in Package Manager Console
Install-Package QuestPDF
```

### 2. Configure License

QuestPDF offers:
- **Community License** (free) - For companies with <$1M annual revenue
- **Professional License** - For commercial use

Get your license key from: https://www.questpdf.com/

### 3. Set License in Program.cs

Add to `Program.cs` initialization (before building the app):

```csharp
// Set QuestPDF license (required for usage)
// Community license is free for companies <$1M revenue
QuestPDF.Settings.License = new QuestPDF.Infrastructure.XmlLicenseKey(
    Environment.GetEnvironmentVariable("QUESTPDF_LICENSE_KEY") 
    ?? "your-license-key-here");
```

Or set environment variable:
```bash
set QUESTPDF_LICENSE_KEY=your-license-key
```

## Service Registration

Already registered in `Program.cs` (line ~266):

```csharp
builder.Services.AddScoped<IReportService, ReportService>();
```

## Usage in Endpoints

### Inject the Service

```csharp
app.MapGet("/api/reports/invoice/{bookingId}", async (
    Guid bookingId,
    IReportService reportService) =>
{
    var result = await reportService.GenerateBookingInvoiceAsync(bookingId);
    
    if (!result.IsSuccess)
        return Results.BadRequest(result.ErrorMessage);

    return Results.File(result.FileContent, result.ContentType, result.FileName);
})
.WithName("GetBookingInvoice")
.WithOpenApi()
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);
```

## Core Methods

### Booking Invoice

```csharp
// Generate PDF invoice for a specific booking
var result = await reportService.GenerateBookingInvoiceAsync(bookingId: Guid.Parse("..."));

// Returns:
// - FileName: "Invoice_{bookingId:N}.pdf"
// - FileContent: PDF bytes
// - ContentType: "application/pdf"
// - ErrorMessage: null if successful

// Usage:
if (result.IsSuccess)
{
    return Results.File(result.FileContent, result.ContentType, result.FileName);
}
```

**Booking Invoice Contents:**
- Property letterhead and logo
- Booking ID, guest name, contact details
- Check-in date, check-out date, number of nights
- Room number, room type, bed configuration
- Item breakdown (room rate, taxes, discounts, extras)
- Total cost with currency
- Payment status and method
- Cancellation policy
- Terms & conditions
- Payment instructions

### Occupancy Report

```csharp
// Generate occupancy report for date range
var result = await reportService.GenerateOccupancyReportAsync(
    propertyId: Guid.Parse("..."),
    startDate: new DateTime(2024, 3, 1),
    endDate: new DateTime(2024, 3, 31));

// Returns:
// - FileName: "Occupancy_20240301_to_20240331.pdf"
```

**Report Contents:**
- Property name and date range
- Daily occupancy table:
  - Date, available rooms, occupied rooms, vacant rooms, dirty rooms, maintenance
  - Occupancy percentage per day
  - Trend analysis (graph)
- Monthly summary:
  - Total rooms available, occupied, vacant
  - Average occupancy rate
  - Peak occupancy date
  - Lowest occupancy date
- Departmental notes

### Revenue Report

```csharp
// Generate revenue report for date range
var result = await reportService.GenerateRevenueReportAsync(
    propertyId: Guid.Parse("..."),
    startDate: new DateTime(2024, 3, 1),
    endDate: new DateTime(2024, 3, 31));

// Returns:
// - FileName: "Revenue_20240301_to_20240331.pdf"
```

**Report Contents:**
- Daily revenue breakdown:
  - Check-ins, check-outs, occupancy
  - Room revenue, other revenue sources
  - Average Daily Rate (ADR)
  - Revenue Per Available Room (RevPAR)
- Revenue trends (graph)
- Top performing days
- Lowest revenue days
- Projections for next period
- Variance analysis vs. budget

### Guest Manifest

```csharp
// Generate daily guest list (arrivals/departures)
var result = await reportService.GenerateGuestManifestAsync(
    propertyId: Guid.Parse("..."),
    date: DateTime.Today);

// Returns:
// - FileName: "GuestManifest_20240318.pdf"
```

**Manifest Contents:**
- Property name and date
- **Today's Arrivals:**
  - Guest name, room number, check-in time
  - Duration of stay, departure date
  - Number in party, special requests
  - Contact details (name on booking)
- **Today's Departures:**
  - Guest name, room number, check-out time
  - Final balance payable/paid
  - Contact information
- **In-House Guests:**
  - Guest name, room number, arrival date
  - Departure date, nights remaining
  - Special notes or requests
- **Late Checkouts:**
  - Guests approved for late departure
  - Approved checkout time

### Financial Statement

```csharp
// Generate comprehensive financial report
var result = await reportService.GenerateFinancialStatementAsync(
    propertyId: Guid.Parse("..."),
    startDate: new DateTime(2024, 3, 1),
    endDate: new DateTime(2024, 3, 31));

// Returns:
// - FileName: "FinancialStatement_20240301_to_20240331.pdf"
```

**Statement Contents:**
- **Income Section:**
  - Room revenue (nightly rates, upgrades)
  - Ancillary revenue (parking, minibar, services)
  - Taxes and surcharges
  - Adjustments (discounts, corrections)
  - **Total Income**
  
- **Expense Section:**
  - Operating expenses (staff, utilities)
  - Maintenance and repairs
  - Marketing and sales
  - Administrative costs
  - **Total Expenses**
  
- **Summary:**
  - **Gross Profit = Income - Expenses**
  - Profit margin percentage
  - Comparison to previous period
  - Year-to-date totals

## Generic Report Generation

```csharp
// Generate any report type with flexible parameters
var request = new ReportRequest(
    Type: ReportType.OccupancyReport,
    PropertyId: Guid.Parse("..."),
    StartDate: DateTime.Today.AddMonths(-1),
    EndDate: DateTime.Today,
    Format: "PDF");

var result = await reportService.GenerateReportAsync(request);
```

## Return HTTP File

```csharp
// Endpoint that downloads the PDF
app.MapGet("/api/reports/{reportType}/{propertyId}", async (
    string reportType,
    Guid propertyId,
    IReportService reportService) =>
{
    if (!Enum.TryParse<ReportType>(reportType, ignoreCase: true, out var type))
        return Results.BadRequest("Invalid report type");

    var request = new ReportRequest(
        Type: type,
        PropertyId: propertyId,
        StartDate: DateTime.Today.AddMonths(-1),
        EndDate: DateTime.Today);

    var result = await reportService.GenerateReportAsync(request);

    if (!result.IsSuccess)
        return Results.BadRequest(result.ErrorMessage);

    // Return as downloadable PDF
    return Results.File(
        result.FileContent,
        result.ContentType,
        result.FileName,
        enableRangeProcessing: true);
})
.WithName("GenerateReport")
.WithOpenApi()
.Produces(StatusCodes.Status200OK, "application/pdf")
.Produces(StatusCodes.Status400BadRequest);
```

## QuestPDF Fluent API

### Basic Document Structure

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;

var pdfBytes = Document.Create(container =>
{
    container.Page(page =>
    {
        // Set page size and margins
        page.Size(PageSizes.A4);
        page.Margin(2, PageUnit.Centimetre);
        
        // Header section
        page.Header()
            .Height(3, PageUnit.Centimetre)
            .Background(Colors.Grey.Lighten2)
            .Padding(0.5, PageUnit.Centimetre)
            .Text("INVOICE")
            .FontSize(20)
            .Bold();
        
        // Main content area
        page.Content()
            .Column(column =>
            {
                column.Spacing(0.5, PageUnit.Centimetre);
                
                column.Item()
                    .Text($"Invoice #: {invoiceNumber}");
                
                column.Item()
                    .Text($"Date: {DateTime.Now:yyyy-MM-dd}");
            });
        
        // Footer section
        page.Footer()
            .AlignCenter()
            .Text($"Page {page.PageNumber}")
            .FontSize(10)
            .FontColor(Colors.Grey.Darken2);
    });
}).GeneratePdf();
```

### Tables

```csharp
page.Content()
    .Table(table =>
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(3);      // 60%
            columns.RelativeColumn(1);      // 20%
            columns.RelativeColumn(1);      // 20%
        });
        
        // Table header
        table.Header(header =>
        {
            header.Cell().Text("Description").Bold().FontSize(12);
            header.Cell().AlignRight().Text("Qty").Bold();
            header.Cell().AlignRight().Text("Amount").Bold();
        });
        
        // Table rows
        foreach (var item in items)
        {
            table.Cell().Text(item.Description);
            table.Cell().AlignRight().Text(item.Quantity.ToString());
            table.Cell().AlignRight().Text($"${item.Amount:F2}");
        }
    });
```

### Styling

```csharp
// Text
column.Item()
    .Text("Hello World")
    .FontSize(14)
    .Bold()
    .FontColor(Colors.Blue.Darken1)
    .AlignCenter();

// Borders and backgrounds
column.Item()
    .BorderLeft(2)
    .BorderColor(Colors.Blue.Medium)
    .Background(Colors.Blue.Lighten5)
    .Padding(1, PageUnit.Centimetre)
    .Text("Highlighted text");

// Layout
row.RelativeColumn(1).Text("Left");
row.RelativeColumn(1).Text("Right").AlignRight();

// Spacing
column.Spacing(0.5, PageUnit.Centimetre);
column.Item().Padding(1, PageUnit.Centimetre).Text("Padded text");
```

### Multi-Page Documents

```csharp
Document.Create(container =>
{
    // Page 1
    container.Page(page =>
    {
        page.Content().Text("Page 1");
    });
    
    // Page 2
    container.Page(page =>
    {
        page.Content().Text("Page 2");
    });
    
    // Dynamic pages
    foreach (var item in largeDataset)
    {
        container.Page(page =>
        {
            page.Content().Text(item.ToString());
        });
    }
}).GeneratePdf();
```

## Example: Complete Invoice PDF

```csharp
private byte[] GenerateInvoicePdf(string bookingId, BookingData booking)
{
    var pdfBytes = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1, PageUnit.Centimetre);
            
            // Logo and header
            page.Header()
                .Height(1.5, PageUnit.Centimetre)
                .Row(row =>
                {
                    row.RelativeColumn(2)
                        .Text("LOGO")
                        .Bold()
                        .FontSize(18);
                    
                    row.RelativeColumn()
                        .AlignRight()
                        .Column(col =>
                        {
                            col.Item().Text("Your Property").Bold();
                            col.Item().Text("Phone: +27 123 456 7890");
                            col.Item().Text("Email: info@property.local");
                        });
                });
            
            // Title
            page.Content()
                .Padding(0.5, PageUnit.Centimetre)
                .Column(column =>
                {
                    column.Spacing(0.3, PageUnit.Centimetre);
                    
                    column.Item()
                        .Text($"INVOICE #{bookingId}")
                        .FontSize(16)
                        .Bold();
                    
                    column.Item()
                        .Row(row =>
                        {
                            row.RelativeColumn()
                                .Column(col =>
                                {
                                    col.Item().Text("BILL TO:").Bold();
                                    col.Item().Text(booking.GuestName);
                                    col.Item().Text(booking.GuestEmail);
                                });
                            
                            row.RelativeColumn()
                                .AlignRight()
                                .Column(col =>
                                {
                                    col.Item().Text($"Check-In: {booking.CheckInDate:yyyy-MM-dd}");
                                    col.Item().Text($"Check-Out: {booking.CheckOutDate:yyyy-MM-dd}");
                                    col.Item().Text($"Room: {booking.RoomNumber}");
                                });
                        });
                    
                    // Items table
                    column.Item()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });
                            
                            table.Header(h =>
                            {
                                h.Cell().Text("Description").Bold();
                                h.Cell().AlignRight().Text("Qty").Bold();
                                h.Cell().AlignRight().Text("Amount").Bold();
                            });
                            
                            table.Cell().Text($"Room ({booking.RoomType})");
                            table.Cell().AlignRight().Text(booking.Nights.ToString());
                            table.Cell().AlignRight().Text($"${booking.RoomRate * booking.Nights:F2}");
                            
                            table.Cell().Text("Taxes");
                            table.Cell().AlignRight().Text("1");
                            table.Cell().AlignRight().Text($"${booking.Taxes:F2}");
                            
                            table.Cell().Text("").Bold();
                            table.Cell().AlignRight().Text("TOTAL:").Bold();
                            table.Cell().AlignRight().Text($"${booking.Total:F2}").Bold();
                        });
                    
                    column.Item()
                        .Text("Thank you for your booking!")
                        .AlignCenter()
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken2);
                });
            
            page.Footer()
                .AlignCenter()
                .Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);
        });
    }).GeneratePdf();
    
    return pdfBytes;
}
```

## Stream to Client

### Download PDF

```csharp
return Results.File(
    pdfBytes,
    contentType: "application/pdf",
    fileDownloadName: "Invoice_BOOK001.pdf");
```

### Inline Display

```csharp
return Results.File(
    pdfBytes,
    contentType: "application/pdf",
    fileDownloadName: "Invoice_BOOK001.pdf",
    enableRangeProcessing: true);  // Allows browser viewing
```

## Integration Checklist

- [x] ReportService created with 6 report types
- [x] Service registered in Program.cs
- [ ] Install QuestPDF NuGet package
- [ ] Configure QuestPDF license
- [ ] Implement PDF generation for each report type
- [ ] Integrate database queries to fetch real data
- [ ] Create report endpoints (GET /api/reports/...)
- [ ] Add authorization/tenant validation
- [ ] Test all report types
- [ ] Add to admin dashboard
- [ ] Monitor PDF generation performance
- [ ] Archive generated reports (optional)

## Performance Tips

1. **Caching:** Cache frequently requested reports
2. **Async Generation:** Generate in background for large reports
3. **Streaming:** Stream large PDFs directly to client
4. **Compression:** GZip compress PDF response
5. **Memory:** Dispose reports after generation

Example with compression:

```csharp
var pdfBytes = await reportService.GenerateReportAsync(request);

using (var originalMemory = new MemoryStream(pdfBytes))
using (var compressedMemory = new MemoryStream())
{
    using (var gzip = new GZipStream(compressedMemory, CompressionMode.Compress))
    {
        originalMemory.CopyTo(gzip);
    }
    
    return Results.File(
        compressedMemory.ToArray(),
        "application/gzip",
        $"{result.FileName}.gz",
        enableRangeProcessing: true);
}
```

## Troubleshooting

| Issue | Solution |
|---|---|
| "License key invalid" | Verify license key, check QuestPDF website |
| "PDF is blank" | Check if content was added to page |
| "Out of memory" | Generate reports in batches, stream to file |
| "Font not found" | QuestPDF uses system fonts, check availability |
| "Performance slow" | Profile PDF generation, optimize queries |

