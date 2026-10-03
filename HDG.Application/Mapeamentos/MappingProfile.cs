using AutoMapper;
using HDG.Application.DTOs;
using HDG.Domain.Entidades;

namespace HDG.Application.Mapeamentos;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Peca, PecaDto>()
            .ForMember(dest => dest.ImagemPrincipalUrl, opt => opt.MapFrom(src => 
                src.Imagens.Where(i => i.Principal).Select(i => i.Url).FirstOrDefault() ?? 
                src.Imagens.OrderBy(i => i.Ordem).Select(i => i.Url).FirstOrDefault()))
            .ForMember(dest => dest.MediaAvaliacoes, opt => opt.MapFrom(src => 
                src.Avaliacoes.Any(a => a.Aprovada) ? src.Avaliacoes.Where(a => a.Aprovada).Average(a => a.Nota) : 0))
            .ForMember(dest => dest.TotalAvaliacoes, opt => opt.MapFrom(src => 
                src.Avaliacoes.Count(a => a.Aprovada)));

        CreateMap<PecaImagem, PecaImagemDto>();

        CreateMap<Avaliacao, AvaliacaoDto>()
            .ForMember(dest => dest.PecaNome, opt => opt.MapFrom(src => src.Peca != null ? src.Peca.Nome : string.Empty));
    }
}
