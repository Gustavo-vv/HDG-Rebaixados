using HDG.Application.DTOs;

namespace HDG.Application.Servicos.Services;

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadImagemAsync(Stream streamArquivo, string nomeArquivo, string pasta = "hdg_rebaixados");
    Task<bool> DeletarImagemAsync(string publicId);
}
