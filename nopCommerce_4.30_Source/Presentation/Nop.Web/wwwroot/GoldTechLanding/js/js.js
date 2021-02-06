$(document).ready(function () {
    $('.spoiler-body').css({'display':'none'});
    $('.spoiler-head').click(function(){
        if ($(this).hasClass('open-spoil'))
        {
            $(this).removeClass('open-spoil');
        }
        else {
            $(this).addClass('open-spoil');
        }

        $(this).next('.spoiler-body').slideToggle(500)
    });

    //$('.spoiler-body-contact').css({'display':'none'});
    $('.spoiler-head-contact').click(function(){
        if ($(this).hasClass('open-spoil-contact'))
        {
            $(this).removeClass('open-spoil-contact');
        }
        else {
            $(this).addClass('open-spoil-contact');
        }

        $(this).next('.spoiler-body-contact').slideToggle(500)
    });


    $('.cert-slide').slick({
        slidesToShow: 3,
        slidesToScroll: 1,
        arrows: true,
        prevArrow: '<button class="slide-cert-arrow prev"></button>',
        nextArrow: '<button class="slide-cert-arrow next"></button>',
    });



    $(document).on('scroll', function () {
        if ($(window).scrollTop() < 100)
        {
            $('.navbar').removeClass('scroll-navbar');
        }
        if ($(window).scrollTop() > 200)
        {
            $('.navbar').addClass('scroll-navbar');
        }
    });


    $('.flow-scroll').on( 'click', function(){
        let el = $(this);
        let dest = el.attr('href'); // получаем направление
        if(dest !== undefined && dest !== '') { // проверяем существование
            $('html').animate({
                    scrollTop: $(dest).offset().top // прокручиваем страницу к требуемому элементу
                }, 500 // скорость прокрутки
            );
        }
        return false;
    });

    if (Cookies.get('agree_cookies')) {
        $('.cookies').addClass('cookies-agree');
    }

    $('#agreeCookies').click(function () {
        Cookies.set('agree_cookies', 'true', { expires: 7 });

        $('.cookies').addClass('cookies-agree')
    });


    //адаптивность

    $('#toggle-mobile').click(function () {
        $(this).hasClass('change')? $(this).removeClass('change'): $(this).addClass('change');
    });

    if ($(document).width() < 1181)
    {
        $('.mobile-menu').css('display','block');
    }



});

$(window).on('load', function () {
    $('#image-preload').addClass('preload_fade');

    setTimeout(function () {
        document.body.classList.add('loaded_hiding');
    }, 500);

    setTimeout(function () {
        document.body.classList.add('loaded_hiding');
        document.body.classList.add('loaded');
        document.body.classList.remove('loaded_hiding');
    }, 1000);
});

function get_coins() {
    return JSON.parse(Cookies.get('cart'));
}

