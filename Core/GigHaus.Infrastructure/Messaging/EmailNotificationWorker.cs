using System.Text;
using System.Text.Json;
using GigHaus.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GigHaus.Infrastructure.Messaging;

/// <summary>
/// Background worker that consumes reservation events from RabbitMQ
/// and sends notification emails via <see cref="IEmailService"/>.
/// </summary>
public class EmailNotificationWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailNotificationWorker> _logger;

    private const string QueueName = "gighaus.notifications.email";

    public EmailNotificationWorker(
        IConnection connection,
        IServiceProvider services,
        ILogger<EmailNotificationWorker> logger)
    {
        _connection = connection;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: RabbitMqEventBus.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Bind to all reservation events
        await channel.QueueBindAsync(QueueName, RabbitMqEventBus.ExchangeName, "reservation.#", cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, RabbitMqEventBus.ExchangeName, "payment.#", cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var type = ea.BasicProperties?.Type;

                await HandleMessageAsync(type, body, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification message");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Keep alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(string? messageType, string body, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        switch (messageType)
        {
            case nameof(ReservationConfirmedEvent):
                var confirmed = JsonSerializer.Deserialize<ReservationConfirmedEvent>(body);
                if (confirmed is not null)
                {
                    await emailService.SendEmailAsync(new EmailMessage(
                        To: confirmed.AttendeeEmail,
                        Subject: "Your reservation is confirmed!",
                        HtmlBody: $"""
                            <h2>Reservation Confirmed</h2>
                            <p>Hi {confirmed.AttendeeName},</p>
                            <p>Your reservation has been confirmed. We look forward to seeing you!</p>
                            <p><small>This is an automated message from GigHaus.</small></p>
                            """
                    ), ct);
                    _logger.LogInformation("Sent confirmation email to {Email}", confirmed.AttendeeEmail);
                }
                break;

            case nameof(ReservationCancelledEvent):
                var cancelled = JsonSerializer.Deserialize<ReservationCancelledEvent>(body);
                if (cancelled is not null)
                {
                    await emailService.SendEmailAsync(new EmailMessage(
                        To: cancelled.AttendeeEmail,
                        Subject: "Your reservation has been cancelled",
                        HtmlBody: $"""
                            <h2>Reservation Cancelled</h2>
                            <p>Hi {cancelled.AttendeeName},</p>
                            <p>Your reservation has been cancelled.{(cancelled.Reason is not null ? $" Reason: {cancelled.Reason}" : "")}</p>
                            """
                    ), ct);
                    _logger.LogInformation("Sent cancellation email to {Email}", cancelled.AttendeeEmail);
                }
                break;

            case nameof(PaymentReceivedEvent):
                var payment = JsonSerializer.Deserialize<PaymentReceivedEvent>(body);
                if (payment is not null)
                {
                    _logger.LogInformation("Payment received: {PaymentId}, Gross={Gross}, Net={Net}",
                        payment.PaymentId, payment.GrossAmount, payment.NetAmount);
                }
                break;

            default:
                _logger.LogDebug("Unhandled message type: {Type}", messageType);
                break;
        }
    }
}

