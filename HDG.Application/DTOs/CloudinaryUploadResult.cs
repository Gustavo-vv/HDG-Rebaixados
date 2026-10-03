namespace HDG.Application.DTOs;

public class CloudinaryUploadResult
{
    public bool Sucesso { get; set; }
    public string? Url { get; set; }
    public string? PublicId { get; set; }
    public string? MensagemErro { get; set; }
}
