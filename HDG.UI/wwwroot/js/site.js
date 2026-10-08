// HDG REBAIXADOS - Interações de Interface & Animações
document.addEventListener('DOMContentLoaded', () => {
    // 0. Faixa de Anúncio Superior (Mobile Slider & Botão Fechar)
    const announcementStrip = document.getElementById('hdg-announcement-strip');
    if (announcementStrip) {
        // Verificar se já foi fechada nesta sessão
        if (sessionStorage.getItem('hdg_announcement_closed') === 'true') {
            announcementStrip.classList.add('fechada');
            announcementStrip.style.display = 'none';
        } else {
            // Botão Fechar
            const btnFechar = document.getElementById('btn-fechar-announcement');
            if (btnFechar) {
                btnFechar.addEventListener('click', (e) => {
                    e.preventDefault();
                    announcementStrip.classList.add('fechada');
                    sessionStorage.setItem('hdg_announcement_closed', 'true');
                    setTimeout(() => {
                        announcementStrip.style.display = 'none';
                    }, 300);
                });
            }

            // Alternância de mensagens no Mobile (< 992px)
            const items = announcementStrip.querySelectorAll('.announcement-item');
            if (items.length > 1) {
                let currentIndex = 0;
                let intervaloRotacao = null;
                let isPaused = false;
                const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

                function proximaMensagem() {
                    if (window.innerWidth >= 992 || isPaused || prefersReducedMotion) return;

                    items[currentIndex].classList.remove('announcement-item-active');
                    currentIndex = (currentIndex + 1) % items.length;
                    items[currentIndex].classList.add('announcement-item-active');
                }

                function iniciarRotacao() {
                    if (intervaloRotacao) clearInterval(intervaloRotacao);
                    if (!prefersReducedMotion) {
                        intervaloRotacao = setInterval(proximaMensagem, 4000);
                    }
                }

                // Pausar no hover ou no foco
                announcementStrip.addEventListener('mouseenter', () => { isPaused = true; });
                announcementStrip.addEventListener('mouseleave', () => { isPaused = false; });
                announcementStrip.addEventListener('focusin', () => { isPaused = true; });
                announcementStrip.addEventListener('focusout', () => { isPaused = false; });

                iniciarRotacao();

                window.addEventListener('resize', () => {
                    if (window.innerWidth >= 992) {
                        items.forEach(it => it.classList.remove('announcement-item-active'));
                        items[0].classList.add('announcement-item-active');
                    }
                }, { passive: true });
            }
        }
    }

    // 1. Navbar Sticky Glass Effect ao Rolar a Página
    const navbar = document.querySelector('.glass-nav');
    if (navbar) {
        window.addEventListener('scroll', () => {
            if (window.scrollY > 40) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        }, { passive: true });
    }

    // 1.1 Fechar Menu Mobile ao clicar em links e ao redimensionar
    const hdgNavbar = document.getElementById('hdgNavbar');
    if (hdgNavbar && typeof bootstrap !== 'undefined') {
        const collapseInstance = bootstrap.Collapse.getOrCreateInstance(hdgNavbar, { toggle: false });
        const mobileLinks = hdgNavbar.querySelectorAll('a, button:not(.dropdown-toggle)');
        mobileLinks.forEach(link => {
            link.addEventListener('click', () => {
                if (window.innerWidth < 992 && hdgNavbar.classList.contains('show')) {
                    setTimeout(() => {
                        collapseInstance.hide();
                    }, 100);
                }
            });
        });

        window.addEventListener('resize', () => {
            if (window.innerWidth >= 992 && hdgNavbar.classList.contains('show')) {
                collapseInstance.hide();
            }
        });
    }

    // 2. Animação de Entrada com Fade/Slide (Intersection Observer)
    const fadeElements = document.querySelectorAll('.fade-in-up');
    if (fadeElements.length > 0) {
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

    // 3. Animação Suave de Números/Contadores na Viewport
    const counters = document.querySelectorAll('[data-counter-target]');
    if (counters.length > 0) {
        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        const countObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const el = entry.target;
                    const target = parseInt(el.getAttribute('data-counter-target'), 10);
                    const prefix = el.getAttribute('data-counter-prefix') || '';
                    const suffix = el.getAttribute('data-counter-suffix') || '';
                    const duration = 1600; // ms

                    if (prefersReducedMotion || isNaN(target)) {
                        el.textContent = `${prefix}${target.toLocaleString('pt-BR')}${suffix}`;
                        observer.unobserve(el);
                        return;
                    }

                    let start = 0;
                    const stepTime = 20;
                    const totalSteps = duration / stepTime;
                    const increment = target / totalSteps;

                    const timer = setInterval(() => {
                        start += increment;
                        if (start >= target) {
                            el.textContent = `${prefix}${target.toLocaleString('pt-BR')}${suffix}`;
                            clearInterval(timer);
                        } else {
                            el.textContent = `${prefix}${Math.floor(start).toLocaleString('pt-BR')}${suffix}`;
                        }
                    }, stepTime);

                    observer.unobserve(el);
                }
            });
        }, { threshold: 0.2 });

        counters.forEach(counter => countObserver.observe(counter));
    }

    // 4. Parallax Sutil na Logo do Hero (Desktop, desligado em touch e prefers-reduced-motion)
    const stage = document.querySelector('.hero-logo-stage');
    const emblem = document.querySelector('.hero-emblem-main');
    if (stage && emblem) {
        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const isTouch = window.matchMedia('(pointer: coarse)').matches;

        if (!prefersReducedMotion && !isTouch) {
            stage.addEventListener('mousemove', (e) => {
                const rect = stage.getBoundingClientRect();
                const x = e.clientX - rect.left - rect.width / 2;
                const y = e.clientY - rect.top - rect.height / 2;
                const tiltX = -(y / (rect.height / 2)) * 4; // Max 4 graus
                const tiltY = (x / (rect.width / 2)) * 4;   // Max 4 graus

                emblem.style.transform = `perspective(800px) rotateX(${tiltX.toFixed(2)}deg) rotateY(${tiltY.toFixed(2)}deg) translateY(-5px)`;
            });

            stage.addEventListener('mouseleave', () => {
                emblem.style.transform = '';
            });
        }
    }

    // 5. Modal Global de Sucesso da Avaliação
    const modalSucessoEl = document.getElementById('modalSucessoAvaliacao');
    if (modalSucessoEl && typeof bootstrap !== 'undefined') {
        const modalSucesso = new bootstrap.Modal(modalSucessoEl, {
            backdrop: true,
            keyboard: true
        });
        modalSucesso.show();

        modalSucessoEl.addEventListener('shown.bs.modal', () => {
            const btnEntendi = document.getElementById('btnFecharModalSucesso');
            if (btnEntendi) btnEntendi.focus();
        });
    }

    // 6. Sistema Unificado de Notificações / Toasts HDG
    function criarToastElement(mensagem, tipo = 'sucesso') {
        const isErro = tipo === 'erro' || tipo === 'danger' || tipo === 'error';
        const delay = isErro ? 7000 : 5000;
        const iconClass = isErro ? 'bi-exclamation-octagon-fill' : 'bi-check-circle-fill';
        const role = isErro ? 'alert' : 'status';

        const toastDiv = document.createElement('div');
        toastDiv.className = `hdg-toast ${isErro ? 'hdg-toast-erro' : 'hdg-toast-sucesso'}`;
        toastDiv.setAttribute('role', role);

        toastDiv.innerHTML = `
            <i class="bi ${iconClass} hdg-toast-icon"></i>
            <div class="hdg-toast-body">${mensagem}</div>
            <button type="button" class="hdg-toast-close" aria-label="Fechar notificação">
                <i class="bi bi-x-lg"></i>
            </button>
        `;

        configurarComportamentoToast(toastDiv, delay);
        return toastDiv;
    }

    function configurarComportamentoToast(toastEl, customDelay) {
        let timer = null;
        let restante = customDelay || parseInt(toastEl.getAttribute('data-bs-delay') || '5000', 10);
        let tempoInicio = Date.now();

        const iniciarTimer = () => {
            tempoInicio = Date.now();
            timer = setTimeout(() => {
                fecharToast(toastEl);
            }, restante);
        };

        const pausarTimer = () => {
            if (timer) {
                clearTimeout(timer);
                timer = null;
                restante -= (Date.now() - tempoInicio);
                if (restante < 500) restante = 500;
            }
        };

        const fecharToast = (el) => {
            if (timer) clearTimeout(timer);
            el.classList.remove('show');
            el.classList.add('hide');
            setTimeout(() => {
                if (el.parentNode) el.parentNode.removeChild(el);
            }, 300);
        };

        // Botão Fechar
        const btnClose = toastEl.querySelector('.hdg-toast-close');
        if (btnClose) {
            btnClose.addEventListener('click', () => fecharToast(toastEl));
        }

        // Pausa no Hover e no Foco
        toastEl.addEventListener('mouseenter', pausarTimer);
        toastEl.addEventListener('mouseleave', iniciarTimer);
        toastEl.addEventListener('focusin', pausarTimer);
        toastEl.addEventListener('focusout', iniciarTimer);

        // Exibição animada (respeita reflow)
        requestAnimationFrame(() => {
            toastEl.classList.add('show');
            iniciarTimer();
        });
    }

    window.mostrarToast = function (mensagem, tipo = 'sucesso') {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.setAttribute('aria-live', 'polite');
            container.setAttribute('aria-atomic', 'true');
            document.body.appendChild(container);
        }

        // Limite de no máximo 3 toasts empilhados simultaneamente
        const toastsAtuais = container.querySelectorAll('.hdg-toast:not(.hide)');
        if (toastsAtuais.length >= 3) {
            // Remove o mais antigo imediatamente
            const maisAntigo = toastsAtuais[0];
            maisAntigo.classList.remove('show');
            maisAntigo.classList.add('hide');
            setTimeout(() => { if (maisAntigo.parentNode) maisAntigo.parentNode.removeChild(maisAntigo); }, 200);
        }

        const novoToast = criarToastElement(mensagem, tipo);
        container.appendChild(novoToast);
    };

    // Inicialização dos Toasts renderizados pelo servidor (TempData)
    const containerServidor = document.getElementById('toast-container');
    if (containerServidor) {
        const toastsServidor = containerServidor.querySelectorAll('.hdg-toast');
        toastsServidor.forEach((t, index) => {
            if (index >= 3) {
                t.remove();
                return;
            }
            const delay = parseInt(t.getAttribute('data-bs-delay') || '5000', 10);
            configurarComportamentoToast(t, delay);
        });
    }


    // 7. Formulário de Avaliação (Estrelas, Contador e Bloqueio de Clique Duplo)
    const starRadios = document.querySelectorAll('.star-rating-widget .star-radio');
    const labelNotaDescricao = document.getElementById('labelNotaDescricao');

    if (starRadios.length > 0 && labelNotaDescricao) {
        starRadios.forEach(radio => {
            radio.addEventListener('change', () => {
                const desc = radio.getAttribute('data-desc');
                if (desc) {
                    labelNotaDescricao.textContent = desc;
                }
            });
        });
    }

    const comentarioTexto = document.getElementById('comentarioTexto');
    const comentarioContador = document.getElementById('comentarioContador');
    if (comentarioTexto && comentarioContador) {
        comentarioTexto.addEventListener('input', () => {
            const tamanho = comentarioTexto.value.length;
            comentarioContador.textContent = `${tamanho}/1000`;
            if (tamanho >= 950) {
                comentarioContador.classList.add('text-warning');
            } else {
                comentarioContador.classList.remove('text-warning');
            }
        });
    }

    const formAvaliacaoPeca = document.getElementById('formAvaliacaoPeca');
    if (formAvaliacaoPeca) {
        formAvaliacaoPeca.addEventListener('submit', (e) => {
            const btn = document.getElementById('btnEnviarAvaliacao');
            if (btn) {
                const btnText = btn.querySelector('.btn-text');
                const btnLoading = btn.querySelector('.btn-loading');

                btn.disabled = true;
                if (btnText) btnText.classList.add('d-none');
                if (btnLoading) {
                    btnLoading.classList.remove('d-none');
                    btnLoading.classList.add('d-inline-flex');
                }
            }
        });
    }

    // 8. Rede de Segurança Global para Modais do Bootstrap (Stacking Context & Acessibilidade)
    // Garante que todo modal seja filho direto de <body>, com foco automático e sem backdrops presos
    document.addEventListener('show.bs.modal', (event) => {
        const modal = event.target;
        if (modal && modal.parentElement !== document.body) {
            document.body.appendChild(modal);
        }
    });

    document.addEventListener('shown.bs.modal', (event) => {
        const modal = event.target;
        if (modal) {
            // Foco no primeiro campo interativo não oculto
            const firstInteractive = modal.querySelector('input:not([type="hidden"]):not([disabled]):not([readonly]), select:not([disabled]), textarea:not([disabled]), button.btn-primary:not([disabled]), button.btn-racing:not([disabled]), button[type="submit"]:not([disabled])');
            if (firstInteractive) {
                firstInteractive.focus();
            }
        }
    });

    document.addEventListener('hidden.bs.modal', () => {
        // Se nenhum modal estiver visível, garante limpeza de classes ou backdrops órfãos
        const openModals = document.querySelectorAll('.modal.show');
        if (openModals.length === 0) {
            const lingeringBackdrops = document.querySelectorAll('.modal-backdrop');
            lingeringBackdrops.forEach(b => b.remove());
            document.body.classList.remove('modal-open');
            document.body.style.removeProperty('overflow');
            document.body.style.removeProperty('padding-right');
        }
    });

    // 9. WhatsApp FAB – Ocultação inteligente (modal aberto, overlap com footer/toast)
    const whatsappFab = document.getElementById('whatsapp-fab');
    if (whatsappFab) {
        const HIDDEN_CLASS = 'whatsapp-fab--hidden';

        // a) Esconder durante modais
        document.addEventListener('show.bs.modal', () => {
            whatsappFab.classList.add(HIDDEN_CLASS);
        });
        document.addEventListener('hidden.bs.modal', () => {
            const anyOpen = document.querySelectorAll('.modal.show');
            if (anyOpen.length === 0) {
                whatsappFab.classList.remove(HIDDEN_CLASS);
            }
        });

        // b) Esconder quando o footer está visível (evitar sobreposição)
        const footer = document.querySelector('.footer-custom');
        if (footer) {
            const fabFooterObserver = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    // Só altera se nenhum modal estiver aberto (modais têm prioridade)
                    if (document.querySelectorAll('.modal.show').length > 0) return;
                    if (entry.isIntersecting) {
                        whatsappFab.classList.add(HIDDEN_CLASS);
                    } else {
                        whatsappFab.classList.remove(HIDDEN_CLASS);
                    }
                });
            }, { rootMargin: '0px 0px -40px 0px', threshold: 0.05 });

            fabFooterObserver.observe(footer);
        }
    }

});



