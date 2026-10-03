namespace HDG.Domain.Entidades;

public class PecaImagem
{
    public int Id { get; set; }
    public int PecaId { get; set; }
    public Peca Peca { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string? PublicId { get; set; } // Identificador no Cloudinary
    public bool Principal { get; set; }
    public int Ordem { get; set; }
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
}
