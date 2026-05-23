using ApiGeneral.AuthApi.Services.Interfaces;
using QRCoder;

namespace ApiGeneral.AuthApi.Services;

public class QrService : IQrService
{
    /// <inheritdoc />
    public byte[] GenerateQrPng(string content, int pixelsPerModule = 10)
    {
        using var qrGenerator  = new QRCodeGenerator();
        using var qrCodeData   = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode       = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }
}
