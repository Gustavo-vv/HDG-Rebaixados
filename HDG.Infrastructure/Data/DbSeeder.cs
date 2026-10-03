using HDG.Domain.Entidades;
using HDG.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HDG.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<HdgDbContext>();

        // 1. Roles
        string[] roles = { "Admin", "Cliente" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Admin Inicial
        var adminEmail = "admin@hdgrebaixados.com.br";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NomeCompleto = "Administrador HDG",
                EmailConfirmed = true,
                Ativo = true,
                DataCadastro = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, "Admin@HDG2026!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // 3. Peças de Exemplo
        if (!await context.Pecas.AnyAsync())
        {
            var pecas = new List<Peca>
            {
                new()
                {
                    Nome = "Kit Suspensão a Ar Tebão I-System 8mm Completo",
                    Descricao = "Kit de suspensão a ar de alta performance com gerenciamento por bluetooth via aplicativo e controle físico. Cilindro de alumínio polido, bloco de válvulas 8mm, compressor elétrico silencioso e bolsas gomadas cônicas super reforçadas. Projetado especialmente para carros rebaixados com máximo conforto e postura agressiva.",
                    Preco = 4890.00m,
                    Categoria = CategoriaPeca.Suspensao,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow,
                    Imagens = new List<PecaImagem>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1486006920555-c77dce18193b?auto=format&fit=crop&w=1000&q=80", Principal = true, Ordem = 1 },
                        new() { Url = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1000&q=80", Principal = false, Ordem = 2 }
                    },
                    Avaliacoes = new List<Avaliacao>
                    {
                        new() { NomeCliente = "Lucas Silva", Nota = 5, Comentario = "Instalei no meu Gol G5 e ficou simplesmente absurdo! Resposta rápida e acabamento impecável.", Aprovada = true, DataCriacao = DateTime.UtcNow.AddDays(-5) },
                        new() { NomeCliente = "Matheus Souza", Nota = 5, Comentario = "Muito top, atendimento da HDG é 10!", Aprovada = true, DataCriacao = DateTime.UtcNow.AddDays(-2) }
                    }
                },
                new()
                {
                    Nome = "Jogo de Rodas BBS RS Aro 18 Tala 8.5 Furação 4x100 Grafite com Borda Diamantada",
                    Descricao = "Jogo com 4 rodas esportivas clássicas modelo BBS RS com borda diamantada e miolo grafite acetinado. Acabamento de primeira linha, super resistente, com calotas centrais com trava hexagonal e furação universal 4x100 / 4x108. Perfeito para projetos eurolook e rebaixados clássicos.",
                    Preco = 3650.00m,
                    Categoria = CategoriaPeca.Rodas,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow,
                    Imagens = new List<PecaImagem>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1558441719-708d7c4be51c?auto=format&fit=crop&w=1000&q=80", Principal = true, Ordem = 1 },
                        new() { Url = "https://images.unsplash.com/photo-1580273916550-e323be2ae537?auto=format&fit=crop&w=1000&q=80", Principal = false, Ordem = 2 }
                    },
                    Avaliacoes = new List<Avaliacao>
                    {
                        new() { NomeCliente = "Renan Andrade", Nota = 5, Comentario = "Rodas lindas demais, o carro virou outro. Veio super bem embalado!", Aprovada = true, DataCriacao = DateTime.UtcNow.AddDays(-3) }
                    }
                },
                new()
                {
                    Nome = "Kit Suspensão de Rosca Slim Castor com Molas Especiais",
                    Descricao = "Kit de suspensão de rosca slim com regulagem milimétrica de altura. Amortecedores pressurizados a gás de alta pressão preparados para andar baixo sem quicar, flanges em aço zincado e molas cônicas especiais para assentamento perfeito na torre dianteira e traseira.",
                    Preco = 1890.00m,
                    Categoria = CategoriaPeca.Suspensao,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow,
                    Imagens = new List<PecaImagem>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?auto=format&fit=crop&w=1000&q=80", Principal = true, Ordem = 1 }
                    },
                    Avaliacoes = new List<Avaliacao>
                    {
                        new() { NomeCliente = "Gabriel Castro", Nota = 5, Comentario = "Melhor custo benefício pra quem quer andar no chão com conforto relativo.", Aprovada = true, DataCriacao = DateTime.UtcNow.AddDays(-1) }
                    }
                },
                new()
                {
                    Nome = "Kit Freio a Disco Dianteiro Ventilado e Perfurado 312mm com Pinças Brembo 4 Pistões",
                    Descricao = "Sistema de freio de alta performance Big Brake Kit com discos de 312mm perfurados e frisados para máxima dissipação térmica. Inclui pinças de 4 pistões em alumínio forjado vermelhas ou pretas, pastilhas de cerâmica de alto atrito e flexíveis de freio em malha de aço aeronáutico (aeroquip).",
                    Preco = 4200.00m,
                    Categoria = CategoriaPeca.Freios,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow,
                    Imagens = new List<PecaImagem>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1600790142055-619df03207e6?auto=format&fit=crop&w=1000&q=80", Principal = true, Ordem = 1 }
                    },
                    Avaliacoes = new List<Avaliacao>
                    {
                        new() { NomeCliente = "Thiago Pereira", Nota = 5, Comentario = "Poder de frenagem absurdo. Essencial para segurança depois de aumentar potência.", Aprovada = true, DataCriacao = DateTime.UtcNow.AddDays(-4) }
                    }
                },
                new()
                {
                    Nome = "Downpipe em Inox 304 com Ponteiras Duplas Esportivas 3.5\"",
                    Descricao = "Downpipe artesanal em aço inoxidável 304 com solda TIG automotiva, flexível reforçado e encaixes perfeitos nas saídas originais da turbina. Proporciona ganho de potência real de 12 a 18cv com ronco esportivo encorpado e liberação do fluxo de gases do motor.",
                    Preco = 1450.00m,
                    Categoria = CategoriaPeca.Escapamento,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow,
                    Imagens = new List<PecaImagem>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1541348263662-e0c866661ba5?auto=format&fit=crop&w=1000&q=80", Principal = true, Ordem = 1 }
                    }
                },
                new()
                {
                    Nome = "Kit Suspensão Fixa Preparada com Amortecedores Encurtados e Pratos Rebaixados",
                    Descricao = "Suspensão fixa na medida exata com altura travada e amortecedores pressurizados com haste encurtada. Trabalhado para não dar fim de curso e garantir estabilidade nas curvas mantendo o visual rebaixado dos sonhos sem burocracia.",
                    Preco = 1350.00m,
                    Categoria = CategoriaPeca.Suspensao,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow,
                    Imagens = new List<PecaImagem>
                    {
                        new() { Url = "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1000&q=80", Principal = true, Ordem = 1 }
                    }
                }
            };

            await context.Pecas.AddRangeAsync(pecas);
            await context.SaveChangesAsync();
        }
    }
}
