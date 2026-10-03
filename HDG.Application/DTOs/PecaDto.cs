using HDG.Domain.Enums;

namespace HDG.Application.DTOs;

public class PecaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public CategoriaPeca Categoria { get; set; }
    public string CategoriaDescricao => Categoria.ToString();
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? ImagemPrincipalUrl { get; set; }
    public List<PecaImagemDto> Imagens { get; set; } = new();
    public double MediaAvaliacoes { get; set; }
    public int TotalAvaliacoes { get; set; }
}

public class PecaImagemDto
{
    public int Id { get; set; }
    public int PecaId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? PublicId { get; set; }
    public bool Principal { get; set; }
    public int Ordem { get; set; }
}

public class CriarPecaDto
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public CategoriaPeca Categoria { get; set; }
    public bool Ativo { get; set; } = true;
    public List<string> ImagensUrls { get; set; } = new();
}

public class AtualizarPecaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public CategoriaPeca Categoria { get; set; }
    public bool Ativo { get; set; }
    public List<string> NovasImagensUrls { get; set; } = new();
    public List<int> ImagensParaRemoverIds { get; set; } = new();
}
