(function () {
  "use strict";
  let SCRIPT = {};

  /*====== Preloader ======*/
  window.addEventListener("load", function () {
    let preloaderFadeOutTime = 500;

    setTimeout(function () {
      document.body.classList.add("loaded");
    }, preloaderFadeOutTime);
  });
  /*====== end Preloader ======*/

  /*====== Header ======*/
  SCRIPT.Header = function () {
    // Search field click handler
    document.querySelectorAll(".search-container .search-form .search-field").forEach(function(element) {
      element.addEventListener("click", function () {
        let parents = this.closest(".search-container");
        parents.querySelector(".btn-search").classList.add("d-none");
        parents.querySelector(".btn-close-search-result").classList.remove("d-none");
        parents.querySelector(".search-result-container").classList.add("show");
      });
    });

    // Close search result handler
    document.querySelectorAll(".search-container .search-form .btn-close-search-result").forEach(function(element) {
      element.addEventListener("click", function (e) {
        e.preventDefault();
        let parents = this.closest(".search-container");
        this.classList.add("d-none");
        parents.querySelector(".btn-search").classList.remove("d-none");
        parents.querySelector(".search-result-container").classList.remove("show");
      });
    });

    // Toggle navigation handler
    document.querySelectorAll(".toggle-navigation").forEach(function(element) {
      element.addEventListener("click", function () {
        document.querySelector(".navigation").classList.add("toggle");
        let overlay = document.querySelector(".navigation-overlay");
        overlay.style.display = "block";
        overlay.style.opacity = "1";
      });
    });

    // Toggle submenu handler
    document.querySelectorAll(".navigation .toggle-submenu").forEach(function(element) {
      element.addEventListener("click", function (event) {
        event.preventDefault();
        this.parentElement.querySelector(".submenu").classList.add("toggle");
      });
    });

    // Close submenu handler
    document.querySelectorAll(".navigation .close-submenu").forEach(function(element) {
      element.addEventListener("click", function (event) {
        event.preventDefault();
        this.parentElement.classList.remove("toggle");
      });
    });

    // Navigation overlay and close navigation handler
    document.querySelectorAll(".navigation-overlay, .close-navigation").forEach(function(element) {
      element.addEventListener("click", function (event) {
        event.preventDefault();
        document.querySelector(".navigation").classList.remove("toggle");
        document.querySelectorAll(".navigation .submenu").forEach(function(submenu) {
          submenu.classList.remove("toggle");
        });
        let overlay = document.querySelector(".navigation-overlay");
        overlay.style.opacity = "0";
        setTimeout(function() {
          overlay.style.display = "none";
        }, 100);
      });
    });
  };
  /*====== end Header ======*/

  /*====== VerticalMenu ======*/
  SCRIPT.VerticalMenu = function () {
    document.querySelectorAll(".vertical-menu-items>ul>li").forEach(function(element) {
      element.addEventListener("mouseenter", function () {
        this.classList.add("show");
        let siblings = Array.from(this.parentElement.children);
        siblings.forEach(function(sibling) {
          if (sibling !== this) {
            sibling.classList.remove("show");
          }
        }.bind(this));
      });
    });
  };
  /*====== end VerticalMenu ======*/

  /*====== MegaSearch ======*/
  SCRIPT.MegaSearch = function () {
    document.querySelectorAll(".user-option-btn--search").forEach(function(element) {
      element.addEventListener("click", function (e) {
        e.preventDefault();
        document.querySelector(".mega-search-container").classList.add("show");
      });
    });

    document.querySelectorAll(".mega-search-container .mega-search-box-close, .mega-search-container .mega-search-overlay").forEach(function(element) {
      element.addEventListener("click", function () {
        document.querySelector(".mega-search-container").classList.remove("show");
      });
    });
  };
  /*====== end MegaSearch ======*/

  /*====== Countdown ======*/
  SCRIPT.Countdown = function () {
    // Note: jQuery countdown plugin needs to be replaced with vanilla JS countdown
    // This is a placeholder for the countdown functionality
    let verifyCodeTimer = document.getElementById("timer--verify-code");
    if (verifyCodeTimer) {
      // Implement vanilla JS countdown here
      console.log("Countdown functionality needs vanilla JS implementation");
    }

    document.querySelectorAll(".verify-code-wrapper .send-again").forEach(function(element) {
      element.addEventListener("click", function (event) {
        event.preventDefault();
        this.classList.remove("d-inline-block");
        let timer = document.getElementById("timer--verify-code");
        if (timer) {
          timer.innerHTML = "";
          // Restart timer logic here
        }
      });
    });

    // Countdown elements
    document.querySelectorAll("[data-countdown]").forEach(function(element) {
      let finalDate = element.dataset.countdown;
      // Implement vanilla JS countdown here
      console.log("Countdown for:", finalDate);
    });
  };
  /*====== end Countdown ======*/

  /*====== Swiper ======*/
  SCRIPT.Swiper = function () {
    // Notification swiper
    if (document.querySelector(".notification-swiper-slider")) {
      const notificationSwiperSlider = new Swiper(".notification-swiper-slider", {
        direction: "rtl",
        loop: true,
        autoplay: {
          delay: 3000,
          disableOnInteraction: false,
        },
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
        },
      });
    }

    // Product swiper
    if (document.querySelector(".product-swiper-slider")) {
      const productSwiperSlider = new Swiper(".product-swiper-slider", {
        direction: "rtl",
        loop: true,
        autoplay: {
          delay: 3000,
          disableOnInteraction: false,
        },
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
        },
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
      });
    }

    // Gallery swiper
    if (document.querySelector(".gallery-swiper-slider")) {
      const gallerySwiperSlider = new Swiper(".gallery-swiper-slider", {
        direction: "rtl",
        loop: true,
        autoplay: {
          delay: 3000,
          disableOnInteraction: false,
        },
        pagination: {
          el: ".swiper-pagination",
          clickable: true,
        },
        navigation: {
          nextEl: ".swiper-button-next",
          prevEl: ".swiper-button-prev",
        },
      });
    }
  };
  /*====== end Swiper ======*/

  /*====== SimpleBar ======*/
  SCRIPT.SimpleBar = function () {
    document.querySelectorAll(".do-simplebar").forEach(function(element) {
      new SimpleBar(element, {
        autoHide: false,
        scrollbarMinSize: 100,
      });
    });
  };
  /*====== end SimpleBar ======*/

  /*====== Zoom ======*/
  SCRIPT.Zoom = function () {
    if (window.innerWidth > 768) {
      document.querySelectorAll(".zoom-img").forEach(function(element) {
        // Note: imagezoomsl plugin needs to be replaced with vanilla JS or alternative
        console.log("Zoom functionality needs vanilla JS implementation");
      });
    }
  };
  /*====== end Zoom ======*/

  /*====== ReadMore ======*/
  SCRIPT.ReadMore = function () {
    document.querySelectorAll(".expandable-text-expand-btn").forEach(function(element) {
      element.addEventListener("click", function () {
        let contentFixedHeight = this.closest(".expandable-text");
        contentFixedHeight.classList.toggle("active");
        this.querySelector(".show-more").classList.toggle("d-none");
        this.querySelector(".show-less").classList.toggle("d-none");
      });
    });
  };
  /*====== end ReadMore ======*/

  /*====== ShopView ======*/
  SCRIPT.ShopView = function () {
    document.querySelectorAll(".btn-list-view").forEach(function(element) {
      element.addEventListener("click", function () {
        document.querySelectorAll(".listing-products-content .product-card-container").forEach(function(card) {
          card.classList.remove("col-xl-3", "col-lg-4", "col-md-6", "col-sm-6");
          card.classList.add("col-lg-6", "col-md-12", "col-sm-6");
        });
        document.querySelectorAll(".listing-products-content .product-card-container .product-card").forEach(function(card) {
          card.classList.add("product-card--list");
        });
      });
    });

    document.querySelectorAll(".btn-grid-view").forEach(function(element) {
      element.addEventListener("click", function () {
        document.querySelectorAll(".listing-products-content .product-card-container").forEach(function(card) {
          card.classList.remove("col-lg-6", "col-md-12", "col-sm-6");
          card.classList.add("col-xl-3", "col-lg-4", "col-md-6", "col-sm-6");
        });
        document.querySelectorAll(".listing-products-content .product-card-container .product-card").forEach(function(card) {
          card.classList.remove("product-card--list");
        });
      });
    });
  };
  /*====== end ShopView ======*/

  /*====== Form ======*/
  SCRIPT.Form = function () {
    // Select2 replacement
    if (document.querySelectorAll(".select2").length > 0) {
      document.querySelectorAll(".select2").forEach(function(element) {
        new Select2(element, {
          dir: "rtl",
        });
      });
    }

    // Input code functionality
    document.querySelectorAll(".form-input-code-container .input-code").forEach(function(element) {
      element.addEventListener("keyup", function (e) {
        if (this.value.length === this.maxLength) {
          let next = this.dataset.next;
          let nextElement = document.getElementById("input-code-" + next);
          if (nextElement) {
            nextElement.focus();
          }
        }
      });
    });

    // Custom radio circle label
    document.querySelectorAll(".custom-radio-circle-label").forEach(function(element) {
      element.addEventListener("click", function () {
        let label = this.dataset.variantLabel;
        document.querySelector(".product-variant-selected").textContent = label;
      });
    });
  };
  /*====== end Form ======*/

  /*====== Quantity ======*/
  SCRIPT.Quantity = function () {
    document.querySelectorAll(".num-in span").forEach(function(element) {
      element.addEventListener("click", function () {
        let input = this.closest(".num-block").querySelector("input.in-num");
        if (this.classList.contains("minus")) {
          let count = parseFloat(input.value) - 1;
          if (count < 2) {
            this.classList.add("dis");
          } else {
            this.classList.remove("dis");
          }
          input.value = count;
        } else {
          let count = parseFloat(input.value) + 1;
          if (count > 1) {
            this.closest(".num-block").querySelector(".minus").classList.remove("dis");
          }
          input.value = count;
        }
      });
    });
  };
  /*====== end Quantity ======*/

  /*====== FilterPrice ======*/
  SCRIPT.FilterPrice = function () {
    let skipSlider = document.getElementById("slider-non-linear-step");
    if (skipSlider) {
      let skipValues = [
        document.getElementById("skip-value-lower"),
        document.getElementById("skip-value-upper")
      ];

      noUiSlider.create(skipSlider, {
        start: [10, 90],
        connect: true,
        range: {
          min: 0,
          max: 100
        }
      });

      skipSlider.noUiSlider.on("update", function (values, handle) {
        skipValues[handle].value = values[handle];
      });
    }
  };
  /*====== end FilterPrice ======*/

  /*====== Tooltip ======*/
  SCRIPT.Tooltip = function () {
    // Bootstrap 5 tooltips
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function(element) {
      new bootstrap.Tooltip(element);
    });
  };
  /*====== end Tooltip ======*/

  /*====== SmoothScroll ======*/
  SCRIPT.SmoothScroll = function () {
    let links = document.querySelectorAll(".product-tabs a.nav-link");

    // Move to specific section when click on menu link
    links.forEach(function(link) {
      link.addEventListener("click", function (e) {
        let target = document.querySelector(this.getAttribute("href"));
        if (target) {
          target.scrollIntoView({
            behavior: "smooth",
            block: "start"
          });
        }
        this.classList.add("active");
        e.preventDefault();
      });
    });

    // Run the scrNav when scroll
    window.addEventListener("scroll", function () {
      scrNav();
    });

    function scrNav() {
      let sTop = window.pageYOffset;
      document.querySelectorAll(".tab-content").forEach(function(element) {
        let id = element.id;
        let offset = element.offsetTop - 1;
        let height = element.offsetHeight;
        if (sTop >= offset && sTop < offset + height) {
          links.forEach(function(link) {
            link.classList.remove("active");
          });
          let activeLink = document.querySelector(".product-tabs [data-scroll='" + id + "']");
          if (activeLink) {
            activeLink.classList.add("active");
          }
        }
      });
    }
  };
  /*====== end SmoothScroll ======*/

  /*====== Box ======*/
  SCRIPT.Box = function () {
    document.querySelectorAll("[data-btn-box]").forEach(function(element) {
      element.addEventListener("click", function (event) {
        event.preventDefault();
        let parent = this.dataset.parent;
        let target = this.dataset.target;
        document.querySelector(parent).classList.add("d-none");
        document.querySelector(target).classList.remove("d-none");
      });
    });

    document.querySelectorAll("[data-btn-box-close]").forEach(function(element) {
      element.addEventListener("click", function (event) {
        event.preventDefault();
        let parent = this.dataset.parent;
        let show = this.dataset.show;
        document.querySelector(parent).classList.add("d-none");
        document.querySelector(show).classList.remove("d-none");
      });
    });

    document.querySelectorAll(".toggle-responsive-sidebar").forEach(function(element) {
      element.addEventListener("click", function (e) {
        e.preventDefault();
        document.querySelector(".responsive-sidebar").classList.add("show");
        document.querySelector(".responsive-sidebar-overlay").classList.add("show");
      });
    });

    document.querySelectorAll(".responsive-sidebar-overlay").forEach(function(element) {
      element.addEventListener("click", function (e) {
        document.querySelector(".responsive-sidebar").classList.remove("show");
        this.classList.remove("show");
      });
    });
  };
  /*====== end Box ======*/

  /*====== Sticky ======*/
  SCRIPT.Sticky = function () {
    let productsList = document.querySelector(".compare-container .compare-list");
    if (productsList) {
      let top = productsList.offsetTop;

      window.addEventListener("scroll", function () {
        if (window.pageYOffset >= top - 100) {
          productsList.classList.add("is-sticky");
        } else {
          productsList.classList.remove("is-sticky");
        }
      });
    }
  };
  /*====== end Sticky ======*/

  /*====== Copy ======*/
  SCRIPT.Copy = function () {
    document.querySelectorAll(".copy-url-btn").forEach(function(element) {
      element.addEventListener("click", function () {
        copyClipboard(this.dataset.copy);
        this.classList.add("copied");
        this.innerHTML = "کپی شد";
        setTimeout(function () {
          this.classList.remove("copied");
          this.innerHTML = "کپی";
        }.bind(this), 2000);
      });
    });
  };
  /*====== end Copy ======*/

  /*====== Toast ======*/
  SCRIPT.Toast = function () {
    document.querySelectorAll("[data-toast]").forEach(function(element) {
      element.addEventListener("click", function (event) {
        event.preventDefault();
        let toastType = this.dataset.toastType;
        let toastMessage = this.dataset.toastMessage;
        
        // Use Bootstrap 5 toast or iziToast
        if (typeof iziToast !== 'undefined') {
          iziToast.show({
            title: 'Success',
            message: toastMessage,
            color: toastType === 'success' ? 'green' : 'red',
            position: 'topRight',
            rtl: true,
          });
        }
      });
    });
  };
  /*====== end Toast ======*/

  /*====== AddComment ======*/
  SCRIPT.AddComment = function () {
    let addCommentProduct = document.querySelector(".add-comment-product");
    if (addCommentProduct) {
      let inputs = document.querySelectorAll("#advantage-input, #disadvantage-input");
      
      function inputChangeCallback() {
        if (this.value.trim().length > 0) {
          this.classList.add("has-value");
        } else {
          this.classList.remove("has-value");
        }
      }

      inputs.forEach(function(input) {
        inputChangeCallback.call(input);
        input.addEventListener("change", inputChangeCallback);
        input.addEventListener("keyup", inputChangeCallback);
      });

      // Advantages functionality
      document.querySelectorAll("#advantages .js-icon-form-add").forEach(function(element) {
        element.addEventListener("click", function (e) {
          let parent = document.querySelector(".js-advantages-list");
          if (parent.querySelectorAll(".js-advantage-item").length >= 5) {
            return;
          }
          let advantageInput = document.getElementById("advantage-input");
          if (advantageInput.value.trim().length > 0) {
            let newItem = document.createElement("div");
            newItem.className = "js-advantage-item";
            newItem.innerHTML = `
              <span>${advantageInput.value}</span>
              <i class="js-icon-form-remove ri-close-line"></i>
            `;
            parent.appendChild(newItem);
            advantageInput.value = "";
          }
        });
      });

      // Remove advantage items
      document.querySelectorAll("#advantages .js-icon-form-remove").forEach(function(element) {
        element.addEventListener("click", function (e) {
          this.parentElement.remove();
        });
      });

      // Similar functionality for disadvantages...
    }
  };
  /*====== end AddComment ======*/

  // Initialize all functions when DOM is ready
  document.addEventListener("DOMContentLoaded", function () {
    SCRIPT.Header();
    SCRIPT.VerticalMenu();
    SCRIPT.MegaSearch();
    SCRIPT.Countdown();
    SCRIPT.Swiper();
    SCRIPT.SimpleBar();
    SCRIPT.Zoom();
    SCRIPT.ReadMore();
    SCRIPT.ShopView();
    SCRIPT.Form();
    SCRIPT.Quantity();
    SCRIPT.FilterPrice();
    SCRIPT.Tooltip();
    SCRIPT.SmoothScroll();
    SCRIPT.Box();
    SCRIPT.Sticky();
    SCRIPT.Copy();
    SCRIPT.Toast();
    SCRIPT.AddComment();
  });

})();