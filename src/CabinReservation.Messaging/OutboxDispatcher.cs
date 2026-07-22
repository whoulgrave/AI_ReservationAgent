using Azure;
using Azure.Communication.Email;
using Azure.Communication.Sms;
using CabinReservation.Integration.Contracts.Outbox;
using Microsoft.Extensions.Options;

namespace CabinReservation.Messaging;

public sealed class OutboxDispatcher(
    IOutboxApiClient outbox,
    SmsClient sms,
    EmailClient email,
    IMessageTemplateRenderer renderer,
    IOptions<MessagingOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.OutboxPollSeconds));
        do
        {
            IReadOnlyList<OutboxLeaseDto> batch = [];
            try
            {
                batch = await outbox.LeaseAsync(20, stoppingToken);
                foreach (var item in batch)
                {
                    try
                    {
                        var text = renderer.Render(item.MessageType, item.PayloadJson);
                        string providerId;
                        if (item.Channel == 1)
                        {
                            if (string.IsNullOrWhiteSpace(item.EmailAddress)) throw new InvalidOperationException("Member email address is missing.");
                            var content = new EmailContent("Cabin reservation notification") { PlainText = text };
                            var message = new EmailMessage(options.Value.EmailSender, item.EmailAddress, content);
                            var operation = await email.SendAsync(WaitUntil.Completed, message, stoppingToken);
                            providerId = operation.Id;
                        }
                        else if (item.Channel == 2)
                        {
                            if (string.IsNullOrWhiteSpace(item.MobileNumber)) throw new InvalidOperationException("Member mobile number is missing.");
                            var result = await sms.SendAsync(options.Value.SmsFromNumber, item.MobileNumber, text,
                                new SmsSendOptions(enableDeliveryReport: true) { Tag = item.Id.ToString("N") }, stoppingToken);
                            if (!result.Value.Successful) throw new InvalidOperationException(result.Value.ErrorMessage);
                            providerId = result.Value.MessageId;
                        }
                        else
                        {
                            throw new NotSupportedException("Outbound telephone confirmations require an explicit call workflow and are not sent by the messaging worker.");
                        }
                        await outbox.CompleteAsync(item.Id, item.LeaseToken, providerId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to dispatch outbox message {MessageId}.", item.Id);
                        await outbox.FailAsync(item.Id, item.LeaseToken, ex.Message, stoppingToken);
                    }
                }
            }
            catch (Exception ex) { logger.LogError(ex, "Outbox lease failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
