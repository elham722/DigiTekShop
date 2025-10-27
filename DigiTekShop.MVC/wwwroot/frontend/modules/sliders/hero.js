/**
 * Hero Slider Module
 * Swiper-based hero slider functionality
 */

export function mountHeroSlider(context = {}) {
    const { Swiper } = context;
    
    if (!Swiper) {
        console.warn('Hero slider: Swiper not available');
        return;
    }

    // Main hero slider
    const mainSlider = document.querySelector('.main-swiper-slider');
    if (mainSlider) {
        new Swiper(mainSlider, {
            slidesPerView: 1,
            spaceBetween: 10,
            loop: true,
            effect: 'fade',
            fadeEffect: {
                crossFade: true,
            },
            autoplay: {
                delay: 2500,
                disableOnInteraction: false,
            },
            pagination: {
                el: '.swiper-pagination',
                clickable: true,
                dynamicBullets: true,
            },
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
        });
    }

    // Mini single slider
    const miniSlider = document.querySelector('.mini-single-swiper-slider');
    if (miniSlider) {
        new Swiper(miniSlider, {
            slidesPerView: 1,
            spaceBetween: 10,
            autoplay: {
                delay: 5000,
                disableOnInteraction: false,
            },
            pagination: {
                el: '.swiper-pagination',
                clickable: true,
            },
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
        });
    }

    // Notification slider
    const notificationSlider = document.querySelector('.notification-swiper-slider');
    if (notificationSlider) {
        new Swiper(notificationSlider, {
            slidesPerView: 1,
            spaceBetween: 10,
            direction: 'vertical',
            autoplay: {
                delay: 5000,
                disableOnInteraction: false,
            },
        });
    }

    console.log('Hero slider module initialized');
}
