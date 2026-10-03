namespace HDG.Application.DTOs;

public class AvaliacaoDto
{
    public int Id { get; set; }
    public int PecaId { get; set; }
    public string PecaNome { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public int Nota { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public bool Aprovada { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class CriarAvaliacaoDto
{
    public int PecaId { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public int Nota { get; set; }
    public string Comentario { get; set; } = string.Empty;
}
