/**
 * Price Filter Module
 * noUiSlider-based price filter functionality
 */

export function mountPriceFilter(context = {}) {
    const { noUiSlider } = context;
    
    if (!noUiSlider) {
        console.warn('Price filter: noUiSlider not available');
        return;
    }

    const filterPrice = document.querySelector('.filter-price');
    if (!filterPrice) return;

    const sliderElement = document.getElementById('slider-non-linear-step');
    const sliderFrom = document.querySelector('.js-slider-range-from');
    const sliderTo = document.querySelector('.js-slider-range-to');

    if (!sliderElement || !sliderFrom || !sliderTo) {
        console.warn('Price filter: Required elements not found');
        return;
    }

    const min = parseInt(sliderFrom.dataset.range);
    const max = parseInt(sliderTo.dataset.range);

    // Create the slider
    noUiSlider.create(sliderElement, {
        start: [sliderFrom.value, sliderTo.value],
        connect: true,
        direction: 'rtl',
        format: {
            to: function (value) {
                return Math.round(value).toLocaleString('fa-IR');
            },
            from: function (value) {
                return parseInt(value.replace(/,/g, ''));
            }
        },
        step: 1,
        range: {
            min: min,
            max: max,
        },
    });

    // Update input values when slider changes
    const skipValues = [
        document.getElementById('skip-value-lower'),
        document.getElementById('skip-value-upper'),
    ];

    sliderElement.noUiSlider.on('update', function (values, handle) {
        if (skipValues[handle]) {
            skipValues[handle].value = values[handle];
        }
    });

    // Update slider when input values change
    sliderFrom.addEventListener('change', function () {
        sliderElement.noUiSlider.set([this.value, null]);
    });

    sliderTo.addEventListener('change', function () {
        sliderElement.noUiSlider.set([null, this.value]);
    });

    console.log('Price filter module initialized');
}
