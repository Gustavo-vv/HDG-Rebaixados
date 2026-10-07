using System.Collections.Concurrent;
using AutoMapper;
using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Entidades;
using HDG.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;

namespace HDG.Application.Servicos.Implementacoes;

public class AvaliacaoService : IAvaliacaoService
{
    private readonly IAvaliacaoRepository _avaliacaoRepository;
    private readonly IPecaRepository _pecaRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    // Cache thread-safe em memória para Rate Limiting (Máx 5 avaliações por hora por usuário)
    private static readonly ConcurrentDictionary<string, List<DateTime>> _rateLimitStore = new();

    public AvaliacaoService(
        IAvaliacaoRepository avaliacaoRepository,
        IPecaRepository pecaRepository,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _avaliacaoRepository = avaliacaoRepository;
        _pecaRepository = pecaRepository;
        _userManager = userManager;
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

    public Task<AvaliacaoDto> CriarAsync(CriarAvaliacaoDto dto)
    {
        return CriarParaUsuarioAsync(dto, dto.NomeCliente, null);
    }

    public async Task<AvaliacaoDto> CriarParaUsuarioAsync(CriarAvaliacaoDto dto, string nomeCliente, string? userId)
    {
        if (dto.Nota < 1 || dto.Nota > 5)
            throw new ArgumentException("A nota deve ser entre 1 e 5 estrelas.");

        var nomeFinal = (nomeCliente ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nomeFinal))
            throw new ArgumentException("O nome do cliente é obrigatório.");

        // Validação de Comentário (máximo 1000 caracteres)
        var comentarioFinal = (dto.Comentario ?? string.Empty).Trim();
        if (comentarioFinal.Length > 1000)
            throw new ArgumentException("O comentário pode ter no máximo 1000 caracteres.");

        // Validação de Peça: deve existir e estar ativa
        var peca = await _pecaRepository.ObterPorIdAsync(dto.PecaId, incluirInativas: false);
        if (peca == null || !peca.Ativo)
            throw new KeyNotFoundException($"Peça com id {dto.PecaId} não encontrada ou não está disponível.");

        // Se houver userId identificado, validar status Ativo do usuário
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.Ativo)
                throw new InvalidOperationException("Usuário inativo ou não autorizado para enviar avaliações.");

            // Rate Limit: máximo 5 avaliações por hora por usuário
            ValidarRateLimit(userId);
        }

        // Bloqueio de duplicata: mesmo texto de comentário, mesma peça e mesmo NomeCliente nas últimas 24h
        var duplicada = await _avaliacaoRepository.ExisteDuplicadaRecenteAsync(dto.PecaId, nomeFinal, comentarioFinal, TimeSpan.FromHours(24));
        if (duplicada)
        {
            throw new InvalidOperationException("Você já enviou esta mesma avaliação para este produto nas últimas 24 horas.");
        }

        var avaliacao = new Avaliacao
        {
            PecaId = dto.PecaId,
            NomeCliente = nomeFinal,
            Nota = dto.Nota,
            Comentario = comentarioFinal,
            Aprovada = false, // Moderação necessária
            DataCriacao = DateTime.UtcNow
        };

        var criada = await _avaliacaoRepository.AdicionarAsync(avaliacao);

        // Se passar com sucesso e tiver userId, registrar carimbo para o rate limit
        if (!string.IsNullOrEmpty(userId))
        {
            RegistrarEnvioRateLimit(userId);
        }

        return _mapper.Map<AvaliacaoDto>(criada);
    }

    private static void ValidarRateLimit(string userId)
    {
        var umaHoraAtras = DateTime.UtcNow.AddHours(-1);
        if (_rateLimitStore.TryGetValue(userId, out var timestamps))
        {
            lock (timestamps)
            {
                timestamps.RemoveAll(t => t < umaHoraAtras);
                if (timestamps.Count >= 5)
                {
                    throw new InvalidOperationException("Limite de avaliações atingido (máximo 5 avaliações por hora). Por favor, aguarde antes de enviar novamente.");
                }
            }
        }
    }

    private static void RegistrarEnvioRateLimit(string userId)
    {
        var lista = _rateLimitStore.GetOrAdd(userId, _ => new List<DateTime>());
        lock (lista)
        {
            lista.Add(DateTime.UtcNow);
        }
    }

    public async Task AprovarAsync(int id)
    {
        await _avaliacaoRepository.AprovarAsync(id);
    }

    public async Task DesaprovarAsync(int id)
    {
        await _avaliacaoRepository.DesaprovarAsync(id);
    }

    public async Task ExcluirAsync(int id)
    {
        await _avaliacaoRepository.RemoverAsync(id);
    }
}
