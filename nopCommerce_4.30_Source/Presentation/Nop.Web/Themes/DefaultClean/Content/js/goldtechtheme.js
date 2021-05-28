function slideNavOpen() {
    var body = document.body;
    var slideNavButtons = document.querySelectorAll('.slide-nav-toggle');

    if (slideNavButtons.length > 0) {
        slideNavButtons.forEach(slideNavButton => {
            slideNavButton.addEventListener('click', slideNavClick)
        });
    }
    function slideNavClick(e) {
        e.preventDefault()
        body.classList.toggle('slide-nav-open');
    }
}
slideNavOpen();

function searchBoxOpen() {
    var body = document.body;
    var searchBoxToggles = document.querySelectorAll('.header-search-toggle');

    if (searchBoxToggles.length > 0) {
        searchBoxToggles.forEach(searchBoxToggle => {
            searchBoxToggle.addEventListener('click', searchNavClick)
        });
    }
    function searchNavClick(e) {
        e.preventDefault()
        body.classList.toggle('store-search-box-open');
    }
}
searchBoxOpen();