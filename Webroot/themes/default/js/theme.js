var disqus = "disqus-id-goes-here";

$(function () {
    $('a[rel="lightbox"]').fluidbox();
    $(".post").fitVids();
    $(".menu-btn, .sidebar").click(function () {
        $("main").toggleClass("content-open disable-scroll")
    })
    $(".show-comments").on("click", function () {
        $.ajax({
            type: "GET",
            url: "http://" + disqus + ".disqus.com/embed.js",
            dataType: "script",
            cache: !0
        }), $(this).fadeOut()
    });

})


