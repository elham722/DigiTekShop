/**
 * Product Slider Module
 * Swiper-based product slider functionality
 */

export function mountProductSlider(context = {}) {
    const { Swiper } = context;
    
    if (!Swiper) {
        console.warn('Product slider: Swiper not available');
        return;
    }

    // Product slider
    const productSlider = document.querySelector('.product-swiper-slider');
    if (productSlider) {
        new Swiper(productSlider, {
            spaceBetween: 10,
            pagination: {
                el: '.swiper-pagination',
                clickable: true,
                dynamicBullets: true,
            },
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
            breakpoints: {
                1200: {
                    slidesPerView: 5,
                },
                1090: {
                    slidesPerView: 4,
                },
                768: {
                    slidesPerView: 3,
                    spaceBetween: 10,
                },
                576: {
                    slidesPerView: 2,
                    spaceBetween: 10,
                },
                480: {
                    slidesPerView: 1,
                    spaceBetween: 8,
                },
            },
        });
    }

    // Product specials slider
    const productSpecialsSlider = document.querySelector('.product-specials-swiper-slider');
    if (productSpecialsSlider) {
        new Swiper(productSpecialsSlider, {
            spaceBetween: 10,
            pagination: {
                el: '.swiper-pagination',
                clickable: true,
                dynamicBullets: true,
            },
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
            breakpoints: {
                1200: {
                    slidesPerView: 4,
                },
                992: {
                    slidesPerView: 3,
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

    // Category slider
    const categorySlider = document.querySelector('.category-swiper-slider');
    if (categorySlider) {
        new Swiper(categorySlider, {
            spaceBetween: 10,
            pagination: {
                el: '.swiper-pagination',
                clickable: true,
                dynamicBullets: true,
            },
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
            breakpoints: {
                1200: {
                    slidesPerView: 7,
                },
                1090: {
                    slidesPerView: 6,
                },
                768: {
                    slidesPerView: 5,
                    spaceBetween: 10,
                },
                576: {
                    slidesPerView: 4,
                    spaceBetween: 10,
                },
                480: {
                    slidesPerView: 3,
                    spaceBetween: 8,
                },
                0: {
                    slidesPerView: 2,
                    spaceBetween: 8,
                },
            },
        });
    }

    console.log('Product slider module initialized');
}
