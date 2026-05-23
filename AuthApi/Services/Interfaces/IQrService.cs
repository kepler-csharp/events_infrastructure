namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IQrService
{
    /// <summary>Genera un QR en PNG y lo devuelve como bytes.</summary>
    byte[] GenerateQrPng(string content, int pixelsPerModule = 10);
}
