using HDG.Domain.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HDG.Infrastructure.Data;

public class HdgDbContext : IdentityDbContext<ApplicationUser>
{
    public HdgDbContext(DbContextOptions<HdgDbContext> options) : base(options)
    {
    }

    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<PecaImagem> PecaImagens => Set<PecaImagem>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Peca>(entity =>
        {
            entity.ToTable("Pecas");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nome).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Descricao).IsRequired().HasMaxLength(2000);
            entity.Property(p => p.Preco).HasPrecision(18, 2);
            entity.Property(p => p.Categoria).IsRequired();
            entity.Property(p => p.Ativo).HasDefaultValue(true);

            entity.HasMany(p => p.Imagens)
                  .WithOne(i => i.Peca)
                  .HasForeignKey(i => i.PecaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Avaliacoes)
                  .WithOne(a => a.Peca)
                  .HasForeignKey(a => a.PecaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PecaImagem>(entity =>
        {
            entity.ToTable("PecaImagens");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Url).IsRequired().HasMaxLength(1000);
            entity.Property(i => i.PublicId).HasMaxLength(250);
        });

        builder.Entity<Avaliacao>(entity =>
        {
            entity.ToTable("Avaliacoes");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.NomeCliente).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Comentario).HasMaxLength(1000);
            entity.Property(a => a.Nota).IsRequired();
            entity.Property(a => a.Aprovada).HasDefaultValue(false);
        });
    }
}
