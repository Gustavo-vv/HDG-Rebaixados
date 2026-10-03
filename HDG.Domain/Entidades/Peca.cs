using HDG.Domain.Enums;

namespace HDG.Domain.Entidades;

public class Peca
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public CategoriaPeca Categoria { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }

    // Relacionamentos
    public ICollection<PecaImagem> Imagens { get; set; } = new List<PecaImagem>();
    public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
}
