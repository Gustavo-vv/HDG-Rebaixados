using AutoMapper;
using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Entidades;
using HDG.Domain.Interfaces.Repositories;

namespace HDG.Application.Servicos.Implementacoes;

public class AvaliacaoService : IAvaliacaoService
{
    private readonly IAvaliacaoRepository _avaliacaoRepository;
    private readonly IPecaRepository _pecaRepository;
    private readonly IMapper _mapper;

    public AvaliacaoService(IAvaliacaoRepository avaliacaoRepository, IPecaRepository pecaRepository, IMapper mapper)
    {
        _avaliacaoRepository = avaliacaoRepository;
        _pecaRepository = pecaRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AvaliacaoDto>> ObterPorPecaAsync(int pecaId)
    {
        var avaliacoes = await _avaliacaoRepository.ObterPorPecaAsync(pecaId, apenasAprovadas: true);
        return _mapper.Map<IEnumerable<AvaliacaoDto>>(avaliacoes);
    }

    public async Task<IEnumerable<AvaliacaoDto>> ObterTodasParaModeracaoAsync()
    {
        var avaliacoes = await _avaliacaoRepository.ObterTodasAsync(apenasAprovadas: null);
        return _mapper.Map<IEnumerable<AvaliacaoDto>>(avaliacoes);
    }

    public async Task<AvaliacaoDto> CriarAsync(CriarAvaliacaoDto dto)
    {
        if (dto.Nota < 1 || dto.Nota > 5)
            throw new ArgumentException("A nota deve ser entre 1 e 5.");

        if (string.IsNullOrWhiteSpace(dto.NomeCliente))
            throw new ArgumentException("O nome do cliente é obrigatório.");

        var existePeca = await _pecaRepository.ExisteAsync(dto.PecaId);
        if (!existePeca)
            throw new KeyNotFoundException($"Peça com id {dto.PecaId} não encontrada.");

        var avaliacao = new Avaliacao
        {
            PecaId = dto.PecaId,
            NomeCliente = dto.NomeCliente.Trim(),
            Nota = dto.Nota,
            Comentario = dto.Comentario?.Trim() ?? string.Empty,
            Aprovada = false, // Moderação necessária
            DataCriacao = DateTime.UtcNow
        };

        var criada = await _avaliacaoRepository.AdicionarAsync(avaliacao);
        return _mapper.Map<AvaliacaoDto>(criada);
    }

    public async Task AprovarAsync(int id)
    {
        await _avaliacaoRepository.AprovarAsync(id);
    }

    public async Task ExcluirAsync(int id)
    {
        await _avaliacaoRepository.RemoverAsync(id);
    }
}
