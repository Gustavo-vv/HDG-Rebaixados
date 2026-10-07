/**
 * HDG REBAIXADOS - Gerenciamento Dinâmico de Catálogo (Sem Reload / AJAX SPA)
 * Recursos: Progressive Enhancement, Histórico do Navegador (pushState/popState),
 * Transições suaves (Fade), Cancelamento via AbortController, Acessibilidade (aria-live).
 */

(function () {
    'use strict';

    // Controlador para cancelar requisições AJAX pendentes caso o usuário clique rapidamente
    let controladorRequisicao = null;

    /**
     * Obtém o parâmetro 'categoria' de uma URL.
     * Retorna a string do id da categoria ou null se for geral/todos.
     */
    function extrairCategoriaDeUrl(urlCompleta) {
        try {
            const urlObj = new URL(urlCompleta, window.location.origin);
            return urlObj.searchParams.get('categoria');
        } catch {
            return null;
        }
    }

    /**
     * Atualiza o estado visual ativo (classe .active) dos cards de categoria na home,
     * das pills de filtro do catálogo e dos links do menu de navegação.
     */
    function atualizarEstadoAtivo(urlAtual) {
        const catId = extrairCategoriaDeUrl(urlAtual);

        // 1. Atualizar Cards de Destaque na Seção "Seleção por categoria"
        const cardsCategoria = document.querySelectorAll('.category-card[data-categoria]');
        cardsCategoria.forEach(card => {
            const cardCat = card.getAttribute('data-categoria');
            if (catId && cardCat === catId) {
                card.classList.add('active');
            } else {
                card.classList.remove('active');
            }
        });

        // 2. Atualizar Links do Menu Superior
        const linksMenu = document.querySelectorAll('.nav-link-custom[data-filtro-catalogo]');
        linksMenu.forEach(link => {
            const linkHref = link.getAttribute('href') || '';
            const linkCat = extrairCategoriaDeUrl(linkHref);
            if ((!catId && !linkCat) || (catId && linkCat === catId)) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
    }

    /**
     * Reinicializa componentes e observadores que dependem do conteúdo recém-injetado
     */
    function reinicializarCatalogo() {
        // Reinicializar animações com IntersectionObserver se disponível
        const fadeElements = document.querySelectorAll('#catalogo-conteudo .fade-in-up');
        if (fadeElements.length > 0 && 'IntersectionObserver' in window) {
            const fadeObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add('visible');
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.1 });

            fadeElements.forEach(el => fadeObserver.observe(el));
        }
    }

    /**
     * Substitui o conteúdo de #catalogo-conteudo com uma transição de fade suave
     */
    function trocarConteudo(novoHtmlConteudo, rolarAteCatalogo) {
        const containerAtual = document.getElementById('catalogo-conteudo');
        if (!containerAtual) return;

        const prefereSemAnimacao = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const duracaoFade = prefereSemAnimacao ? 0 : 160;

        // Inicia o fade out
        containerAtual.classList.add('catalogo-carregando');

        setTimeout(() => {
            // Substitui o miolo do catálogo
            containerAtual.innerHTML = novoHtmlConteudo;

            // Remove o estado de carregamento (fade in)
            containerAtual.classList.remove('catalogo-carregando');

            // Reinicializa comportamentos do grid
            reinicializarCatalogo();

            // Rola suavemente até a seção se solicitado (ex: clique fora da seção)
            if (rolarAteCatalogo) {
                const secaoCatalogo = document.getElementById('catalogo');
                if (secaoCatalogo) {
                    secaoCatalogo.scrollIntoView({
                        behavior: prefereSemAnimacao ? 'auto' : 'smooth',
                        block: 'start'
                    });
                }
            }

            // Acessibilidade: focar a barra de status / contador de resultados
            const badgeContador = document.getElementById('catalogo-contador-badge');
            if (badgeContador) {
                badgeContador.focus();
            }
        }, duracaoFade);
    }

    /**
     * Carrega a página via fetch e extrai apenas o #catalogo-conteudo
     */
    async function carregarCatalogo(urlDestino, adicionarAoHistorico = true, rolarAteCatalogo = true) {
        // Se houver uma requisição em andamento, cancela para evitar race condition
        if (controladorRequisicao) {
            controladorRequisicao.abort();
        }

        controladorRequisicao = new AbortController();
        const signal = controladorRequisicao.signal;

        const containerAtual = document.getElementById('catalogo-conteudo');
        if (containerAtual) {
            containerAtual.classList.add('catalogo-carregando');
        }

        try {
            const resposta = await fetch(urlDestino, {
                headers: {
                    'X-Requested-With': 'fetch'
                },
                signal: signal
            });

            if (!resposta.ok) {
                // Fallback para navegação tradicional se o servidor retornar erro
                window.location.href = urlDestino;
                return;
            }

            const htmlTexto = await resposta.text();
            const parser = new DOMParser();
            const docNovo = parser.parseFromString(htmlTexto, 'text/html');

            // 1. Atualizar Título da Página se houver mudança
            if (docNovo.title) {
                document.title = docNovo.title;
            }

            // 2. Extrair o container do catálogo
            const novoContainer = docNovo.getElementById('catalogo-conteudo');
            if (novoContainer) {
                trocarConteudo(novoContainer.innerHTML, rolarAteCatalogo);

                // 3. Atualizar histórico do navegador
                if (adicionarAoHistorico) {
                    window.history.pushState({ url: urlDestino }, '', urlDestino);
                }

                // 4. Atualizar estados visuais de ativação
                atualizarEstadoAtivo(urlDestino);
            } else {
                // Se a página de destino não tiver #catalogo-conteudo (ex: navegou para /Sobre), faz fallback
                window.location.href = urlDestino;
            }
        } catch (erro) {
            // Se foi cancelado intencionalmente, apenas ignora
            if (erro.name === 'AbortError') {
                return;
            }

            console.error('Falha ao carregar catálogo dinamicamente:', erro);
            // Fallback elegante para navegação completa
            window.location.href = urlDestino;
        } finally {
            controladorRequisicao = null;
        }
    }

    /**
     * Intercepta cliques nos links de catálogo via Event Delegation
     */
    function interceptarCliques(evento) {
        // Se a página atual não possuir a seção de catálogo, não intercepta (deixa navegar para a Home)
        const temCatalogoNaPagina = !!document.getElementById('catalogo-conteudo');
        if (!temCatalogoNaPagina) return;

        // Permite abrir em nova aba com botões especiais (Ctrl, Cmd, Shift, Botão do Meio)
        if (evento.metaKey || evento.ctrlKey || evento.shiftKey || evento.altKey || evento.button !== 0) {
            return;
        }

        // Busca o elemento <a> mais próximo que possua os critérios de filtro
        const link = evento.target.closest('a');
        if (!link) return;

        const href = link.getAttribute('href');
        if (!href) return;

        // Verifica se é um link com o marcador explícito ou relativo para a Home com parâmetros de filtro
        const temAtributoFiltro = link.hasAttribute('data-filtro-catalogo') || link.classList.contains('category-card');
        const ehUrlDeCatalogo = (
            href.includes('categoria=') ||
            href.includes('busca=') ||
            href === '/' ||
            href === '/Home' ||
            href === '/Home/Index' ||
            href.startsWith('/?') ||
            href.startsWith('/Home/?') ||
            href.startsWith('/Home/Index?')
        );

        // Se for um link de catálogo elegível
        if (temAtributoFiltro || (ehUrlDeCatalogo && !href.includes('/Sobre') && !href.includes('/Detalhes') && !href.includes('/Admin') && !href.includes('/Auth'))) {
            evento.preventDefault();

            // Determina se deve rolar até o catálogo (se clicou fora dele, rola; se clicou numa pill dentro, não precisa rolar)
            const clicouDentroDoCatalogo = !!link.closest('#catalogo');
            const deveRolar = !clicouDentroDoCatalogo;

            carregarCatalogo(link.href, true, deveRolar);
        }
    }

    /**
     * Intercepta o envio do formulário de busca para executar via AJAX
     */
    function interceptarBusca(evento) {
        const form = evento.target.closest('#form-busca-catalogo');
        if (!form) return;

        evento.preventDefault();

        const formData = new FormData(form);
        const params = new URLSearchParams();

        for (const [key, value] of formData.entries()) {
            if (value && value.toString().trim() !== '') {
                params.set(key, value.toString().trim());
            }
        }

        const actionUrl = form.getAttribute('action') || window.location.pathname;
        const urlFinal = `${actionUrl}?${params.toString()}`;

        carregarCatalogo(urlFinal, true, false);
    }

    /**
     * Trata o botão Voltar / Avançar do navegador (popstate)
     */
    function gerenciarPopState() {
        const temCatalogoNaPagina = !!document.getElementById('catalogo-conteudo');
        if (!temCatalogoNaPagina) return;

        // Carrega o conteúdo correspondente à nova URL sem adicionar novamente ao histórico
        carregarCatalogo(window.location.href, false, false);
    }

    // Inicialização quando o DOM estiver pronto
    document.addEventListener('DOMContentLoaded', () => {
        // Sincroniza o estado inicial ativo com a URL carregada
        atualizarEstadoAtivo(window.location.href);

        // Event delegation global único para cliques
        document.addEventListener('click', interceptarCliques);

        // Event delegation global único para submit de busca
        document.addEventListener('submit', interceptarBusca);

        // Ouvinte de histórico do navegador
        window.addEventListener('popstate', gerenciarPopState);
    });
})();
