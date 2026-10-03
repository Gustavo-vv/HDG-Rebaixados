namespace HDG.Domain.Entidades;

public class Avaliacao
{
    public int Id { get; set; }
    public int PecaId { get; set; }
    public Peca Peca { get; set; } = null!;
    
    public string NomeCliente { get; set; } = string.Empty;
    public int Nota { get; set; } // 1 a 5
    public string Comentario { get; set; } = string.Empty;
    public bool Aprovada { get; set; } = false; // Moderação pelo admin
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
