/**
 * Select Module
 * Vanilla JS replacement for select2 functionality
 * Uses Choices.js as a modern alternative
 */

export function mountSelect(root = document) {
    const selectElements = root.querySelectorAll('.select2');
    
    if (selectElements.length === 0) {
        console.warn('Select: No select elements found');
        return;
    }

    // Check if Choices.js is available
    if (typeof Choices === 'undefined') {
        console.warn('Select: Choices.js not available, using native select');
        return;
    }

    selectElements.forEach(select => {
        try {
            new Choices(select, {
                searchEnabled: true,
                searchChoices: true,
                searchResultLimit: 10,
                searchPlaceholderValue: 'جستجو...',
                noResultsText: 'نتیجه‌ای یافت نشد',
                noChoicesText: 'گزینه‌ای موجود نیست',
                itemSelectText: 'برای انتخاب کلیک کنید',
                loadingText: 'در حال بارگذاری...',
                removeItemButton: true,
                shouldSort: false,
                shouldSortChoices: false,
                placeholder: true,
                placeholderValue: select.getAttribute('placeholder') || 'انتخاب کنید...',
                classNames: {
                    containerOuter: 'choices choices--rtl',
                    containerInner: 'choices__inner',
                    input: 'choices__input',
                    inputCloned: 'choices__input--cloned',
                    list: 'choices__list',
                    listItems: 'choices__list--multiple',
                    listSingle: 'choices__list--single',
                    listDropdown: 'choices__list--dropdown',
                    item: 'choices__item',
                    itemSelectable: 'choices__item--selectable',
                    itemDisabled: 'choices__item--disabled',
                    itemChoice: 'choices__item--choice',
                    placeholder: 'choices__placeholder',
                    group: 'choices__group',
                    groupHeading: 'choices__heading',
                    button: 'choices__button',
                    activeState: 'is-active',
                    focusState: 'is-focused',
                    openState: 'is-open',
                    disabledState: 'is-disabled',
                    highlightedState: 'is-highlighted',
                    selectedState: 'is-selected',
                    flippedState: 'is-flipped',
                    loadingState: 'is-loading',
                    noResults: 'has-no-results',
                    noChoices: 'has-no-choices'
                }
            });
        } catch (error) {
            console.error('Error initializing select:', error);
        }
    });

    console.log('Select module initialized');
}
