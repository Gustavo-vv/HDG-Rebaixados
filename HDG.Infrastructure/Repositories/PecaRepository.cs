using HDG.Domain.Entidades;
using HDG.Domain.Enums;
using HDG.Domain.Interfaces.Repositories;
using HDG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HDG.Infrastructure.Repositories;

public class PecaRepository : IPecaRepository
{
    private readonly HdgDbContext _context;

    public PecaRepository(HdgDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Peca>> ObterTodasAsync(bool apenasAtivas = true, string? busca = null, CategoriaPeca? categoria = null)
    {
        var query = _context.Pecas
            .Include(p => p.Imagens)
            .Include(p => p.Avaliacoes)
            .AsNoTracking()
            .AsQueryable();

        if (apenasAtivas)
            query = query.Where(p => p.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim().ToLower();
            query = query.Where(p => p.Nome.ToLower().Contains(busca) || p.Descricao.ToLower().Contains(busca));
        }

        if (categoria.HasValue)
            query = query.Where(p => p.Categoria == categoria.Value);

        return await query.OrderByDescending(p => p.Id).ToListAsync();
    }

    public async Task<Peca?> ObterPorIdAsync(int id, bool incluirInativas = false)
    {
        var query = _context.Pecas
            .Include(p => p.Imagens)
            .Include(p => p.Avaliacoes)
            .AsQueryable();

        if (!incluirInativas)
            query = query.Where(p => p.Ativo);

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Peca> AdicionarAsync(Peca peca)
    {
        await _context.Pecas.AddAsync(peca);
        await _context.SaveChangesAsync();
        return peca;
    }

    public async Task AtualizarAsync(Peca peca)
    {
        _context.Pecas.Update(peca);
        await _context.SaveChangesAsync();
    }

    public async Task RemoverAsync(int id)
    {
        var peca = await _context.Pecas.FindAsync(id);
        if (peca != null)
        {
            _context.Pecas.Remove(peca);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExisteAsync(int id)
    {
        return await _context.Pecas.AnyAsync(p => p.Id == id);
    }

    public async Task AdicionarImagemAsync(PecaImagem imagem)
    {
        await _context.PecaImagens.AddAsync(imagem);
        await _context.SaveChangesAsync();
    }

    public async Task RemoverImagemAsync(int imagemId)
    {
        var img = await _context.PecaImagens.FindAsync(imagemId);
        if (img != null)
        {
            _context.PecaImagens.Remove(img);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PecaImagem?> ObterImagemPorIdAsync(int imagemId)
    {
        return await _context.PecaImagens.FindAsync(imagemId);
    }
}
