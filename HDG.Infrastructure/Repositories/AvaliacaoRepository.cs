using HDG.Domain.Entidades;
using HDG.Domain.Interfaces.Repositories;
using HDG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HDG.Infrastructure.Repositories;

public class AvaliacaoRepository : IAvaliacaoRepository
{
    private readonly HdgDbContext _context;

    public AvaliacaoRepository(HdgDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Avaliacao>> ObterPorPecaAsync(int pecaId, bool apenasAprovadas = true)
    {
        var query = _context.Avaliacoes
            .Where(a => a.PecaId == pecaId)
            .AsNoTracking()
            .AsQueryable();

        if (apenasAprovadas)
            query = query.Where(a => a.Aprovada);

        return await query.OrderByDescending(a => a.DataCriacao).ToListAsync();
    }

    public async Task<IEnumerable<Avaliacao>> ObterTodasAsync(bool? apenasAprovadas = null)
    {
        var query = _context.Avaliacoes
            .Include(a => a.Peca)
            .AsNoTracking()
            .AsQueryable();

        if (apenasAprovadas.HasValue)
            query = query.Where(a => a.Aprovada == apenasAprovadas.Value);

        return await query.OrderByDescending(a => a.DataCriacao).ToListAsync();
    }

    public async Task<Avaliacao?> ObterPorIdAsync(int id)
    {
        return await _context.Avaliacoes.Include(a => a.Peca).FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Avaliacao> AdicionarAsync(Avaliacao avaliacao)
    {
        await _context.Avaliacoes.AddAsync(avaliacao);
        await _context.SaveChangesAsync();
        return avaliacao;
    }

    public async Task AprovarAsync(int id)
    {
        var avaliacao = await _context.Avaliacoes.FindAsync(id);
        if (avaliacao != null)
        {
            avaliacao.Aprovada = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DesaprovarAsync(int id)
    {
        var avaliacao = await _context.Avaliacoes.FindAsync(id);
        if (avaliacao != null)
        {
            avaliacao.Aprovada = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExisteDuplicadaRecenteAsync(int pecaId, string nomeCliente, string comentario, TimeSpan janela)
    {
        var limite = DateTime.UtcNow.Subtract(janela);
        var comentarioTrim = (comentario ?? string.Empty).Trim();
        var nomeTrim = (nomeCliente ?? string.Empty).Trim();

        return await _context.Avaliacoes
            .AnyAsync(a => a.PecaId == pecaId &&
                           a.NomeCliente == nomeTrim &&
                           a.Comentario == comentarioTrim &&
                           a.DataCriacao >= limite);
    }

    public async Task RemoverAsync(int id)
    {
        var avaliacao = await _context.Avaliacoes.FindAsync(id);
        if (avaliacao != null)
        {
            _context.Avaliacoes.Remove(avaliacao);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<double> ObterMediaNotaPorPecaAsync(int pecaId)
    {
        var notas = await _context.Avaliacoes
            .Where(a => a.PecaId == pecaId && a.Aprovada)
            .Select(a => a.Nota)
            .ToListAsync();

        return notas.Any() ? notas.Average() : 0.0;
    }

    public async Task<int> ObterTotalAvaliacoesPorPecaAsync(int pecaId)
    {
        return await _context.Avaliacoes
            .CountAsync(a => a.PecaId == pecaId && a.Aprovada);
    }
}
