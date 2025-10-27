/**
 * Search functionality fix
 * Handles search field and button interactions
 */
document.addEventListener('DOMContentLoaded', function() {
    // Find search elements
    const searchContainer = document.querySelector('.page-header .search-container');
    if (!searchContainer) return;
    
    const searchField = searchContainer.querySelector('.search-field');
    const btnAction = searchContainer.querySelector('.btn-action');
    const btnClose = searchContainer.querySelector('.btn-close-search-result');
    const resultsContainer = searchContainer.querySelector('.search-result-container');
    
    if (!searchField || !btnAction || !btnClose || !resultsContainer) return;
    
    // Open function
    function openSearch() {
        btnAction.classList.add('d-none');
        btnClose.classList.remove('d-none');
        resultsContainer.classList.add('show');
        searchField.focus();
    }
    
    // Close function
    function closeSearch() {
        btnClose.classList.add('d-none');
        btnAction.classList.remove('d-none');
        resultsContainer.classList.remove('show');
        searchField.value = '';
    }
    
    // Event listeners
    searchField.addEventListener('click', openSearch);
    searchField.addEventListener('focus', openSearch);
    btnAction.addEventListener('click', function(e) {
        e.preventDefault();
        openSearch();
    });
    btnClose.addEventListener('click', function(e) {
        e.preventDefault();
        closeSearch();
    });
    
    // Close on outside click
    document.addEventListener('click', function(e) {
        if (!searchContainer.contains(e.target)) {
            closeSearch();
        }
    });
    
    // Close on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && resultsContainer.classList.contains('show')) {
            closeSearch();
        }
    });
    
    console.log('Search functionality initialized');
});
