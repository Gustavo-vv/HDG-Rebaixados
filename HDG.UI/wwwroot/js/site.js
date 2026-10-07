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

    // 6. Toasts Globais de Notificação
    const toastElements = document.querySelectorAll('.toast');
    if (toastElements.length > 0 && typeof bootstrap !== 'undefined') {
        toastElements.forEach(t => {
            const toast = new bootstrap.Toast(t);
            toast.show();
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
});

