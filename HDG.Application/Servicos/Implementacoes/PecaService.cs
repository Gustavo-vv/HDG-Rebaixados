using AutoMapper;
using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Entidades;
using HDG.Domain.Enums;
using HDG.Domain.Interfaces.Repositories;

namespace HDG.Application.Servicos.Implementacoes;

public class PecaService : IPecaService
{
    private readonly IPecaRepository _pecaRepository;
    private readonly IMapper _mapper;

    public PecaService(IPecaRepository pecaRepository, IMapper mapper)
    {
        _pecaRepository = pecaRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PecaDto>> ObterCatalogoAsync(string? busca = null, CategoriaPeca? categoria = null)
    {
        var pecas = await _pecaRepository.ObterTodasAsync(apenasAtivas: true, busca: busca, categoria: categoria);
        return _mapper.Map<IEnumerable<PecaDto>>(pecas);
    }

    public async Task<IEnumerable<PecaDto>> ObterTodasAdminAsync()
    {
        var pecas = await _pecaRepository.ObterTodasAsync(apenasAtivas: false);
        return _mapper.Map<IEnumerable<PecaDto>>(pecas);
    }

    public async Task<PecaDto?> ObterPorIdAsync(int id)
    {
        var peca = await _pecaRepository.ObterPorIdAsync(id, incluirInativas: true);
        if (peca == null) return null;
        return _mapper.Map<PecaDto>(peca);
    }

    public async Task<PecaDto> CriarAsync(CriarPecaDto dto)
    {
        var peca = new Peca
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Preco = dto.Preco,
            Categoria = dto.Categoria,
            Ativo = dto.Ativo,
            DataCriacao = DateTime.UtcNow
        };

        if (dto.ImagensUrls != null && dto.ImagensUrls.Any())
        {
            int ordem = 1;
            foreach (var url in dto.ImagensUrls)
            {
                peca.Imagens.Add(new PecaImagem
                {
                    Url = url,
                    Principal = ordem == 1,
                    Ordem = ordem++,
                    DataEnvio = DateTime.UtcNow
                });
            }
        }

        var criado = await _pecaRepository.AdicionarAsync(peca);
        return _mapper.Map<PecaDto>(criado);
    }

    public async Task AtualizarAsync(AtualizarPecaDto dto)
    {
        var peca = await _pecaRepository.ObterPorIdAsync(dto.Id, incluirInativas: true);
        if (peca == null)
            throw new KeyNotFoundException($"Peça com id {dto.Id} não encontrada.");

        peca.Nome = dto.Nome;
        peca.Descricao = dto.Descricao;
        peca.Preco = dto.Preco;
        peca.Categoria = dto.Categoria;
        peca.Ativo = dto.Ativo;
        peca.DataAtualizacao = DateTime.UtcNow;

        if (dto.ImagensParaRemoverIds != null && dto.ImagensParaRemoverIds.Any())
        {
            foreach (var imgId in dto.ImagensParaRemoverIds)
            {
                await _pecaRepository.RemoverImagemAsync(imgId);
                var imgNaLista = peca.Imagens.FirstOrDefault(i => i.Id == imgId);
                if (imgNaLista != null) peca.Imagens.Remove(imgNaLista);
            }
        }

        if (dto.NovasImagensUrls != null && dto.NovasImagensUrls.Any())
        {
            // Se uma nova imagem foi adicionada no topo ou como principal, desmarca anteriores se solicitado
            bool precisaPrincipal = !peca.Imagens.Any(i => i.Principal);
            int ordem = peca.Imagens.Count + 1;
            
            foreach (var url in dto.NovasImagensUrls)
            {
                var novaImg = new PecaImagem
                {
                    PecaId = peca.Id,
                    Url = url,
                    Principal = precisaPrincipal,
                    Ordem = ordem++,
                    DataEnvio = DateTime.UtcNow
                };
                precisaPrincipal = false;
                await _pecaRepository.AdicionarImagemAsync(novaImg);
                peca.Imagens.Add(novaImg);
            }
        }

        await _pecaRepository.AtualizarAsync(peca);
    }

    public async Task DefinirImagemPrincipalAsync(int pecaId, int imagemId)
    {
        var peca = await _pecaRepository.ObterPorIdAsync(pecaId, incluirInativas: true);
        if (peca == null) return;

        foreach (var img in peca.Imagens)
        {
            img.Principal = (img.Id == imagemId);
        }
        await _pecaRepository.AtualizarAsync(peca);
    }

    public async Task AlternarStatusAsync(int id)
    {
        var peca = await _pecaRepository.ObterPorIdAsync(id, incluirInativas: true);
        if (peca == null)
            throw new KeyNotFoundException($"Peça com id {id} não encontrada.");

        peca.Ativo = !peca.Ativo;
        peca.DataAtualizacao = DateTime.UtcNow;
        await _pecaRepository.AtualizarAsync(peca);
    }

    public async Task ExcluirAsync(int id)
    {
        await _pecaRepository.RemoverAsync(id);
    }

    public async Task AdicionarImagemAsync(int pecaId, string url, string? publicId, bool principal)
    {
        var peca = await _pecaRepository.ObterPorIdAsync(pecaId, incluirInativas: true);
        if (peca == null)
            throw new KeyNotFoundException($"Peça com id {pecaId} não encontrada.");

        if (principal && peca.Imagens.Any())
        {
            foreach (var i in peca.Imagens) i.Principal = false;
            await _pecaRepository.AtualizarAsync(peca);
        }

        var imagem = new PecaImagem
        {
            PecaId = pecaId,
            Url = url,
            PublicId = publicId,
            Principal = principal || !peca.Imagens.Any(),
            Ordem = peca.Imagens.Count + 1,
            DataEnvio = DateTime.UtcNow
        };

        await _pecaRepository.AdicionarImagemAsync(imagem);
    }

    public async Task RemoverImagemAsync(int imagemId)
    {
        await _pecaRepository.RemoverImagemAsync(imagemId);
    }
}