/**
 * Checkout Slider Module
 * Swiper-based checkout slider functionality
 */

export function mountCheckoutSlider(context = {}) {
    const { Swiper } = context;
    
    if (!Swiper) {
        console.warn('Checkout slider: Swiper not available');
        return;
    }

    // Compare slider
    const compareSlider = document.querySelector('.compare-swiper-slider');
    if (compareSlider) {
        new Swiper(compareSlider, {
            spaceBetween: 10,
            slidesPerView: 'auto',
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
        });
    }

    // Checkout pack slider
    const checkoutPackSlider = document.querySelector('.checkout-pack-swiper-slider');
    if (checkoutPackSlider) {
        new Swiper(checkoutPackSlider, {
            spaceBetween: 10,
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
            breakpoints: {
                1200: {
                    slidesPerView: 6,
                },
                1090: {
                    slidesPerView: 5,
                },
                768: {
                    slidesPerView: 4,
                    spaceBetween: 10,
                },
                576: {
                    slidesPerView: 3,
                    spaceBetween: 10,
                },
                480: {
                    slidesPerView: 2,
                    spaceBetween: 8,
                },
            },
        });
    }

    // Checkout time slider
    const checkoutTimeSlider = document.querySelector('.checkout-time-swiper-slider');
    if (checkoutTimeSlider) {
        new Swiper(checkoutTimeSlider, {
            slidesPerView: 'auto',
            spaceBetween: 10,
            freeMode: true,
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
        });
    }

    console.log('Checkout slider module initialized');
}
