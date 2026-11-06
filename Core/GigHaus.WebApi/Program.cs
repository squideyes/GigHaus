using FluentValidation;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure;
using GigHaus.Infrastructure.Data;
using GigHaus.Infrastructure.Messaging;
using GigHaus.Infrastructure.Repositories;
using GigHaus.Infrastructure.Services;
using GigHaus.WebApi;
using GigHaus.WebApi.Auth;
using GigHaus.WebApi.Hubs;
using GigHaus.WebApi.Middleware;
using GigHaus.WebApi.Telemetry;
using Microsoft.AspNetCore.SignalR;
using HostEntity = GigHaus.Domain.Entities.Host;

DapperConfig.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Npgsql data source from Aspire
builder.AddNpgsqlDataSource("gighaus");

// RabbitMQ connection from Aspire
builder.AddRabbitMQClient("messaging");

// Redis output caching from Aspire
builder.AddRedisOutputCache("redis");

// Dapper repositories + services
builder.Services.AddGigHausInfrastructure();
builder.Services.AddGigHausPayments(opts => opts.IsDevMode = true);
builder.Services.AddGigHausMessaging();
builder.Services.AddGigHausEmail();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// SignalR
builder.Services.AddSignalR();

// Authentication & Authorization
builder.Services.AddGigHausAuth(builder.Configuration);

// Custom business metrics
builder.Services.AddSingleton<GigHausMetrics>();

// Error handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseOutputCache();
app.UseHttpsRedirection();

// Auth middleware
app.UseAuthentication();
app.UseAuthorization();

// ── SignalR ──────────────────────────────────────────────────
app.MapHub<GigHausHub>("/hubs/gighaus");

// ── Auth (dev-mode token endpoint) ─────────────────────────
if (app.Environment.IsDevelopment())
{
    var auth = app.MapGroup("/api/auth").WithTags("Auth");

    auth.MapPost("/dev-token", (DevTokenRequest request, IConfiguration config) =>
    {
        var token = AuthConfiguration.GenerateDevToken(
            config, Guid.NewGuid().ToString(), request.Email, request.Name, request.Roles);
        return Results.Ok(new { token });
    });
}

// ── Artists ──────────────────────────────────────────────────
var artists = app.MapGroup("/api/artists").WithTags("Artists");

artists.MapGet("/", async (IArtistRepository repo) =>
    await repo.GetAllAsync())
    .RequireAuthorization(AuthConfiguration.PolicyAdmin);

artists.MapGet("/{id:guid}", async (Guid id, IArtistRepository repo) =>
    await repo.GetByIdAsync(id) is { } artist
        ? Results.Ok(artist)
        : Results.NotFound())
    .RequireAuthorization(AuthConfiguration.PolicyAdmin);

artists.MapPost("/", async (Artist artist, IArtistRepository repo) =>
{
    var created = await repo.CreateAsync(artist);
    return Results.Created($"/api/artists/{created.Id}", created);
}).AddEndpointFilter<ValidationFilter<Artist>>()
  .RequireAuthorization(AuthConfiguration.PolicyAdmin);

artists.MapPut("/{id:guid}", async (Guid id, Artist input, IArtistRepository repo) =>
{
    var updated = await repo.UpdateAsync(id, input);
    return updated is not null ? Results.Ok(updated) : Results.NotFound();
}).AddEndpointFilter<ValidationFilter<Artist>>()
  .RequireAuthorization(AuthConfiguration.PolicyAdmin);

artists.MapDelete("/{id:guid}", async (Guid id, IArtistRepository repo) =>
{
    return await repo.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(AuthConfiguration.PolicyAdmin);

// ── Hosts ────────────────────────────────────────────────────
var hosts = app.MapGroup("/api/hosts").WithTags("Hosts");

// Hosts are private homes, so their details never leave this API unauthenticated.
hosts.MapGet("/", async (IHostRepository repo) =>
    await repo.GetAllAsync())
    .RequireAuthorization(AuthConfiguration.PolicyAdmin);

hosts.MapGet("/{id:guid}", async (Guid id, IHostRepository repo) =>
    await repo.GetByIdAsync(id) is { } host
        ? Results.Ok(host)
        : Results.NotFound())
    .RequireAuthorization(AuthConfiguration.PolicyAdmin);

hosts.MapPost("/", async (HostEntity host, IHostRepository repo) =>
{
    var created = await repo.CreateAsync(host);
    return Results.Created($"/api/hosts/{created.Id}", created);
}).AddEndpointFilter<ValidationFilter<HostEntity>>()
  .RequireAuthorization(AuthConfiguration.PolicyAdmin);

hosts.MapDelete("/{id:guid}", async (Guid id, IHostRepository repo) =>
{
    return await repo.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(AuthConfiguration.PolicyAdmin);

// ── Events ───────────────────────────────────────────────────
var events = app.MapGroup("/api/events").WithTags("Events");

events.MapGet("/", async (IEventRepository repo) =>
    (await repo.GetAllAsync()).Where(PublicEvent.IsVisible).Select(PublicEvent.From))
    .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(15)).Tag("events"));

events.MapGet("/{id:guid}", async (Guid id, IEventRepository repo) =>
    await repo.GetByIdAsync(id) is { } ev && PublicEvent.IsVisible(ev)
        ? Results.Ok(PublicEvent.From(ev))
        : Results.NotFound())
    .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(15)).Tag("events"));

events.MapPost("/", async (Event ev, IEventRepository repo) =>
{
    var created = await repo.CreateAsync(ev);
    return Results.Created($"/api/events/{created.Id}", created);
}).AddEndpointFilter<ValidationFilter<Event>>()
  .RequireAuthorization(AuthConfiguration.PolicyHost);

events.MapDelete("/{id:guid}", async (Guid id, IEventRepository repo) =>
{
    return await repo.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization(AuthConfiguration.PolicyAdmin);

// ── Reservations ─────────────────────────────────────────────
var reservations = app.MapGroup("/api/reservations").WithTags("Reservations");

reservations.MapGet("/event/{eventId:guid}", async (Guid eventId, IReservationRepository repo) =>
    await repo.GetByEventIdAsync(eventId))
    .RequireAuthorization(AuthConfiguration.PolicyHost);

reservations.MapGet("/{id:guid}", async (Guid id, IReservationRepository repo) =>
    await repo.GetByIdAsync(id) is { } res
        ? Results.Ok(res)
        : Results.NotFound())
    .RequireAuthorization(AuthConfiguration.PolicyHost);

reservations.MapPost("/", async (Reservation res, IReservationService svc, IEventBus bus,
    IHubContext<GigHausHub, IGigHausClient> hub, IReservationRepository resRepo, IEventRepository evRepo, GigHausMetrics metrics) =>
{
    var result = await svc.CreateReservationAsync(res);
    if (!result.Success) return Results.BadRequest(new { error = result.Error });

    metrics.ReservationCreated(result.Reservation!.EventId);

    await bus.PublishAsync(RabbitMqEventBus.ExchangeName, "reservation.created",
        new ReservationCreatedEvent(
            result.Reservation!.Id, result.Reservation.EventId,
            result.Reservation.AttendeeName, result.Reservation.AttendeeEmail,
            result.Reservation.NumberOfGuests, DateTime.UtcNow));

    // SignalR: broadcast capacity update
    var ev = await evRepo.GetByIdAsync(result.Reservation.EventId);
    var currentGuests = await resRepo.GetConfirmedGuestCountAsync(result.Reservation.EventId);
    await hub.Clients.Group(GigHausHub.EventGroup(result.Reservation.EventId))
        .CapacityUpdated(new CapacityUpdate(result.Reservation.EventId, currentGuests, ev?.MaxAttendees ?? 0, "Created"));

    return Results.Created($"/api/reservations/{result.Reservation!.Id}", result.Reservation);
}).AddEndpointFilter<ValidationFilter<Reservation>>()
  .RequireAuthorization(AuthConfiguration.PolicyAuthenticated);

reservations.MapPost("/{id:guid}/confirm", async (Guid id, IReservationService svc, IEventBus bus,
    IHubContext<GigHausHub, IGigHausClient> hub, IReservationRepository resRepo, IEventRepository evRepo, GigHausMetrics metrics) =>
{
    var result = await svc.ConfirmReservationAsync(id);
    if (!result.Success) return Results.BadRequest(new { error = result.Error });

    metrics.ReservationConfirmed(result.Reservation!.EventId);

    await bus.PublishAsync(RabbitMqEventBus.ExchangeName, "reservation.confirmed",
        new ReservationConfirmedEvent(
            result.Reservation!.Id, result.Reservation.EventId,
            result.Reservation.AttendeeName, result.Reservation.AttendeeEmail,
            result.Reservation.ConfirmedAt!.Value));

    var ev = await evRepo.GetByIdAsync(result.Reservation.EventId);
    var currentGuests = await resRepo.GetConfirmedGuestCountAsync(result.Reservation.EventId);
    await hub.Clients.Group(GigHausHub.EventGroup(result.Reservation.EventId))
        .CapacityUpdated(new CapacityUpdate(result.Reservation.EventId, currentGuests, ev?.MaxAttendees ?? 0, "Confirmed"));

    return Results.Ok(result.Reservation);
}).RequireAuthorization(AuthConfiguration.PolicyHost);

reservations.MapPost("/{id:guid}/cancel", async (Guid id, CancelRequest? body, IReservationService svc, IEventBus bus,
    IHubContext<GigHausHub, IGigHausClient> hub, IReservationRepository resRepo, IEventRepository evRepo, GigHausMetrics metrics) =>
{
    var result = await svc.CancelReservationAsync(id, body?.Reason);
    if (!result.Success) return Results.BadRequest(new { error = result.Error });

    metrics.ReservationCancelled(result.Reservation!.EventId);

    await bus.PublishAsync(RabbitMqEventBus.ExchangeName, "reservation.cancelled",
        new ReservationCancelledEvent(
            result.Reservation!.Id, result.Reservation.EventId,
            result.Reservation.AttendeeName, result.Reservation.AttendeeEmail,
            result.Reservation.CancellationReason, result.Reservation.CancelledAt!.Value));

    var ev = await evRepo.GetByIdAsync(result.Reservation.EventId);
    var currentGuests = await resRepo.GetConfirmedGuestCountAsync(result.Reservation.EventId);
    await hub.Clients.Group(GigHausHub.EventGroup(result.Reservation.EventId))
        .CapacityUpdated(new CapacityUpdate(result.Reservation.EventId, currentGuests, ev?.MaxAttendees ?? 0, "Cancelled"));

    return Results.Ok(result.Reservation);
}).RequireAuthorization(AuthConfiguration.PolicyAuthenticated);

reservations.MapPost("/{id:guid}/checkin", async (Guid id, IReservationService svc, IEventBus bus,
    IHubContext<GigHausHub, IGigHausClient> hub, IReservationRepository resRepo, IEventRepository evRepo, GigHausMetrics metrics) =>
{
    var result = await svc.CheckInReservationAsync(id);
    if (!result.Success) return Results.BadRequest(new { error = result.Error });

    metrics.CheckIn(result.Reservation!.EventId);

    await bus.PublishAsync(RabbitMqEventBus.ExchangeName, "reservation.checkedin",
        new ReservationCheckedInEvent(
            result.Reservation!.Id, result.Reservation.EventId,
            result.Reservation.AttendeeName, result.Reservation.AttendeeEmail,
            result.Reservation.CheckedInAt!.Value));

    // SignalR: broadcast check-in update
    var allRes = await resRepo.GetByEventIdAsync(result.Reservation.EventId);
    var totalCheckedIn = allRes.Count(r => r.CheckedInAt.HasValue);
    await hub.Clients.Group(GigHausHub.EventGroup(result.Reservation.EventId))
        .CheckInUpdated(new CheckInUpdate(result.Reservation.EventId, result.Reservation.Id,
            result.Reservation.AttendeeName, totalCheckedIn));

    return Results.Ok(result.Reservation);
}).RequireAuthorization(AuthConfiguration.PolicyHost);

// ── Payments ─────────────────────────────────────────────────
var payments = app.MapGroup("/api/payments").WithTags("Payments");

payments.MapPost("/intent", async (CreatePaymentRequest request, IPaymentService svc, IEventBus bus,
    IHubContext<GigHausHub, IGigHausClient> hub, IReservationRepository resRepo, GigHausMetrics metrics) =>
{
    var result = await svc.CreatePaymentIntentAsync(request);
    if (!result.Success) return Results.BadRequest(new { error = result.Error });

    metrics.PaymentProcessed(result.PaymentRecord!.GrossAmount, result.PaymentRecord.Provider ?? "Online");

    await bus.PublishAsync(RabbitMqEventBus.ExchangeName, "payment.received",
        new PaymentReceivedEvent(
            result.PaymentRecord!.Id, result.PaymentRecord.ReservationId,
            result.PaymentRecord.GrossAmount, result.PaymentRecord.NetAmount,
            result.PaymentRecord.Provider ?? "Unknown", DateTime.UtcNow));

    // SignalR: broadcast payment update
    var reservation = await resRepo.GetByIdAsync(result.PaymentRecord.ReservationId);
    if (reservation is not null)
        await hub.Clients.Group(GigHausHub.EventGroup(reservation.EventId))
            .PaymentReceived(new PaymentUpdate(reservation.EventId, result.PaymentRecord.Id,
                result.PaymentRecord.GrossAmount, result.PaymentRecord.Provider ?? "Online"));

    return Results.Ok(new { result.ClientSecret, result.PaymentIntentId, Payment = result.PaymentRecord });
}).AddEndpointFilter<ValidationFilter<CreatePaymentRequest>>()
  .RequireAuthorization(AuthConfiguration.PolicyAuthenticated);

payments.MapPost("/door", async (DoorPaymentRequest request, IPaymentService svc, IEventBus bus,
    IHubContext<GigHausHub, IGigHausClient> hub, IReservationRepository resRepo, GigHausMetrics metrics) =>
{
    var result = await svc.RecordDoorPaymentAsync(request);
    if (!result.Success) return Results.BadRequest(new { error = result.Error });

    metrics.PaymentProcessed(result.PaymentRecord!.GrossAmount, result.PaymentRecord.Provider ?? "Cash");

    await bus.PublishAsync(RabbitMqEventBus.ExchangeName, "payment.received",
        new PaymentReceivedEvent(
            result.PaymentRecord!.Id, result.PaymentRecord.ReservationId,
            result.PaymentRecord.GrossAmount, result.PaymentRecord.NetAmount,
            result.PaymentRecord.Provider ?? "Cash", DateTime.UtcNow));

    var reservation = await resRepo.GetByIdAsync(result.PaymentRecord.ReservationId);
    if (reservation is not null)
        await hub.Clients.Group(GigHausHub.EventGroup(reservation.EventId))
            .PaymentReceived(new PaymentUpdate(reservation.EventId, result.PaymentRecord.Id,
                result.PaymentRecord.GrossAmount, result.PaymentRecord.Provider ?? "Cash"));

    return Results.Ok(result.PaymentRecord);
}).AddEndpointFilter<ValidationFilter<DoorPaymentRequest>>()
  .RequireAuthorization(AuthConfiguration.PolicyHost);

payments.MapGet("/reservation/{reservationId:guid}", async (Guid reservationId, IPaymentRecordRepository repo) =>
    await repo.GetByReservationIdAsync(reservationId))
    .RequireAuthorization(AuthConfiguration.PolicyAuthenticated);

payments.MapGet("/fees", (decimal amount, IPaymentService svc) =>
    Results.Ok(svc.CalculateFees(amount)));

// ── Settlement ───────────────────────────────────────────────
var settlements = app.MapGroup("/api/settlements").WithTags("Settlements");

settlements.MapGet("/event/{eventId:guid}", async (Guid eventId, ISettlementSummaryRepository repo) =>
    await repo.GetByEventIdAsync(eventId) is { } summary
        ? Results.Ok(summary)
        : Results.NotFound())
    .RequireAuthorization(AuthConfiguration.PolicyHost);

app.Run();

// ── Request DTOs ─────────────────────────────────────────────
public record CancelRequest(string? Reason);
