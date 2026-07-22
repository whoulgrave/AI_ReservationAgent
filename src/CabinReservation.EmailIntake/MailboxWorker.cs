using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace CabinReservation.EmailIntake;

public sealed class MailboxWorker(GraphServiceClient graph, IHermesEmailClient hermes, IOptions<EmailIntakeOptions> options,
    ILogger<MailboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollSeconds));
        do
        {
            try
            {
                var messages = await graph.Users[options.Value.MailboxAddress].MailFolders["Inbox"].Messages.GetAsync(r =>
                {
                    r.QueryParameters.Filter = "isRead eq false";
                    r.QueryParameters.Top = 20;
                    r.QueryParameters.Select = ["id", "internetMessageId", "subject", "body", "from", "receivedDateTime"];
                    r.QueryParameters.Orderby = ["receivedDateTime asc"];
                }, stoppingToken);
                foreach (var message in messages?.Value ?? [])
                {
                    if (message.Id is null) continue;
                    var sender = message.From?.EmailAddress?.Address ?? "";
                    await hermes.ProcessAsync(sender, message.InternetMessageId ?? message.Id, message.Subject ?? "", message.Body?.Content ?? "", stoppingToken);
                    await graph.Users[options.Value.MailboxAddress].Messages[message.Id].PatchAsync(new Message { IsRead = true }, cancellationToken: stoppingToken);
                }
            }
            catch (Exception ex) { logger.LogError(ex, "Mailbox polling failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
