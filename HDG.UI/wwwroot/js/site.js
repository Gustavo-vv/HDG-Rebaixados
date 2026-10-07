// HDG REBAIXADOS - Interações de Interface & Animações
document.addEventListener('DOMContentLoaded', () => {
    // 1. Navbar Sticky Glass Effect ao Rolar a Página
    const navbar = document.querySelector('.glass-nav');
    if (navbar) {
        window.addEventListener('scroll', () => {
            if (window.scrollY > 20) {
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
});
