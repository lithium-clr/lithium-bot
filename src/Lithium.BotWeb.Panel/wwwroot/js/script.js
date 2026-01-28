// Animação de entrada do Login
anime({
    targets: '.glass-card',
    translateY: [50, 0],
    opacity: [0, 1],
    duration: 1000,
    easing: 'easeOutExpo'
});

function login() {
    // Animação de saída do login
    anime({
        targets: '#login-section',
        opacity: 0,
        scale: 0.9,
        duration: 500,
        easing: 'easeInOutQuad',
        complete: function () {
            document.getElementById('login-section').classList.add('d-none');
            showDashboard();
        }
    });
}

function showDashboard() {
    const dash = document.getElementById('dashboard-section');
    dash.classList.remove('d-none');

    // Animação dos cards aparecendo em cascata
    anime({
        targets: '.glass-card',
        scale: [0.9, 1],
        opacity: [0, 1],
        delay: anime.stagger(100), // Aparece um por um
        duration: 800,
        easing: 'easeOutElastic(1, .8)'
    });
}

function logout() {
    location.reload(); // Simples refresh para voltar ao login
}