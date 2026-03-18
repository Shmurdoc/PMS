using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class FinancialEndpoints
{
    public static void MapFinancialEndpoints(this WebApplication app)
    {
        // ═══════════════════════════════════════════════════════════════
        //  FOLIO ENDPOINTS
        // ═══════════════════════════════════════════════════════════════
        var folioGroup = app.MapGroup("/api/folios")
            .WithTags("Folios")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        folioGroup.MapGet("/booking/{bookingId:guid}", async (Guid bookingId, IUnitOfWork uow) =>
        {
            var folio = await uow.Folios.GetByBookingAsync(bookingId);
            return folio is null ? Results.NotFound() : Results.Ok(folio);
        })
        .WithName("GetFolioByBooking").WithOpenApi();

        folioGroup.MapGet("/open/{propertyId:guid}", async (Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Folios
                .Where(f => f.PropertyId == propertyId && f.Status == FolioStatus.Open)
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id, f.FolioNumber, f.BookingId, f.GuestId,
                    f.TotalCharges, f.TotalPayments, f.Balance, f.Status
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetOpenFolios").WithOpenApi();

        folioGroup.MapPost("/", async (CreateFolioRequest req, IUnitOfWork uow) =>
        {
            var folioNumber = $"F-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            var folio = Folio.Create(req.PropertyId, req.BookingId, req.GuestId, folioNumber);
            await uow.Folios.AddAsync(folio);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/folios/{folio.Id}", new { folio.Id, folio.FolioNumber });
        })
        .WithName("CreateFolio").WithOpenApi();

        folioGroup.MapPost("/{folioId:guid}/charges", async (Guid folioId, AddChargeRequest req, IUnitOfWork uow) =>
        {
            var folio = await uow.Folios.GetWithLineItemsAsync(folioId);
            if (folio is null) return Results.NotFound();
            var item = folio.AddCharge(req.Description, req.Amount, req.Category, req.Quantity);
            await uow.SaveChangesAsync();
            return Results.Ok(new { item.Id, item.Description, item.UnitPrice, item.Quantity });
        })
        .WithName("AddCharge").WithOpenApi();

        folioGroup.MapPost("/{folioId:guid}/close", async (Guid folioId, Guid? userId, IUnitOfWork uow) =>
        {
            var folio = await uow.Folios.GetWithLineItemsAsync(folioId);
            if (folio is null) return Results.NotFound();
            folio.Close(userId);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("CloseFolio").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  PAYMENT ENDPOINTS
        // ═══════════════════════════════════════════════════════════════
        var paymentGroup = app.MapGroup("/api/payments")
            .WithTags("Payments")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        paymentGroup.MapGet("/folio/{folioId:guid}", async (Guid folioId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Payments
                .Where(p => p.FolioId == folioId)
                .AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    p.Id, p.Amount, p.Method, p.Status,
                    p.TransactionReference, p.PaymentDate, p.IsRefund
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetPaymentsByFolio").WithOpenApi();

        paymentGroup.MapPost("/", async (RecordPaymentRequest req, IUnitOfWork uow) =>
        {
            var payment = Payment.Create(req.PropertyId, req.FolioId, req.Amount, req.Method, req.TransactionReference, req.BookingId);

            // Also record on the folio
            var folio = await uow.Folios.GetWithLineItemsAsync(req.FolioId);
            if (folio is null) return Results.NotFound("Folio not found");

            folio.RecordPayment(payment);
            await uow.SaveChangesAsync();

            return Results.Created($"/api/payments/{payment.Id}", new { payment.Id, payment.TransactionReference });
        })
        .WithName("RecordPayment").WithOpenApi();

        paymentGroup.MapGet("/revenue/{propertyId:guid}", async (
            Guid propertyId, DateTime from, DateTime to, IUnitOfWork uow) =>
        {
            var total = await uow.Payments.GetTotalRevenueAsync(propertyId, from, to);
            return Results.Ok(new { PropertyId = propertyId, From = from, To = to, TotalRevenue = total });
        })
        .WithName("GetTotalRevenue").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  INVOICE ENDPOINTS
        // ═══════════════════════════════════════════════════════════════
        var invoiceGroup = app.MapGroup("/api/invoices")
            .WithTags("Invoices")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        invoiceGroup.MapGet("/{invoiceNumber}", async (string invoiceNumber, IUnitOfWork uow) =>
        {
            var invoice = await uow.Invoices.GetByNumberAsync(invoiceNumber);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        })
        .WithName("GetInvoiceByNumber").WithOpenApi();

        invoiceGroup.MapGet("/overdue/{propertyId:guid}", async (Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Invoices
                .Where(i => i.PropertyId == propertyId &&
                            i.Status == InvoiceStatus.Issued && i.DueDate < DateTime.UtcNow)
                .AsNoTracking()
                .OrderBy(i => i.DueDate)
                .Select(i => new
                {
                    i.Id, i.InvoiceNumber, i.TotalAmount, i.PaidAmount,
                    i.OutstandingAmount, i.DueDate, i.Status
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetOverdueInvoices").WithOpenApi();

        invoiceGroup.MapPost("/{id:guid}/finalize", async (Guid id, IUnitOfWork uow) =>
        {
            var invoice = await uow.Invoices.GetByIdAsync(id);
            if (invoice is null) return Results.NotFound();
            invoice.FinalizeInvoice();
            uow.Invoices.Update(invoice);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("FinalizeInvoice").WithOpenApi();
    }
}

public record CreateFolioRequest(Guid PropertyId, Guid BookingId, Guid GuestId);
public record AddChargeRequest(string Description, decimal Amount, ChargeCategory Category, int Quantity = 1);
public record RecordPaymentRequest(Guid PropertyId, Guid FolioId, decimal Amount, PaymentMethod Method, string? TransactionReference, Guid? BookingId);
