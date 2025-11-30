/**
 * Jalali DatePicker - تقویم فارسی مدرن
 * قابل استفاده در کل پروژه
 * 
 * Usage:
 *   JalaliDatePicker.init('#myInput', {
 *       onSelect: function(jalaliDate, gregorianDate) {
 *           console.log('Selected:', jalaliDate, '->', gregorianDate);
 *       }
 *   });
 * 
 * Or with jQuery:
 *   $('#myInput').jalaliDatePicker({ onSelect: ... });
 */

(function(global) {
    'use strict';

    // روزهای هفته فارسی
    const weekDays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];
    const weekDaysFull = ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنجشنبه', 'جمعه'];
    
    // ماه‌های فارسی
    const months = [
        'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
        'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
    ];

    // تعداد روزهای هر ماه شمسی
    const monthDays = [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29];

    // تبدیل میلادی به شمسی
    function toJalaali(gy, gm, gd) {
        return jalaali.toJalaali(gy, gm, gd);
    }

    // تبدیل شمسی به میلادی
    function toGregorian(jy, jm, jd) {
        return jalaali.toGregorian(jy, jm, jd);
    }

    // آیا سال کبیسه است؟
    function isLeapJalaaliYear(jy) {
        return jalaali.isLeapJalaaliYear(jy);
    }

    // تعداد روزهای ماه
    function jalaaliMonthLength(jy, jm) {
        if (jm <= 6) return 31;
        if (jm <= 11) return 30;
        return isLeapJalaaliYear(jy) ? 30 : 29;
    }

    // روز هفته برای اولین روز ماه (0 = شنبه)
    function getFirstDayOfMonth(jy, jm) {
        const g = toGregorian(jy, jm, 1);
        const date = new Date(g.gy, g.gm - 1, g.gd);
        // تبدیل به روز هفته ایرانی (شنبه = 0)
        let day = date.getDay();
        // در JS: 0 = یکشنبه, 6 = شنبه
        // ما می‌خواهیم: 0 = شنبه, 1 = یکشنبه, ...
        return (day + 1) % 7;
    }

    // تاریخ امروز به شمسی
    function getTodayJalali() {
        const today = new Date();
        return toJalaali(today.getFullYear(), today.getMonth() + 1, today.getDate());
    }

    // فرمت تاریخ
    function formatDate(jy, jm, jd) {
        const month = String(jm).padStart(2, '0');
        const day = String(jd).padStart(2, '0');
        return `${jy}/${month}/${day}`;
    }

    // فرمت تاریخ میلادی
    function formatGregorian(gy, gm, gd) {
        const month = String(gm).padStart(2, '0');
        const day = String(gd).padStart(2, '0');
        return `${gy}-${month}-${day}`;
    }

    // کلاس اصلی DatePicker
    class JalaliDatePicker {
        constructor(input, options = {}) {
            this.input = typeof input === 'string' ? document.querySelector(input) : input;
            if (!this.input) {
                console.error('JalaliDatePicker: Input element not found');
                return;
            }

            this.options = {
                onSelect: options.onSelect || function() {},
                minDate: options.minDate || null,
                maxDate: options.maxDate || null,
                format: options.format || 'YYYY/MM/DD',
                placeholder: options.placeholder || 'انتخاب تاریخ',
                theme: options.theme || 'light', // light, dark
                position: options.position || 'bottom', // top, bottom
                autoClose: options.autoClose !== false,
                showTodayBtn: options.showTodayBtn !== false,
                gregorianField: options.gregorianField || null // hidden field for gregorian date
            };

            this.isOpen = false;
            this.selectedDate = null;
            
            const today = getTodayJalali();
            this.viewYear = today.jy;
            this.viewMonth = today.jm;

            this.init();
        }

        init() {
            this.createPicker();
            this.bindEvents();
            
            // Set placeholder
            if (!this.input.placeholder) {
                this.input.placeholder = this.options.placeholder;
            }
            
            // Make input readonly to prevent manual typing issues
            this.input.setAttribute('readonly', 'readonly');
            this.input.style.cursor = 'pointer';
            this.input.style.backgroundColor = '#fff';
        }

        createPicker() {
            // Create container
            this.picker = document.createElement('div');
            this.picker.className = `jalali-datepicker ${this.options.theme}`;
            this.picker.innerHTML = `
                <div class="jdp-header">
                    <button type="button" class="jdp-nav jdp-prev-year" title="سال قبل">«</button>
                    <button type="button" class="jdp-nav jdp-prev-month" title="ماه قبل">‹</button>
                    <span class="jdp-title"></span>
                    <button type="button" class="jdp-nav jdp-next-month" title="ماه بعد">›</button>
                    <button type="button" class="jdp-nav jdp-next-year" title="سال بعد">»</button>
                </div>
                <div class="jdp-weekdays"></div>
                <div class="jdp-days"></div>
                ${this.options.showTodayBtn ? '<div class="jdp-footer"><button type="button" class="jdp-today-btn">امروز</button></div>' : ''}
            `;
            
            document.body.appendChild(this.picker);
            
            // Render weekdays
            const weekdaysEl = this.picker.querySelector('.jdp-weekdays');
            weekdaysEl.innerHTML = weekDays.map(d => `<span>${d}</span>`).join('');
        }

        bindEvents() {
            // Open on input click/focus
            this.input.addEventListener('click', () => this.open());
            this.input.addEventListener('focus', () => this.open());

            // Navigation
            this.picker.querySelector('.jdp-prev-year').addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.changeYear(-1);
            });
            this.picker.querySelector('.jdp-next-year').addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.changeYear(1);
            });
            this.picker.querySelector('.jdp-prev-month').addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.changeMonth(-1);
            });
            this.picker.querySelector('.jdp-next-month').addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.changeMonth(1);
            });

            // Today button
            const todayBtn = this.picker.querySelector('.jdp-today-btn');
            if (todayBtn) {
                todayBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.selectToday();
                });
            }

            // Day selection
            this.picker.querySelector('.jdp-days').addEventListener('click', (e) => {
                if (e.target.classList.contains('jdp-day') && !e.target.classList.contains('disabled')) {
                    const day = parseInt(e.target.dataset.day, 10);
                    this.selectDate(this.viewYear, this.viewMonth, day);
                }
            });

            // Close on outside click
            document.addEventListener('click', (e) => {
                if (this.isOpen && !this.picker.contains(e.target) && e.target !== this.input) {
                    this.close();
                }
            });

            // Close on escape
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && this.isOpen) {
                    this.close();
                }
            });
        }

        open() {
            if (this.isOpen) return;
            
            this.isOpen = true;
            this.picker.classList.add('open');
            this.render();
            this.position();
        }

        close() {
            this.isOpen = false;
            this.picker.classList.remove('open');
        }

        position() {
            const rect = this.input.getBoundingClientRect();
            const pickerHeight = this.picker.offsetHeight;
            const windowHeight = window.innerHeight;
            
            let top = rect.bottom + window.scrollY + 5;
            let left = rect.left + window.scrollX;
            
            // Check if picker goes below viewport
            if (rect.bottom + pickerHeight > windowHeight) {
                top = rect.top + window.scrollY - pickerHeight - 5;
            }
            
            // Check if picker goes outside right edge
            const pickerWidth = this.picker.offsetWidth;
            if (left + pickerWidth > window.innerWidth) {
                left = window.innerWidth - pickerWidth - 10;
            }
            
            this.picker.style.top = `${top}px`;
            this.picker.style.left = `${left}px`;
        }

        render() {
            // Update title
            const title = this.picker.querySelector('.jdp-title');
            title.textContent = `${months[this.viewMonth - 1]} ${this.viewYear}`;
            
            // Render days
            const daysEl = this.picker.querySelector('.jdp-days');
            const firstDay = getFirstDayOfMonth(this.viewYear, this.viewMonth);
            const daysInMonth = jalaaliMonthLength(this.viewYear, this.viewMonth);
            const today = getTodayJalali();
            
            let html = '';
            
            // Empty cells before first day
            for (let i = 0; i < firstDay; i++) {
                html += '<span class="jdp-day empty"></span>';
            }
            
            // Days
            for (let day = 1; day <= daysInMonth; day++) {
                const isToday = this.viewYear === today.jy && 
                               this.viewMonth === today.jm && 
                               day === today.jd;
                const isSelected = this.selectedDate &&
                                  this.viewYear === this.selectedDate.jy &&
                                  this.viewMonth === this.selectedDate.jm &&
                                  day === this.selectedDate.jd;
                
                let classes = 'jdp-day';
                if (isToday) classes += ' today';
                if (isSelected) classes += ' selected';
                
                // Friday (index 6 in our weekdays)
                const dayIndex = (firstDay + day - 1) % 7;
                if (dayIndex === 6) classes += ' friday';
                
                html += `<span class="${classes}" data-day="${day}">${day}</span>`;
            }
            
            daysEl.innerHTML = html;
        }

        changeMonth(delta) {
            this.viewMonth += delta;
            if (this.viewMonth > 12) {
                this.viewMonth = 1;
                this.viewYear++;
            } else if (this.viewMonth < 1) {
                this.viewMonth = 12;
                this.viewYear--;
            }
            this.render();
        }

        changeYear(delta) {
            this.viewYear += delta;
            this.render();
        }

        selectDate(jy, jm, jd) {
            this.selectedDate = { jy, jm, jd };
            
            const jalaliStr = formatDate(jy, jm, jd);
            const g = toGregorian(jy, jm, jd);
            const gregorianStr = formatGregorian(g.gy, g.gm, g.gd);
            
            // Update input
            this.input.value = jalaliStr;
            
            // Update hidden gregorian field if exists
            if (this.options.gregorianField) {
                const hiddenField = typeof this.options.gregorianField === 'string' 
                    ? document.querySelector(this.options.gregorianField)
                    : this.options.gregorianField;
                if (hiddenField) {
                    hiddenField.value = gregorianStr;
                }
            }
            
            // Callback
            this.options.onSelect(jalaliStr, gregorianStr, { jy, jm, jd }, { gy: g.gy, gm: g.gm, gd: g.gd });
            
            // Auto close
            if (this.options.autoClose) {
                this.close();
            } else {
                this.render();
            }
        }

        selectToday() {
            const today = getTodayJalali();
            this.viewYear = today.jy;
            this.viewMonth = today.jm;
            this.selectDate(today.jy, today.jm, today.jd);
        }

        setDate(jalaliStr) {
            if (!jalaliStr) {
                this.selectedDate = null;
                this.input.value = '';
                return;
            }
            
            const parts = jalaliStr.split('/');
            if (parts.length === 3) {
                const jy = parseInt(parts[0], 10);
                const jm = parseInt(parts[1], 10);
                const jd = parseInt(parts[2], 10);
                this.selectDate(jy, jm, jd);
            }
        }

        getDate() {
            return this.selectedDate ? formatDate(this.selectedDate.jy, this.selectedDate.jm, this.selectedDate.jd) : null;
        }

        getGregorianDate() {
            if (!this.selectedDate) return null;
            const g = toGregorian(this.selectedDate.jy, this.selectedDate.jm, this.selectedDate.jd);
            return formatGregorian(g.gy, g.gm, g.gd);
        }

        destroy() {
            if (this.picker && this.picker.parentNode) {
                this.picker.parentNode.removeChild(this.picker);
            }
        }
    }

    // Static init method
    JalaliDatePicker.init = function(selector, options) {
        const elements = typeof selector === 'string' 
            ? document.querySelectorAll(selector)
            : [selector];
        
        const instances = [];
        elements.forEach(el => {
            instances.push(new JalaliDatePicker(el, options));
        });
        
        return instances.length === 1 ? instances[0] : instances;
    };

    // jQuery plugin
    if (typeof jQuery !== 'undefined') {
        jQuery.fn.jalaliDatePicker = function(options) {
            return this.each(function() {
                if (!jQuery(this).data('jalaliDatePicker')) {
                    jQuery(this).data('jalaliDatePicker', new JalaliDatePicker(this, options));
                }
            });
        };
    }

    // Export
    global.JalaliDatePicker = JalaliDatePicker;

})(typeof window !== 'undefined' ? window : this);

