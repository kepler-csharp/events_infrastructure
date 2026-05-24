using ApiGeneral.AuthApi.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace ApiGeneral.AuthApi.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── Public Methods ────────────────────────────────────────────────────────

    public async Task SendTicketEmailAsync(
        string toEmail,
        string toName,
        TicketEmailData data
    )
    {
        var message = new MimeMessage();
        message.From.Add(
            new MailboxAddress("Tickets", _config["Email:Username"])
        );
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"🎟️ Tu entrada para {data.EventName} - #{data.TicketId}";

        var builder = new BodyBuilder();

        string qrCid = $"qr_{data.TicketId}@tickets";
        bool hasInlineQr = false;

        if (!string.IsNullOrWhiteSpace(data.QrCodeBase64))
        {
            var qrBytes = Convert.FromBase64String(data.QrCodeBase64);
            var linkedResource = builder.LinkedResources.Add(
                "qr.png",
                qrBytes,
                new ContentType("image", "png")
            );
            linkedResource.ContentId = qrCid;
            linkedResource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            hasInlineQr = true;
        }

        builder.HtmlBody = BuildTicketHtml(toName, data, qrCid, hasInlineQr);
        message.Body = builder.ToMessageBody();

        await SendAsync(message);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName)
    {
        var message = new MimeMessage();
        message.From.Add(
            new MailboxAddress("Tickets", _config["Email:Username"])
        );
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "¡Bienvenido a Tickets - app! 🎉";

        message.Body = new TextPart(TextFormat.Html)
        {
            Text = BuildWelcomeHtml(toName)
        };

        await SendAsync(message);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task SendAsync(MimeMessage message)
    {
        var host     = _config["Email:Host"]     ?? throw new InvalidOperationException("Email:Host no configurado");
        var port     = int.Parse(_config["Email:Port"] ?? "587");
        var username = _config["Email:Username"] ?? throw new InvalidOperationException("Email:Username no configurado");
        var password = _config["Email:Password"] ?? throw new InvalidOperationException("Email:Password no configurado");

        _logger.LogInformation("Enviando correo a {To} via {Host}:{Port}", message.To, host, port);

        using var client = new SmtpClient();

        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Correo enviado exitosamente a {To}", message.To);
    }

    private static string BuildTicketHtml(
        string toName,
        TicketEmailData data,
        string qrCid,
        bool hasInlineQr
    )
    {
        var qrSection = hasInlineQr
            ? $@"<img src=""cid:{qrCid}"" alt=""QR Ticket #{data.TicketId}"" style=""width:200px;height:200px;display:block;margin:0 auto;border:4px solid #FD7B41;border-radius:8px;"" />"
            : (data.QrImageUrl != null
                ? $@"<img src=""{data.QrImageUrl}"" alt=""QR Ticket #{data.TicketId}"" style=""width:200px;height:200px;display:block;margin:0 auto;border:4px solid #FD7B41;border-radius:8px;"" />"
                : "<p style=\"color:#3C4044;text-align:center;\">QR disponible en tu cuenta</p>");

        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <title>Tu Ticket - Ticket-app</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">

  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f4;padding:32px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">

          <!-- Header -->
          <tr>
            <td style=""background-color:#FD7B41;padding:32px 40px;text-align:center;"">
              <h1 style=""margin:0;color:#ffffff;font-size:28px;font-weight:bold;letter-spacing:1px;"">🎟️ Tickets</h1>
              <p style=""margin:8px 0 0;color:#ffffff;font-size:15px;opacity:0.9;"">Tu entrada está lista</p>
            </td>
          </tr>

          <!-- Greeting -->
          <tr>
            <td style=""padding:32px 40px 16px;"">
              <p style=""margin:0;color:#3C4044;font-size:18px;"">Hola, <strong>{System.Net.WebUtility.HtmlEncode(toName)}</strong> 👋</p>
              <p style=""margin:12px 0 0;color:#3C4044;font-size:15px;line-height:1.6;"">
                Tu pago fue procesado con éxito. Aquí tienes tu entrada para el evento.
                Presenta el código QR en la puerta.
              </p>
            </td>
          </tr>

          <!-- Event Card -->
          <tr>
            <td style=""padding:16px 40px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                style=""background-color:#DDDCDB;border-radius:10px;overflow:hidden;"">
                <tr>
                  <td colspan=""2"" style=""background-color:#3C4044;padding:14px 20px;"">
                    <p style=""margin:0;color:#FD7B41;font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;"">Evento</p>
                    <p style=""margin:4px 0 0;color:#ffffff;font-size:20px;font-weight:bold;"">{System.Net.WebUtility.HtmlEncode(data.EventName)}</p>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:20px;"" width=""50%"" valign=""top"">
                    {DetailRow("📍 Lugar",  System.Net.WebUtility.HtmlEncode(data.VenueName))}
                    {DetailRow("🏙️ Ciudad", System.Net.WebUtility.HtmlEncode(data.VenueCity))}
                    {DetailRow("📅 Fecha",  data.ShowtimeStart.ToString("dddd, dd MMM yyyy", new System.Globalization.CultureInfo("es-ES")))}
                    {DetailRow("⏰ Hora",   data.ShowtimeStart.ToString("hh:mm tt"))}
                  </td>
                  <td style=""padding:20px;"" width=""50%"" valign=""top"">
                    {DetailRow("💺 Asiento",     System.Net.WebUtility.HtmlEncode(data.SeatLabel))}
                    {DetailRow("🎫 Ticket #",    data.TicketId.ToString())}
                    {DetailRow("💰 Precio",      $"${data.PricePaid:F2}")}
                    {DetailRow("🔖 Transacción", System.Net.WebUtility.HtmlEncode(data.TransactionId))}
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- QR Section -->
          <tr>
            <td style=""padding:24px 40px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                style=""border:2px dashed #EDBF9B;border-radius:10px;padding:24px;"">
                <tr>
                  <td align=""center"">
                    <p style=""margin:0 0 16px;color:#3C4044;font-size:14px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;"">
                      Código QR de acceso
                    </p>
                    {qrSection}
                    <p style=""margin:16px 0 0;color:#3C4044;font-size:12px;opacity:0.7;"">
                      Ticket #{data.TicketId} · Válido para una sola entrada
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Address -->
          <tr>
            <td style=""padding:0 40px 24px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                style=""background-color:#EDBF9B;border-radius:8px;padding:14px 20px;"">
                <tr>
                  <td>
                    <p style=""margin:0;color:#3C4044;font-size:13px;"">
                      📍 <strong>Dirección del evento:</strong> {System.Net.WebUtility.HtmlEncode(data.VenueAddress)}, {System.Net.WebUtility.HtmlEncode(data.VenueCity)}
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background-color:#3C4044;padding:24px 40px;text-align:center;"">
              <p style=""margin:0;color:#DDDCDB;font-size:13px;line-height:1.6;"">
                Este correo fue enviado automáticamente por <strong style=""color:#FD7B41;"">Tickets - app</strong>.<br/>
                Si tienes dudas, responde este correo o visita nuestro sitio web.
              </p>
              <p style=""margin:12px 0 0;color:#DDDCDB;font-size:11px;opacity:0.6;"">© {DateTime.UtcNow.Year} Tickets · Todos los derechos reservados</p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>

</body>
</html>";
    }

    private static string DetailRow(string label, string value) =>
        $@"<p style=""margin:0 0 10px;"">
             <span style=""color:#FD7B41;font-size:12px;font-weight:bold;"">{label}</span><br/>
             <span style=""color:#3C4044;font-size:15px;"">{value}</span>
           </p>";

    private static string BuildWelcomeHtml(string toName) => $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""UTF-8""/><title>Bienvenido</title></head>
<body style=""margin:0;padding:0;background:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:32px 0;"">
    <tr>
      <td align=""center"">
        <table width=""580"" cellpadding=""0"" cellspacing=""0""
          style=""background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
          <tr>
            <td style=""background:#FD7B41;padding:32px 40px;text-align:center;"">
              <h1 style=""margin:0;color:#fff;font-size:26px;"">🎟️ Tickets</h1>
            </td>
          </tr>
          <tr>
            <td style=""padding:36px 40px;"">
              <h2 style=""margin:0 0 16px;color:#3C4044;font-size:22px;"">¡Bienvenido, {System.Net.WebUtility.HtmlEncode(toName)}! 🎉</h2>
              <p style=""margin:0;color:#3C4044;font-size:15px;line-height:1.7;"">
                Tu cuenta ha sido creada exitosamente en <strong>Tickets</strong>.<br/>
                Ya puedes explorar eventos, reservar asientos y obtener tus entradas digitales.
              </p>
              <table style=""margin:28px 0 0;"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td style=""background:#FD7B41;border-radius:6px;padding:12px 28px;"">
                    <span style=""color:#fff;font-size:15px;font-weight:bold;"">¡Explorar eventos!</span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td style=""background:#3C4044;padding:20px 40px;text-align:center;"">
              <p style=""margin:0;color:#DDDCDB;font-size:12px;"">© {DateTime.UtcNow.Year} Tickets</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
}
