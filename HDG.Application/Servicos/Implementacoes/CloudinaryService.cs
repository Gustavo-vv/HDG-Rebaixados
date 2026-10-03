using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using Microsoft.Extensions.Configuration;

namespace HDG.Application.Servicos.Implementacoes;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly bool _configurado;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (!string.IsNullOrWhiteSpace(cloudName) &&
            !string.IsNullOrWhiteSpace(apiKey) &&
            !string.IsNullOrWhiteSpace(apiSecret) &&
            apiKey != "123456789012345")
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
            _configurado = true;
        }
    }

    public async Task<CloudinaryUploadResult> UploadImagemAsync(Stream streamArquivo, string nomeArquivo, string pasta = "hdg_rebaixados")
    {
        if (!_configurado || _cloudinary == null)
        {
            // Fallback para desenvolvimento quando credenciais não estão configuradas
            var fakeId = $"{pasta}/{Guid.NewGuid()}";
            return new CloudinaryUploadResult
            {
                Sucesso = true,
                PublicId = fakeId,
                Url = "https://images.unsplash.com/photo-1486006920555-c77dce18193b?auto=format&fit=crop&w=1000&q=80"
            };
        }

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(nomeArquivo, streamArquivo),
                Folder = pasta,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                return new CloudinaryUploadResult
                {
                    Sucesso = false,
                    MensagemErro = uploadResult.Error.Message
                };
            }

            return new CloudinaryUploadResult
            {
                Sucesso = true,
                Url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString(),
                PublicId = uploadResult.PublicId
            };
        }
        catch (Exception ex)
        {
            return new CloudinaryUploadResult
            {
                Sucesso = false,
                MensagemErro = ex.Message
            };
        }
    }

    public async Task<bool> DeletarImagemAsync(string publicId)
    {
        if (!_configurado || _cloudinary == null)
            return true;

        try
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }
        catch
        {
            return false;
        }
    }
}