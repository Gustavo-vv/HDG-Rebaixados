using System.ComponentModel.DataAnnotations;

namespace HDG.Application.DTOs;

public class UsuarioListaDto
{
    public string Id { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DataCadastroUtc { get; set; }
    public DateTime DataCadastroBrasilia { get; set; }
    public bool Ativo { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsAdmin => Roles.Contains("Admin");
}

public class EditarUsuarioDto
{
    [Required]
    public string Id { get; set; } = string.Empty;

    // Apenas para exibição em campo disabled no cliente. NÃO é utilizado nem aceito na persistência de alteração.
    public string? EmailExibicao { get; set; }

    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome completo deve ter entre 3 e 100 caracteres.")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Informe um telefone válido.")]
    [StringLength(20, ErrorMessage = "O telefone não pode exceder 20 caracteres.")]
    public string? Telefone { get; set; }
}

public class AlterarSenhaAdminDto
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve conter no mínimo 8 caracteres.")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "A confirmação de senha é obrigatória.")]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class FiltroUsuariosDto
{
    public string? Busca { get; set; }
    public string? Status { get; set; } // "todos", "ativos", "inativos"
    public string? Role { get; set; }   // "todos", "Admin", "Cliente"
}
