// 3D Tilt Effect for Cards
function initTiltEffect() {
    const tiltCards = document.querySelectorAll('.tilt-card, .card-3d');

    tiltCards.forEach(card => {
        card.addEventListener('mousemove', (e) => {
            const rect = card.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            const centerX = rect.width / 2;
            const centerY = rect.height / 2;

            const rotateX = (y - centerY) / 10;
            const rotateY = (centerX - x) / 10;

            card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.05, 1.05, 1.05)`;
        });

        card.addEventListener('mouseleave', () => {
            card.style.transform = 'perspective(1000px) rotateX(0) rotateY(0) scale3d(1, 1, 1)';
        });
    });
}

// Magnetic Button Effect
function initMagneticButtons() {
    const magneticBtns = document.querySelectorAll('.magnetic-btn');

    magneticBtns.forEach(btn => {
        btn.addEventListener('mousemove', (e) => {
            const rect = btn.getBoundingClientRect();
            const x = e.clientX - rect.left - rect.width / 2;
            const y = e.clientY - rect.top - rect.height / 2;

            btn.style.transform = `translate(${x * 0.3}px, ${y * 0.3}px)`;
        });

        btn.addEventListener('mouseleave', () => {
            btn.style.transform = 'translate(0, 0)';
        });
    });
}

// Parallax Effect on Scroll
function initParallaxEffect() {
    const parallaxElements = document.querySelectorAll('.parallax-layer');

    window.addEventListener('scroll', () => {
        const scrolled = window.pageYOffset;

        parallaxElements.forEach((element, index) => {
            const speed = (index + 1) * 0.5;
            element.style.transform = `translateY(${scrolled * speed}px)`;
        });
    });
}

// Smooth Scroll Reveal
function initScrollReveal() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-fade-in-up');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    document.querySelectorAll('.reveal-on-scroll').forEach(el => {
        observer.observe(el);
    });
}

// Cursor Trail Effect
function initCursorTrail() {
    const coords = { x: 0, y: 0 };
    const circles = document.querySelectorAll('.cursor-trail-circle');

    if (circles.length === 0) {
        // Create cursor trail circles
        for (let i = 0; i < 20; i++) {
            const circle = document.createElement('div');
            circle.className = 'cursor-trail-circle';
            circle.style.cssText = `
                position: fixed;
                width: 10px;
                height: 10px;
                border-radius: 50%;
                background: linear-gradient(135deg, #667eea, #764ba2);
                pointer-events: none;
                z-index: 9999;
                opacity: ${1 - i * 0.05};
                transition: transform 0.2s ease-out;
            `;
            document.body.appendChild(circle);
        }
    }

    const allCircles = document.querySelectorAll('.cursor-trail-circle');

    window.addEventListener('mousemove', (e) => {
        coords.x = e.clientX;
        coords.y = e.clientY;
    });

    function animateCircles() {
        let x = coords.x;
        let y = coords.y;

        allCircles.forEach((circle, index) => {
            circle.style.left = x - 5 + 'px';
            circle.style.top = y - 5 + 'px';
            circle.style.transform = `scale(${(allCircles.length - index) / allCircles.length})`;

            const nextCircle = allCircles[index + 1] || allCircles[0];
            x += (nextCircle.offsetLeft - x) * 0.3;
            y += (nextCircle.offsetTop - y) * 0.3;
        });

        requestAnimationFrame(animateCircles);
    }

    animateCircles();
}

// Floating Particles Background
function initParticles(container) {
    if (!container) return;

    const particleCount = 50;

    for (let i = 0; i < particleCount; i++) {
        const particle = document.createElement('div');
        particle.className = 'particle';
        particle.style.cssText = `
            position: absolute;
            width: ${Math.random() * 4 + 1}px;
            height: ${Math.random() * 4 + 1}px;
            background: radial-gradient(circle, rgba(255,255,255,0.8), transparent);
            border-radius: 50%;
            left: ${Math.random() * 100}%;
            top: ${Math.random() * 100}%;
            animation: float ${Math.random() * 10 + 10}s ease-in-out infinite;
            animation-delay: ${Math.random() * 5}s;
            opacity: ${Math.random() * 0.5 + 0.2};
        `;
        container.appendChild(particle);
    }
}

// Ripple Click Effect
function initRippleEffect() {
    document.addEventListener('click', (e) => {
        const ripple = document.createElement('span');
        ripple.className = 'click-ripple';
        ripple.style.cssText = `
            position: fixed;
            border-radius: 50%;
            background: rgba(102, 126, 234, 0.5);
            transform: translate(-50%, -50%);
            pointer-events: none;
            animation: ripple-expand 0.6s ease-out;
            left: ${e.clientX}px;
            top: ${e.clientY}px;
            z-index: 9999;
        `;
        document.body.appendChild(ripple);

        setTimeout(() => ripple.remove(), 600);
    });

    // Add ripple animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes ripple-expand {
            0% {
                width: 0;
                height: 0;
                opacity: 1;
            }
            100% {
                width: 500px;
                height: 500px;
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
}

// Number Counter Animation
function animateCounter(element, target, duration = 2000) {
    const start = 0;
    const increment = target / (duration / 16);
    let current = start;

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            element.textContent = Math.round(target);
            clearInterval(timer);
        } else {
            element.textContent = Math.round(current);
        }
    }, 16);
}

// Glitch Text Effect
function initGlitchEffect(element) {
    const text = element.textContent;
    const glitchChars = '!<>-_\\/[]{}—=+*^?#________';

    setInterval(() => {
        let glitched = '';
        for (let i = 0; i < text.length; i++) {
            if (Math.random() < 0.02) {
                glitched += glitchChars[Math.floor(Math.random() * glitchChars.length)];
            } else {
                glitched += text[i];
            }
        }
        element.textContent = glitched;

        setTimeout(() => {
            element.textContent = text;
        }, 50);
    }, 3000);
}

// Typewriter Effect
function typewriterEffect(element, text, speed = 100) {
    let i = 0;
    element.textContent = '';

    function type() {
        if (i < text.length) {
            element.textContent += text.charAt(i);
            i++;
            setTimeout(type, speed);
        }
    }

    type();
}

// Canvas Gradient Background
function initCanvasGradient(canvas) {
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    let time = 0;

    function resize() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    }

    function draw() {
        const gradient = ctx.createLinearGradient(
            Math.sin(time) * canvas.width,
            0,
            Math.cos(time) * canvas.width,
            canvas.height
        );

        gradient.addColorStop(0, '#667eea');
        gradient.addColorStop(0.5, '#764ba2');
        gradient.addColorStop(1, '#f093fb');

        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        time += 0.01;
        requestAnimationFrame(draw);
    }

    window.addEventListener('resize', resize);
    resize();
    draw();
}

// Initialize all effects on page load
document.addEventListener('DOMContentLoaded', () => {
    initTiltEffect();
    initMagneticButtons();
    initParallaxEffect();
    initScrollReveal();
    // initCursorTrail(); // Uncomment if you want cursor trail
    // initRippleEffect(); // Disabled per user request - click circle effect removed

    // Initialize particles if container exists
    const particleContainer = document.querySelector('.particles-container');
    if (particleContainer) {
        initParticles(particleContainer);
    }

    // Animate counters
    document.querySelectorAll('.counter').forEach(counter => {
        const target = parseInt(counter.getAttribute('data-target'));
        if (target) {
            animateCounter(counter, target);
        }
    });
});

// Export functions for use in other scripts
window.premiumEffects = {
    initTiltEffect,
    initMagneticButtons,
    initParallaxEffect,
    initScrollReveal,
    initCursorTrail,
    initParticles,
    initRippleEffect,
    animateCounter,
    initGlitchEffect,
    typewriterEffect,
    initCanvasGradient
};
