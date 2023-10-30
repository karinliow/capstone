function showLoadingOverlay() {
    $("#loading-overlay").show();
}

function hideLoadingOverlay() {
    $("#loading-overlay").hide();
}

$(document).ready(function () {
    // Hide the loading overlay initially
    hideLoadingOverlay();

    // Attach the loading overlay to the click event of the "View Grades" link
    $(".dropdown-item[href='/Grade/Index']").on("click", function (e) {
        showLoadingOverlay();
        window.location.href = $(this).attr("href"); // Redirect manually after showing overlay
    });

    $("form").on("submit", function () {
        showLoadingOverlay();
    });

//    if ("@TempData["message"]" !== null) {
//        hideLoadingOverlay();
//}
});
