/**
 * Gallery Slider Module
 * Swiper-based gallery slider functionality
 */

export function mountGallerySlider(context = {}) {
    const { Swiper } = context;
    
    if (!Swiper) {
        console.warn('Gallery slider: Swiper not available');
        return;
    }

    const gallerySlider = document.querySelector('.gallery-swiper-slider');
    const galleryThumbsSlider = document.querySelector('.gallery-thumbs-swiper-slider');

    if (gallerySlider && galleryThumbsSlider) {
        // Main gallery slider
        const gallerySwiper = new Swiper(gallerySlider, {
            centeredSlides: true,
        });

        // Thumbnail slider
        const galleryThumbsSwiper = new Swiper(galleryThumbsSlider, {
            slidesPerView: 4,
            slideToClickedSlide: true,
            centeredSlides: true,
            spaceBetween: 15,
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
        });

        // Connect the sliders
        gallerySwiper.controller.control = galleryThumbsSwiper;
        galleryThumbsSwiper.controller.control = gallerySwiper;
    }

    console.log('Gallery slider module initialized');
}
